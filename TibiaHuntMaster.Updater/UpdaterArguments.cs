namespace TibiaHuntMaster.Updater
{
    internal sealed class UpdaterArguments
    {
        internal int WaitForPid { get; private init; }
        internal string Package { get; private init; } = string.Empty;
        internal string RestartExecutable { get; private init; } = string.Empty;
        internal string UpdateCompletedVersion { get; private init; } = string.Empty;
        internal string? ReleasePageUrl { get; private init; }

        internal static bool TryParse(string[] args, out UpdaterArguments? result)
        {
            result = null;

            string? waitForPidRaw = GetValue(args, "--wait-for-pid");
            string? package = GetValue(args, "--package");
            string? restartExecutable = GetValue(args, "--restart-executable");
            string? updateCompletedVersion = GetValue(args, "--update-completed-version");
            string? releasePageUrl = GetValue(args, "--update-completed-release-page-url");

            if (string.IsNullOrWhiteSpace(waitForPidRaw)
                || !int.TryParse(waitForPidRaw, out int waitForPid)
                || string.IsNullOrWhiteSpace(package)
                || string.IsNullOrWhiteSpace(restartExecutable)
                || string.IsNullOrWhiteSpace(updateCompletedVersion))
            {
                return false;
            }

            result = new UpdaterArguments
            {
                WaitForPid = waitForPid,
                Package = package,
                RestartExecutable = restartExecutable,
                UpdateCompletedVersion = updateCompletedVersion,
                ReleasePageUrl = releasePageUrl
            };

            return true;
        }

        private static string? GetValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }
    }
}
