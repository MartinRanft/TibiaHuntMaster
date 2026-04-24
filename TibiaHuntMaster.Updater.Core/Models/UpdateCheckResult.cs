namespace TibiaHuntMaster.Updater.Core.Models
{
    public sealed class UpdateCheckResult
    {
        public required UpdateCheckStatus Status { get; init; }

        public required string CurrentVersion { get; init; }

        public string? LatestVersion { get; init; }

        public UpdatePlan? UpdatePlan { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
