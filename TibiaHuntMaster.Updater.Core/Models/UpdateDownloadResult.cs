namespace TibiaHuntMaster.Updater.Core.Models
{
    public sealed class UpdateDownloadResult
    {
        public required UpdateDownloadStatus Status { get; init; }

        public required UpdatePlan UpdatePlan { get; init; }

        public string? DownloadFilePath { get; init; }

        public string? ErrorMessage { get; init; }
    }
}
