namespace TibiaHuntMaster.Updater.Core.Abstractions
{
    public interface IUpdatePackageExtractor
    {
        Task<string> ExtractAsync(string archiveFilePath, string targetDirectory, CancellationToken cancellationToken = default);
    }
}
