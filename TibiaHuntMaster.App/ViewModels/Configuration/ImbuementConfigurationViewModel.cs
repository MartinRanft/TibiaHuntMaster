using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using TibiaHuntMaster.App.Services.Navigation;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;
using TibiaHuntMaster.Infrastructure.Services.Analysis;

namespace TibiaHuntMaster.App.ViewModels.Configuration
{
    public sealed partial class ImbuementConfigurationViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IImbuementCalculatorService calcService,
        INavigationService navigationService,
        ILogger<ImbuementConfigurationViewModel>? logger = null) : ViewModelBase, INavigationAware
    {
        private readonly List<ImbuementTypeGroup> _allGroups = new(); // Master Liste
        private readonly ILogger<ImbuementConfigurationViewModel> _logger = logger ?? NullLogger<ImbuementConfigurationViewModel>.Instance;
        private int _characterId;
        [ObservableProperty]private bool _isLoading;
        private Action? _onClose;
        [ObservableProperty]private string _searchText = string.Empty;

        // Filter-Tabs
        [ObservableProperty]private int _selectedCategoryIndex; // 0=All, 1=Core, 2=Skill, 3=Defense, 4=Utility

        [ObservableProperty]private bool _useBlankScrolls;

        // UI Properties
        public ObservableCollection<ImbuementTypeGroup> FilteredGroups { get; } = new();

        public ObservableCollection<ItemPriceViewModel> ItemPrices { get; } = new();

        // INavigationAware Implementation
        public void OnNavigatedTo(object? parameter)
        {
            if(parameter is NavigationParameters.ImbuementConfiguration config)
            {
                _onClose = config.OnClose;
                _ = LoadAsync(config.CharacterId);
            }
        }

        public void OnNavigatedFrom()
        {
            // Cleanup if needed
        }

        // Wenn sich Suche oder Tab ändert -> Filtern
        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }
        partial void OnSelectedCategoryIndexChanged(int value)
        {
            ApplyFilter();
        }

        public async Task LoadAsync(int charId)
        {
            _characterId = charId;
            IsLoading = true;
            _allGroups.Clear();
            FilteredGroups.Clear();
            ItemPrices.Clear();

            try
            {
                await using AppDbContext db = await dbFactory.CreateDbContextAsync();

                // 1. Profil & Rezepte laden (wie gehabt)
                ImbuementProfileEntity profile = await calcService.GetProfileAsync(charId);
                List<CharacterActiveImbuement> activeLinks = await db.CharacterActiveImbuements.Where(x => x.ImbuementProfileId == profile.Id).ToListAsync();
                UseBlankScrolls = profile.UseBlankScrolls;

                List<ImbuementRecipeEntity> recipes = await db.ImbuementRecipes
                                                              .Include(r => r.Ingredients).ThenInclude(i => i.Item)
                                                              .OrderBy(r => r.Type).ThenBy(r => r.Tier)
                                                              .ToListAsync();

                Dictionary<int, long> userPrices = await db.UserItemPrices.ToDictionaryAsync(x => x.ItemId, x => x.Price);

                // 2. Gruppen bauen (Master Liste)
                IEnumerable<IGrouping<ImbuementType, ImbuementRecipeEntity>> grouped = recipes.GroupBy(r => r.Type);
                foreach(IGrouping<ImbuementType, ImbuementRecipeEntity> group in grouped)
                {
                    ImbuementTypeGroup groupVm = new(group.Key.ToString(), group.Key);
                    foreach(ImbuementRecipeEntity recipe in group)
                    {
                        CharacterActiveImbuement? link = activeLinks.FirstOrDefault(x => x.ImbuementRecipeId == recipe.Id);
                        groupVm.Recipes.Add(new ImbuementRecipeViewModel(recipe, link?.Count ?? 0));
                    }
                    _allGroups.Add(groupVm);
                }

                // 3. Preise
                List<ImbuementIngredientEntity> allIngredients = recipes.SelectMany(r => r.Ingredients).DistinctBy(i => i.ItemId).ToList();
                foreach(ImbuementIngredientEntity ing in allIngredients.OrderBy(i => i.Item.Name))
                {
                    long currentPrice = userPrices.TryGetValue(ing.ItemId, out long p) ? p : 0;
                    ItemPrices.Add(new ItemPriceViewModel(ing.Item.Id, ing.Item.Name, currentPrice, ing.Item.Icon));
                }

                ApplyFilter();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            FilteredGroups.Clear();
            string search = SearchText.Trim().ToLowerInvariant();

            foreach(ImbuementTypeGroup group in _allGroups)
            {
                // 1. Kategorie Filter
                bool categoryMatch = SelectedCategoryIndex == 0 || IsInCategory(group.Type, SelectedCategoryIndex);
                if(!categoryMatch)
                {
                    continue;
                }

                // 2. Text Filter (Suche in Rezept-Namen)
                // Wir zeigen die Gruppe, wenn der Gruppenname matched ODER eines der Rezepte
                List<ImbuementRecipeViewModel> matchingRecipes = group.Recipes.Where(r => string.IsNullOrEmpty(search) || r.Recipe.Name.ToLowerInvariant().Contains(search)).ToList();

                if(matchingRecipes.Any())
                {
                    // Wir erstellen eine temporäre View-Gruppe nur mit den Treffern
                    ImbuementTypeGroup viewGroup = new(group.Name, group.Type);
                    foreach(ImbuementRecipeViewModel r in matchingRecipes)
                    {
                        viewGroup.Recipes.Add(r);
                    }
                    FilteredGroups.Add(viewGroup);
                }
            }
        }

        private bool IsInCategory(ImbuementType type, int index)
        {
            return index switch
            {
                1 => type is ImbuementType.Void or ImbuementType.Vampirism or ImbuementType.Strike, // Core
                2 => type is ImbuementType.Skill,
                3 => type is ImbuementType.Protection,
                4 => type is ImbuementType.Utility,
                _ => true
            };
        }

        // ... Save und Cancel Commands bleiben gleich ...
        // (Bitte den Save Command von vorhin hier einfügen, nur auf FilteredGroups aufpassen -> _allGroups iterieren beim Speichern!)

        [RelayCommand]
        private async Task Save()
        {
            _logger.LogDebug("Saving imbuement configuration for character {CharacterId}", _characterId);
            IsLoading = true;
            try
            {
                await using AppDbContext db = await dbFactory.CreateDbContextAsync();
                ImbuementProfileEntity? profile = await db.ImbuementProfiles.Include(p => p.ActiveImbuements).FirstOrDefaultAsync(p => p.CharacterId == _characterId);

                if(profile == null)
                {
                    profile = new ImbuementProfileEntity
                    {
                        CharacterId = _characterId
                    };
                    db.ImbuementProfiles.Add(profile);
                }

                profile.UseBlankScrolls = UseBlankScrolls;
                profile.LastUpdated = DateTimeOffset.UtcNow;

                db.CharacterActiveImbuements.RemoveRange(profile.ActiveImbuements);

                // WICHTIG: Über _allGroups iterieren, nicht über FilteredGroups, sonst gehen ausgeblendete verloren!
                foreach(ImbuementRecipeViewModel recipeVm in from @group in _allGroups from recipeVm in @group.Recipes where recipeVm.Count > 0 select recipeVm)
                {
                    db.CharacterActiveImbuements.Add(new CharacterActiveImbuement
                    {
                        ImbuementProfileId = profile.Id,
                        ImbuementProfile = profile,
                        ImbuementRecipeId = recipeVm.Recipe.Id,
                        Count = recipeVm.Count
                    });
                }

                foreach(ItemPriceViewModel priceVm in ItemPrices)
                {
                    UserItemPriceEntity? priceEntity = await db.UserItemPrices.FirstOrDefaultAsync(x => x.ItemId == priceVm.ItemId);
                    if(priceEntity == null)
                    {
                        if(priceVm.Price > 0)
                        {
                            db.UserItemPrices.Add(new UserItemPriceEntity
                            {
                                ItemId = priceVm.ItemId,
                                Price = priceVm.Price,
                                LastUpdated = DateTimeOffset.UtcNow
                            });
                        }
                    }
                    else if(priceEntity.Price != priceVm.Price)
                    {
                        priceEntity.Price = priceVm.Price;
                        priceEntity.LastUpdated = DateTimeOffset.UtcNow;
                    }
                }

                await db.SaveChangesAsync();
                _logger.LogInformation("Imbuement configuration saved for character {CharacterId}", _characterId);

                _onClose?.Invoke();

                navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while saving imbuement configuration for character {CharacterId}", _characterId);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _logger.LogDebug("Cancelling imbuement configuration for character {CharacterId}", _characterId);
            navigationService.GoBack();
        }
    }

    // Helper Update
    public sealed class ImbuementTypeGroup(string name, ImbuementType type)
    {
        public string Name { get; } = name;

        public ImbuementType Type { get; } = type; // Für Filter

        public ObservableCollection<ImbuementRecipeViewModel> Recipes { get; } = new();
    }

    // ... Rest der Helper (ItemPriceViewModel etc.) bleibt gleich ...
    public sealed partial class ImbuementRecipeViewModel(ImbuementRecipeEntity recipe, int count) : ObservableObject
    {
        [ObservableProperty]private int _count = count;

        public ImbuementRecipeEntity Recipe { get; } = recipe;

        public string TierColor => Recipe.Tier switch
        {
            ImbuementTier.Basic => "#888888",
            ImbuementTier.Intricate => "#2E7D32",  // Darker green for better contrast
            ImbuementTier.Powerful => "#6A1B9A",   // Darker purple for better contrast
            _ => "#000000"
        };

        [RelayCommand]
        private void Increment()
        {
            if(Count < 4)
            {
                Count++;
            }
        }
        [RelayCommand]
        private void Decrement()
        {
            if(Count > 0)
            {
                Count--;
            }
        }
    }

    public sealed partial class ItemPriceViewModel(int itemId, string name, long price, string iconUrl) : ObservableObject
    {
        [ObservableProperty]private long _price = price;

        public int ItemId { get; } = itemId;

        public string Name { get; } = name;

        public string IconUrl { get; } = iconUrl;

        // Farbe für Preis: Grau wenn 0 (Warnung/Hinweis), sonst Weiß
        public string PriceColor => Price == 0 ? "#666" : "White";

        // Wenn Price geändert wird, Farbe updaten (quick fix über PropertyChanged notification im Setter oder in der View via Converter)
        // Einfacher: View Logik.
    }
}
