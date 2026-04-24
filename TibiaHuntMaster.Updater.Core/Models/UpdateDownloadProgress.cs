namespace TibiaHuntMaster.Updater.Core.Models
{
    public sealed class UpdateDownloadProgress
    {
        public required long BytesReceived { get; init; }
        public long? TotalBytes { get; init; }

        public double? Percentage =>
        TotalBytes is > 0
        ? BytesReceived * 100d / TotalBytes.Value
        : null;
    }
}