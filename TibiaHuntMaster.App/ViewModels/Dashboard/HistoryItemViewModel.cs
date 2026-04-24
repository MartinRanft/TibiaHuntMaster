using System.Collections.ObjectModel;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.App.ViewModels.Dashboard
{
    public sealed partial class HistoryItemViewModel : ViewModelBase
    {
        private readonly ILocalizationService? _localizationService;
        [ObservableProperty]private string _goalName = string.Empty;

        [ObservableProperty]private bool _isExpanded;

        // NEU: Goal Infos
        [ObservableProperty]private bool _isFinisher;
        [ObservableProperty]private bool _isGroup;
        [ObservableProperty]private bool _isSelected;

        public HistoryItemViewModel(HuntSessionEntity session, ILocalizationService? localizationService = null)
        {
            Session = session;
            IsGroup = false;
            _localizationService = localizationService;
        }

        public HistoryItemViewModel(TeamHuntSessionEntity teamSession, ILocalizationService? localizationService = null)
        {
            TeamSession = teamSession;
            IsGroup = false;
            _localizationService = localizationService;
        }

        public HistoryItemViewModel(HuntGroupEntity group, IEnumerable<HuntSessionEntity> sessions, ILocalizationService? localizationService = null)
        {
            Group = group;
            IsGroup = true;
            _localizationService = localizationService;
            foreach(HuntSessionEntity s in sessions.OrderByDescending(x => x.SessionStartTime))
            {
                Children.Add(new HistoryItemViewModel(s, localizationService));
            }
        }

        public HuntSessionEntity? Session { get; }

        public TeamHuntSessionEntity? TeamSession { get; }

        public HuntGroupEntity? Group { get; }

        public ObservableCollection<HistoryItemViewModel> Children { get; } = new();

        // --- Helper für die View ---
        public bool ShowMedal => IsFinisher;

        public bool IsTeam => TeamSession != null;

        // --- Display Logic ---
        public string GroupNameDisplay
        {
            get
            {
                if(!IsGroup)
                {
                    return "";
                }
                DateTimeOffset latestDate = Children.Select(c => c.Session?.SessionStartTime.Year > 2000 ? c.Session!.SessionStartTime : c.Session?.ImportedAt ?? DateTimeOffset.MinValue)
                                                    .DefaultIfEmpty(DateTimeOffset.MinValue).Max();
                if(_localizationService != null)
                {
                    return latestDate > DateTimeOffset.MinValue
                        ? string.Format(_localizationService["HistoryItem_MergedHunt"], latestDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm"))
                        : _localizationService["HistoryItem_MergedGroup"];
                }
                return latestDate > DateTimeOffset.MinValue ? $"Merged Hunt ({latestDate.ToLocalTime():dd.MM.yyyy HH:mm})" : Group?.Name ?? "Merged Group";
            }
        }

        public string DateDisplay
        {
            get
            {
                if(IsGroup)
                {
                    DateTimeOffset latestDate = Children.Select(c => c.Session?.SessionStartTime.Year > 2000 ? c.Session!.SessionStartTime : c.Session?.ImportedAt ?? DateTimeOffset.MinValue)
                                                        .DefaultIfEmpty(DateTimeOffset.MinValue).Max();
                    return latestDate > DateTimeOffset.MinValue ? latestDate.ToLocalTime().ToString("dd.MM.yyyy\nHH:mm") : Group?.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy\nHH:mm") ?? "-";
                }
                if(Session != null)
                {
                    DateTimeOffset time = Session.SessionStartTime.Year > 2000 ? Session.SessionStartTime : Session.ImportedAt;
                    return time.ToLocalTime().ToString("dd.MM.yyyy\nHH:mm");
                }
                if(TeamSession != null)
                {
                    DateTimeOffset time = TeamSession.SessionStartTime.Year > 2000 ? TeamSession.SessionStartTime : TeamSession.ImportedAt;
                    return time.ToLocalTime().ToString("dd.MM.yyyy\nHH:mm");
                }
                return "-";
            }
        }

        public string DurationDisplay
        {
            get
            {
                TimeSpan totalTime = TimeSpan.Zero;
                if(IsGroup)
                {
                    totalTime = TimeSpan.FromTicks(Children.Sum(c => c.Session?.Duration.Ticks ?? 0));
                }
                else if(Session != null)
                {
                    totalTime = Session.Duration;
                }
                else if(TeamSession != null)
                {
                    totalTime = TeamSession.Duration;
                }
                if(_localizationService != null)
                {
                    return $"{(int)totalTime.TotalMinutes}{_localizationService["HistoryItem_MinutesSuffix"]}";
                }
                return $"{(int)totalTime.TotalMinutes} min";
            }
        }

        public long BalanceDisplay
        {
            get
            {
                if(IsGroup)
                {
                    return Children.Sum(c => CalculateEffectiveBalance(c.Session));
                }
                if(Session != null)
                {
                    return CalculateEffectiveBalance(Session);
                }
                if(TeamSession != null)
                {
                    return TeamSession.TotalBalance;
                }
                return 0;
            }
        }

        // --- Badges ---
        public string TypeIcon
        {
            get
            {
                if(IsGroup)
                {
                    return "📚";
                }
                if(IsTeam)
                {
                    return "👥";
                }
                return "👤";
            }
        }

        public string TypeLabel
        {
            get
            {
                if(_localizationService != null)
                {
                    if(IsGroup)
                    {
                        return _localizationService["HistoryItem_TypeMerged"];
                    }
                    if(IsTeam)
                    {
                        return _localizationService["HistoryItem_TypeParty"];
                    }
                    return _localizationService["HistoryItem_TypeSolo"];
                }

                if(IsGroup)
                {
                    return "MERGED";
                }
                if(IsTeam)
                {
                    return "PARTY";
                }
                return "SOLO";
            }
        }

        public IBrush TypeColor
        {
            get
            {
                if(IsGroup)
                {
                    return SolidColorBrush.Parse("#FFC107");
                }
                if(IsTeam)
                {
                    return SolidColorBrush.Parse("#AB47BC");
                }
                return SolidColorBrush.Parse("#666666");
            }
        }

        public IBrush TypeBackground
        {
            get
            {
                if(IsGroup)
                {
                    return SolidColorBrush.Parse("#26FFC107");
                }
                if(IsTeam)
                {
                    return SolidColorBrush.Parse("#26AB47BC");
                }
                return SolidColorBrush.Parse("#1AFFFFFF");
            }
        }

        private long CalculateEffectiveBalance(HuntSessionEntity? s)
        {
            if(s == null)
            {
                return 0;
            }
            long baseBalance = s.Balance;
            if(s.SupplyAdjustments != null)
            {
                foreach(HuntSupplyAdjustment adj in s.SupplyAdjustments)
                {
                    if(adj.Type == SupplyAdjustmentType.Deduction)
                    {
                        baseBalance += adj.Value;
                    }
                    else
                    {
                        baseBalance -= adj.Value;
                    }
                }
            }
            return baseBalance;
        }

        // Refresh localized strings when language changes
        public void RefreshLocalizedStrings()
        {
            OnPropertyChanged(nameof(GroupNameDisplay));
            OnPropertyChanged(nameof(DurationDisplay));
            OnPropertyChanged(nameof(TypeLabel));

            // Refresh children recursively
            foreach (var child in Children)
            {
                child.RefreshLocalizedStrings();
            }
        }
    }
}