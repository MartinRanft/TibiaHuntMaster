using System.Security.Cryptography;
using TibiaHuntMaster.Updater.Core.Abstractions;

namespace TibiaHuntMaster.Updater.Core.Services.Packages
{
    public sealed class ChecksumVerifier : IChecksumVerifier
    {
        public async Task<bool> VerifyAsync(string filePath, string expectedSha256, CancellationToken cancellationToken = default)
        {
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
            byte[] hashBytes = await SHA256.HashDataAsync(fileStream, cancellationToken);
            string actualHash = Convert.ToHexStringLower(hashBytes);

            return string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase);
        }
    }
}
