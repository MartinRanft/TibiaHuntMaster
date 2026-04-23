using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TibiaHuntMaster.Infrastructure.Services.Hunts
{
    internal static class SqliteWriteRetry
    {
        private static readonly TimeSpan[] RetryDelays =
        [
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(500)
        ];

        public static async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            ILogger logger,
            string operationName,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

            for (int attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await operation(ct);
                }
                catch (Exception ex) when (attempt < RetryDelays.Length && IsWriteContention(ex))
                {
                    TimeSpan delay = RetryDelays[attempt];
                    logger.LogWarning(
                        ex,
                        "SQLite write contention during {OperationName}. Retrying in {DelayMs} ms (attempt {Attempt}/{TotalAttempts}).",
                        operationName,
                        (int)delay.TotalMilliseconds,
                        attempt + 1,
                        RetryDelays.Length + 1);

                    await Task.Delay(delay, ct);
                }
            }
        }

        internal static bool IsWriteContention(Exception exception)
        {
            return exception switch
            {
                SqliteException sqliteException => IsBusyOrLocked(sqliteException),
                DbUpdateException dbUpdateException when dbUpdateException.InnerException is SqliteException sqliteException =>
                    IsBusyOrLocked(sqliteException),
                _ => false
            };
        }

        private static bool IsBusyOrLocked(SqliteException exception)
        {
            return exception.SqliteErrorCode is 5 or 6;
        }
    }
}
