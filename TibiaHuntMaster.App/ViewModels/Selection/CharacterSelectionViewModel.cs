using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TibiaHuntMaster.App.Services.Localization;
using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Core.Abstractions.TibiaData;
using TibiaHuntMaster.Core.Characters;

namespace TibiaHuntMaster.App.ViewModels.Selection
{
    public sealed partial class CharacterSelectionViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly ILocalizationService _localizationService;

        public CharacterSelectionViewModel(ICharacterService characterService, ILocalizationService localizationService)
        {
            _characterService = characterService;
            _localizationService = localizationService;
        }
        [ObservableProperty]private ObservableCollection<Character> _characters = [];

        [ObservableProperty]private string _errorMessage = string.Empty;

        // Für den "Add Character" Dialog
        [ObservableProperty]private bool _isAddDialogOpen;

        [ObservableProperty]private bool _isLoading;

        [ObservableProperty]private bool _isUpdatingInBackground;

        [ObservableProperty]private string _newCharacterName = string.Empty;

        [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(ConfirmSelectionCommand))]
        private Character? _selectedCharacter;

        public event Action<Character>? CharacterSelected;

        public async Task LoadCharactersAsync()
        {
            IsLoading = true;
            try
            {
                IReadOnlyList<Character> list = await _characterService.ListAsync();
                Characters.Clear();
                foreach (Character character in list)
                {
                    Characters.Add(character);
                }
            }
            finally
            {
                IsLoading = false;
            }

            if(Characters.Count > 0)
            {
                IsUpdatingInBackground = true;

                List<Character> charsToUpdate = Characters.ToList();

                foreach(Character existingChar in charsToUpdate)
                {
                    try
                    {
                        Character updatedChar = await _characterService.ImportFromTibiaDataAsync(existingChar.Name);

                        Dispatcher.UIThread.Post(() =>
                        {
                            Character? oldItem = Characters.FirstOrDefault(c => c.Id == existingChar.Id);

                            if(oldItem != null)
                            {
                                int index = Characters.IndexOf(oldItem);
                                if(index != -1)
                                {
                                    Characters[index] = updatedChar;

                                    if(SelectedCharacter?.Id == updatedChar.Id)
                                    {
                                        SelectedCharacter = updatedChar;
                                    }
                                }
                            }
                        });

                        await Task.Delay(100);
                    }
                    catch (Exception)
                    {
                        // Background update failed, ignore
                    }
                }
                IsUpdatingInBackground = false;
            }
        }

        [RelayCommand]
        private void OpenAddDialog()
        {
            ErrorMessage = string.Empty;
            NewCharacterName = string.Empty;
            IsAddDialogOpen = true;
        }

        [RelayCommand]
        private void CloseAddDialog()
        {
            IsAddDialogOpen = false;
        }

        [RelayCommand]
        private async Task ImportCharacter()
        {
            string requestedName = UserInputSanitizer.TrimAndTruncate(NewCharacterName, UserInputLimits.CharacterNameMaxLength);
            if(string.IsNullOrWhiteSpace(requestedName))
            {
                return;
            }

            if(UserInputSanitizer.ExceedsLength(NewCharacterName, UserInputLimits.CharacterNameMaxLength))
            {
                ErrorMessage = $"Character name is too long (max {UserInputLimits.CharacterNameMaxLength} characters).";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // 1. Importieren (Backend ruft TibiaData auf)
                Character newChar = await _characterService.ImportFromTibiaDataAsync(requestedName);

                // 2. Speichern
                await _characterService.SaveAsync(newChar);

                // 3. UI Update
                await LoadCharactersAsync();

                // 4. Dialog schließen & neuen Char auswählen
                IsAddDialogOpen = false;
                SelectedCharacter = Characters.FirstOrDefault(c => c.Name == newChar.Name);
            }
            catch (Exception ex)
            {
                ErrorMessage = string.Format(_localizationService["CharSelection_Error"], ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void ConfirmSelection()
        {
            if(SelectedCharacter != null)
            {
                CharacterSelected?.Invoke(SelectedCharacter);
            }
        }

        private bool CanConfirm()
        {
            return SelectedCharacter != null;
        }
    }
}
