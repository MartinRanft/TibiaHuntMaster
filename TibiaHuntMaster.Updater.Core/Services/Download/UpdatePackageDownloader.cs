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
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (IOException)
                {
                    // Previous temp file is locked (e.g. AV scan); fall back to a unique name.
                    tempFilePath = $"{targetFilePath}.{Guid.NewGuid():N}.download";
                }
            }

            using HttpResponseMessage response = await httpClient.GetAsync(
                packageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            byte[] buffer = new byte[81920];

            // Scope the streams so they are disposed before VerifyAsync and File.Move run.
            // On Windows, FileShare.None holds an exclusive lock until the FileStream is
            // actually disposed, which would otherwise make those follow-up steps fail with
            // "the process cannot access the file because it is used by another process".
            await using (Stream sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (FileStream targetStream = new(
                tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, useAsync: true))
            {
                while (true)
                {
                    int bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

                    if (bytesRead == 0)
                        break;

                    await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }

                await targetStream.FlushAsync(cancellationToken);
            }

            bool checksumValid = await checksumVerifier.VerifyAsync(tempFilePath, expectedSha256, cancellationToken);

            if (!checksumValid)
            {
                TryDelete(tempFilePath);
                throw new InvalidOperationException($"SHA256 checksum mismatch for downloaded package.");
            }

            if (File.Exists(targetFilePath))
                await DeleteWithRetryAsync(targetFilePath, cancellationToken);

            await MoveWithRetryAsync(tempFilePath, targetFilePath, cancellationToken);

            return targetFilePath;
        }

        // Windows AV/SmartScreen may briefly hold the file even after the stream is
        // disposed. Retry a handful of times with backoff before giving up.
        private static async Task MoveWithRetryAsync(string source, string destination, CancellationToken cancellationToken)
        {
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    File.Move(source, destination);
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    await Task.Delay(200 * attempt, cancellationToken);
                }
            }
        }

        private static async Task DeleteWithRetryAsync(string path, CancellationToken cancellationToken)
        {
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    await Task.Delay(200 * attempt, cancellationToken);
                }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // Best-effort cleanup; the leftover file will be reused or overwritten on the next attempt.
            }
        }
    }
}
