using Microsoft.Extensions.Logging;

namespace TibiaHuntMaster.Infrastructure.Services.TibiaData
{
    public sealed class BoostedCreatureMonitor(
        ICreatureSyncService syncService,
        ILogger<BoostedCreatureMonitor> logger) : IDisposable
    {
        private Timer? _timer;

        public void Dispose()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
        }

        public void Start()
        {
            logger.LogInformation("Starting Boosted Creature Monitor...");

            // 1. Sofortiger Sync beim App-Start (im Hintergrund)
            Task.Run(SyncSafe);

            // 2. Timer berechnen für nächsten Server Save (10:00 CET/CEST)
            ScheduleNextRun();
        }

        private void ScheduleNextRun()
        {
            try
            {
                DateTime now = DateTime.Now;

                // Server Save ist 10:00 Deutsche Zeit. Wir nehmen 10:05 als Puffer.
                DateTime todayTarget = now.Date.AddHours(10).AddMinutes(5);
                if(now > todayTarget)
                {
                    todayTarget = todayTarget.AddDays(1); // Dann morgen
                }

                TimeSpan delay = todayTarget - now;
                logger.LogInformation("Next Boosted Sync scheduled in {Time}", delay);

                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
                _timer?.Dispose();
                _timer = new Timer(OnTimerCallback, null, delay, Timeout.InfiniteTimeSpan);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to schedule next boosted creature sync.");
            }
        }

        private void OnTimerCallback(object? state)
        {
            _ = RunScheduledSyncSafeAsync();
        }

        private async Task RunScheduledSyncSafeAsync()
        {
            try
            {
                await SyncSafe();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception during scheduled boosted creature sync.");
            }
            finally
            {
                ScheduleNextRun(); // Timer für den nächsten Tag neu setzen
            }
        }

        private async Task SyncSafe()
        {
            try
            {
                await syncService.SyncCreaturesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Boosted Creature Monitor");
            }
        }
    }
}
