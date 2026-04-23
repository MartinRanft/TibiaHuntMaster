using CommunityToolkit.Mvvm.ComponentModel;

namespace TibiaHuntMaster.App.Services
{
    public sealed partial class DataStatusService : ObservableObject
    {
        // Zeigt an, dass die DB kritisch leer ist (Blockierender Banner oder Warnung)
        [ObservableProperty]private bool _isCriticalMissing;

        // Zeigt an, ob der letzte Versuch fehlgeschlagen ist (für den Retry-Countdown)
        [ObservableProperty]private bool _isInRetryDelay;

        // Zeigt an, dass gerade ein Prozess läuft (Update oder Initial)
        [ObservableProperty]private bool _isSyncing;

        // Die Nachricht für den User
        [ObservableProperty]private string _statusMessage = string.Empty;

        // Farbe für den Banner (true = Gold/Warnung, false = Info/Blau)
        public bool IsWarningState => IsCriticalMissing;
    }
}