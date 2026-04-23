using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Services.Content.Interfaces;

namespace TibiaHuntMaster.Infrastructure.Services.System
{
    // Interface definieren, damit wir es in der App Schicht nutzen können
    public interface IDataSyncStatusSource
    {
        void UpdateState(bool isCritical, bool isSyncing, bool isRetry, string message);
    }

    public sealed class BackgroundSyncWorker(
        IDbContextFactory<AppDbContext> dbFactory,
        IContentService contentService,
        IContentProgressService contentProgressService,
        ImbuementSeedService imbuementSeeder,
        MonsterSpawnSeedService monsterSpawnSeedService,
        ILogger<BackgroundSyncWorker> logger)
    {
        private CancellationTokenSource? _cts;

        // Wir injizieren hier eine Action oder ein Interface, um den Status an die UI zu melden,
        // ohne dass die Infrastructure Schicht die App Schicht kennen muss.
        public Action<bool, bool, bool, string>? OnStateChanged { get; set; }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            // Fire & Forget im Hintergrund
            Task.Run(() => RunOnceAsync(_cts.Token));
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        public Task RunOnceAsync(CancellationToken cancellationToken = default)
        {
            return RunSyncProcess(cancellationToken);
        }

        private async Task RunSyncProcess(CancellationToken cancellationToken)
        {
            try
            {
                // 1. Initial Check: Ist die DB leer?
                bool isEmpty = await IsDatabaseEmpty();

                if(isEmpty)
                {
                    await RunInitialLoop(cancellationToken);
                }
                else
                {
                    await RunUpdateCheck(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                contentProgressService.Report("Cancelled", "Content synchronization was cancelled.", 0, true);
                Notify(false, false, false, "Sync cancelled by user.");
                await Task.Delay(3000);
                Notify(false, false, false, string.Empty);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background sync crashed unexpectedly.");
                contentProgressService.Report("Failed", "Background sync failed unexpectedly.", 0, true);
                Notify(true, false, true, "Background sync failed unexpectedly. Please restart the app.");
            }
        }

        private async Task<bool> IsDatabaseEmpty()
        {
            await using AppDbContext db = await dbFactory.CreateDbContextAsync();
            bool hasItems = await db.Items.AnyAsync();
            bool hasCreatures = await db.Creatures.AnyAsync();
            bool hasHuntingPlaces = await db.HuntingPlaces.AnyAsync();
            return !(hasItems && hasCreatures && hasHuntingPlaces);
        }

        private async Task RunInitialLoop(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Status: Kritisch, Syncing
                Notify(true, true, false, "Database empty. Preparing local content data...");
                contentProgressService.Report("Preparing local content", "Database is empty. Starting initial setup...", 2, true);

                try
                {
                    // Versuch Import
                    await PerformInitialImport(cancellationToken);

                    // Erfolg!
                    contentProgressService.Report("Ready", "Initial setup complete.", 100);
                    Notify(false, false, false, "Initial setup complete.");
                    return; // Loop beenden
                }
                catch (OperationCanceledException)
                {
                    // User hat abgebrochen - werfen wir nach oben
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Initial import failed.");

                    // Fehler -> Warten
                    for (int i = 60; i > 0; i--)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        contentProgressService.Report("Retrying download", $"Download failed. Retrying in {i} seconds...", 0, true);
                        Notify(true,
                            false,
                            true,
                            $"Download failed. Retrying in {i} seconds... (Check Internet)");
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
        }

        private async Task RunUpdateCheck(CancellationToken cancellationToken)
        {
            // Status: Nicht kritisch, aber Syncing (Update Banner)
            Notify(false, true, false, "Checking for content updates...");
            contentProgressService.Report("Checking for updates", "Checking local content for updates...", 10, true);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Update Logik (Items, Creatures, Places)
                // Da unsere Import-Services "Upsert" machen (existierende updaten, neue anlegen),
                // können wir einfach dieselben Methoden rufen.

                await PerformRefresh(cancellationToken);

                Notify(false, false, false, "Database is up to date.");
                contentProgressService.Report("Up to date", "Local content is up to date.", 100);

                // Nachricht nach 3 Sekunden ausblenden
                await Task.Delay(3000, cancellationToken);
                Notify(false, false, false, string.Empty);
            }
            catch (OperationCanceledException)
            {
                // User hat abgebrochen - werfen wir nach oben
                throw;
            }
            catch (Exception ex)
            {
                // Bei Updates ist ein Fehler nicht schlimm, wir haben ja alte Daten.
                logger.LogError(ex, "Update check failed.");
                contentProgressService.Report("Update failed", "Update failed. Using local content.", 0, true);
                Notify(false, false, true, "Update failed. Using local data.");
                await Task.Delay(5000, cancellationToken);
                Notify(false, false, false, string.Empty);
            }
        }

        private async Task PerformInitialImport(CancellationToken cancellationToken)
        {
            contentProgressService.Report("Preparing supporting data", "Preparing monster spawn data...", 5);
            await monsterSpawnSeedService.EnsureSpawnsSeededAsync(cancellationToken);
            contentProgressService.Report("Importing content", "Importing content from ByteWizards API...", 10);
            await contentService.EnsureContentInitializedAsync(cancellationToken);
            contentProgressService.Report("Finalizing setup", "Refreshing monster spawn links...", 92);
            await monsterSpawnSeedService.EnsureSpawnsSeededAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            contentProgressService.Report("Preparing configuration", "Preparing imbuement recipes...", 96);
            await imbuementSeeder.EnsureRecipesSeededAsync();
        }

        private async Task PerformRefresh(CancellationToken cancellationToken)
        {
            contentProgressService.Report("Refreshing supporting data", "Preparing monster spawn data...", 8);
            await monsterSpawnSeedService.EnsureSpawnsSeededAsync(cancellationToken);
            contentProgressService.Report("Refreshing content", "Refreshing content from ByteWizards API...", 15);
            await contentService.RefreshContentAsync(cancellationToken);
            contentProgressService.Report("Refreshing supporting data", "Refreshing monster spawn links...", 92);
            await monsterSpawnSeedService.EnsureSpawnsSeededAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            contentProgressService.Report("Refreshing configuration", "Refreshing imbuement recipes...", 96);
            await imbuementSeeder.EnsureRecipesSeededAsync();
        }

        private void Notify(bool isCritical, bool isSyncing, bool isRetry, string msg)
        {
            OnStateChanged?.Invoke(isCritical, isSyncing, isRetry, msg);
        }
    }
}
