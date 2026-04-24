using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.App.ViewModels.Startup
{
    public sealed partial class SetupViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localizationService;

        [ObservableProperty]private double _progressValue; // 0 bis 100
        [ObservableProperty]private bool _isProgressIndeterminate = true;
        [ObservableProperty]private string _hintMessage = string.Empty;

        [ObservableProperty]private string _currentStep;
        [ObservableProperty]private string _statusMessage;

        public ObservableCollection<string> RecentActivities { get; } = [];

        public SetupViewModel(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _currentStep = _localizationService["Setup_CurrentStepDefault"];
            _statusMessage = _localizationService["Setup_Initializing"];
            _hintMessage = _localizationService["Setup_MayTakeTime"];
        }

        // Designer constructor
        public SetupViewModel()
        {
            _localizationService = null!;
            _currentStep = "Preparing setup";
            _statusMessage = "Initializing...";
            _hintMessage = "First setup can take a while depending on your connection.";
        }

        public void ApplyProgress(ContentProgressUpdate update)
        {
            if(!string.IsNullOrWhiteSpace(update.Step))
            {
                CurrentStep = update.Step;
            }

            if(!string.IsNullOrWhiteSpace(update.Message))
            {
                StatusMessage = update.Message;
                AddActivity(update.Message);
            }

            ProgressValue = update.ProgressValue;
            IsProgressIndeterminate = update.IsIndeterminate;
        }

        public void AddActivity(string message)
        {
            if(string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if(RecentActivities.Count > 0 && string.Equals(RecentActivities[0], message, StringComparison.Ordinal))
            {
                return;
            }

            RecentActivities.Insert(0, message);

            while (RecentActivities.Count > 6)
            {
                RecentActivities.RemoveAt(RecentActivities.Count - 1);
            }
        }

        public void ClearActivities()
        {
            RecentActivities.Clear();
        }
    }
}
