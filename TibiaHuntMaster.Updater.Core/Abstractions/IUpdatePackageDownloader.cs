namespace TibiaHuntMaster.Updater.Core.Abstractions
{
    public interface IUpdatePackageDownloader
    {
        Task<string> DownloadPackageAsync(
            Uri packageUri,
            string targetFilePath,
            string expectedSha256,
            CancellationToken cancellationToken = default);
    }
}
