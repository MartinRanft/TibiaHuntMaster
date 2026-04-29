using TibiaHuntMaster.Updater.Core.Abstractions;

namespace TibiaHuntMaster.Updater.Core.Services.Download
{
    public sealed class UpdatePackageDownloader(HttpClient httpClient, IChecksumVerifier checksumVerifier) : IUpdatePackageDownloader
    {
        public async Task<string> DownloadPackageAsync(
            Uri packageUri,
            string targetFilePath,
            string expectedSha256,
            CancellationToken cancellationToken = default)
        {
            string tempFilePath = $"{targetFilePath}.download";

            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);

            using HttpResponseMessage response = await httpClient.GetAsync(
                packageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            byte[] buffer = new byte[81920];

            await using Stream sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream targetStream = new(
                tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, useAsync: true);

            while (true)
            {
                int bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

                if (bytesRead == 0)
                    break;

                await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            await targetStream.FlushAsync(cancellationToken);

            bool checksumValid = await checksumVerifier.VerifyAsync(tempFilePath, expectedSha256, cancellationToken);

            if (!checksumValid)
            {
                File.Delete(tempFilePath);
                throw new InvalidOperationException($"SHA256 checksum mismatch for downloaded package.");
            }

            if (File.Exists(targetFilePath))
                File.Delete(targetFilePath);

            File.Move(tempFilePath, targetFilePath);

            return targetFilePath;
        }
    }
}
