using System.Text;

namespace TibiaHuntMaster.App.Services.Changelog
{
    internal sealed class ChangelogService(HttpClient httpClient) : IChangelogService
    {
        public async Task<string?> GetChangelogSectionAsync(string version, string? releasePageUrl, CancellationToken cancellationToken = default)
        {
            string? rawUrl = BuildRawChangelogUrl(releasePageUrl);
            if (rawUrl is null)
                return null;

            string markdown = await httpClient.GetStringAsync(rawUrl, cancellationToken);
            return ParseVersionSection(markdown, version);
        }

        private static string? BuildRawChangelogUrl(string? releasePageUrl)
        {
            if (string.IsNullOrWhiteSpace(releasePageUrl))
                return null;

            try
            {
                Uri uri = new(releasePageUrl);
                string[] parts = uri.AbsolutePath.Trim('/').Split('/');
                if (parts.Length < 5)
                    return null;

                string owner = parts[0];
                string repo = parts[1];
                string tag = parts[4];

                return $"https://raw.githubusercontent.com/{owner}/{repo}/{tag}/CHANGELOG.md";
            }
            catch
            {
                return null;
            }
        }

        private static string? ParseVersionSection(string markdown, string version)
        {
            string[] lines = markdown.Split('\n');
            bool inSection = false;
            StringBuilder sb = new();

            foreach (string line in lines)
            {
                string trimmed = line.TrimStart();

                if (trimmed.StartsWith($"## [{version}]", StringComparison.OrdinalIgnoreCase))
                {
                    inSection = true;
                    sb.AppendLine(line);
                    continue;
                }

                if (inSection && trimmed.StartsWith("## [", StringComparison.Ordinal))
                    break;

                if (inSection)
                    sb.AppendLine(line);
            }

            string result = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
    }
}
