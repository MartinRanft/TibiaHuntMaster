namespace TibiaHuntMaster.Infrastructure.Services.Content.Models
{
    public sealed record ContentOperationResult(
        int Loaded,
        int Created,
        int Updated,
        int Skipped,
        int Failed,
        TimeSpan Duration)
    {
        public int Processed => Created + Updated + Skipped + Failed;

        public bool HasFailures => Failed > 0;

        public static ContentOperationResult Empty()
        {
            return new ContentOperationResult(0, 0, 0, 0, 0, TimeSpan.Zero);
        }

        public static ContentOperationResult Combine(params ContentOperationResult[] results)
        {
            int loaded = 0;
            int created = 0;
            int updated = 0;
            int skipped = 0;
            int failed = 0;
            TimeSpan duration = TimeSpan.Zero;

            foreach (ContentOperationResult result in results)
            {
                loaded += result.Loaded;
                created += result.Created;
                updated += result.Updated;
                skipped += result.Skipped;
                failed += result.Failed;
                duration += result.Duration;
            }

            return new ContentOperationResult(loaded, created, updated, skipped, failed, duration);
        }
    }
}
