namespace TibiaHuntMaster.Updater.Core.Abstractions
{
    public interface IChecksumVerifier
    {
        Task<bool> VerifyAsync(string filePath, string expectedSha256, CancellationToken cancellationToken = default);
    }
}
