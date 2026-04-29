using System.Diagnostics;

namespace TibiaHuntMaster.Updater
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (!UpdaterArguments.TryParse(args, out UpdaterArguments? arguments) || arguments is null)
            {
                await Console.Error.WriteLineAsync("Invalid arguments.");
                return UpdaterExitCodes.InvalidArguments;
            }

            try
            {
                Process process = Process.GetProcessById(arguments.WaitForPid);
                await process.WaitForExitAsync();
            }
            catch (ArgumentException )
            {
                //Process Ended already so its fine
            }
            
            if(OperatingSystem.IsWindows())
            {
                using Process installer = new();
                installer.StartInfo.FileName = arguments.Package;
                installer.StartInfo.Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART";
                installer.StartInfo.UseShellExecute = false;
                installer.Start();
                await installer.WaitForExitAsync();
            }
            else if(OperatingSystem.IsLinux())
            {
                // Copy instead of Move to handle cross-filesystem paths (e.g. /tmp vs /home).
                // File.Copy with overwrite:true works even when the destination was previously deleted.
                File.Copy(arguments.Package, arguments.RestartExecutable, overwrite: true);
                File.SetUnixFileMode(arguments.RestartExecutable, UnixFileMode.UserRead | UnixFileMode.UserWrite |
                                                                  UnixFileMode.UserExecute | UnixFileMode.GroupRead |
                                                                  UnixFileMode.GroupExecute | UnixFileMode.OtherRead |
                                                                  UnixFileMode.OtherExecute);
                try { File.Delete(arguments.Package); } catch { }
            } 
            else if(OperatingSystem.IsMacOS())
            {
                string bundleRoot = Path.GetFullPath(Path.Combine(arguments.RestartExecutable, "..", "..", ".."));
                string appParentDir = Path.GetDirectoryName(bundleRoot)!;
                string appBundleName = Path.GetFileName(bundleRoot);
                string mountPoint = string.Empty;

                try
                {
                    using Process attachProcess = new();
                    attachProcess.StartInfo.FileName = "hdiutil";
                    attachProcess.StartInfo.ArgumentList.Add("attach");
                    attachProcess.StartInfo.ArgumentList.Add(arguments.Package);
                    attachProcess.StartInfo.ArgumentList.Add("-nobrowse");
                    attachProcess.StartInfo.ArgumentList.Add("-noautoopen");
                    attachProcess.StartInfo.UseShellExecute = false;
                    attachProcess.StartInfo.RedirectStandardOutput = true;
                    attachProcess.Start();

                    string output = await attachProcess.StandardOutput.ReadToEndAsync();
                    await attachProcess.WaitForExitAsync();

                    foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
                            mountPoint = parts[2].Trim();
                    }

                    if (string.IsNullOrEmpty(mountPoint))
                        return UpdaterExitCodes.ApplyFailed;

                    string sourceApp = Path.Combine(mountPoint, appBundleName);

                    if (Directory.Exists(bundleRoot))
                        Directory.Delete(bundleRoot, recursive: true);

                    using Process copyProcess = new();
                    copyProcess.StartInfo.FileName = "cp";
                    copyProcess.StartInfo.ArgumentList.Add("-R");
                    copyProcess.StartInfo.ArgumentList.Add(sourceApp);
                    copyProcess.StartInfo.ArgumentList.Add(appParentDir);
                    copyProcess.StartInfo.UseShellExecute = false;
                    copyProcess.Start();
                    await copyProcess.WaitForExitAsync();
                }
                finally
                {
                    if (!string.IsNullOrEmpty(mountPoint))
                    {
                        using Process detachProcess = new();
                        detachProcess.StartInfo.FileName = "hdiutil";
                        detachProcess.StartInfo.ArgumentList.Add("detach");
                        detachProcess.StartInfo.ArgumentList.Add(mountPoint);
                        detachProcess.StartInfo.UseShellExecute = false;
                        detachProcess.Start();
                        await detachProcess.WaitForExitAsync();
                    }
                }
            }
            
            ProcessStartInfo restartInfo = new(arguments.RestartExecutable);                                                                                                                                                   
            restartInfo.ArgumentList.Add("--update-completed-version");
            restartInfo.ArgumentList.Add(arguments.UpdateCompletedVersion);                                                                                                                                                    
                                                                                                                                                                                                                     
            if (!string.IsNullOrWhiteSpace(arguments.ReleasePageUrl))
            {                                                                                                                                                                                                                  
                restartInfo.ArgumentList.Add("--update-completed-release-page-url");
                restartInfo.ArgumentList.Add(arguments.ReleasePageUrl);                                                                                                                                                        
            }
            
            Process.Start(restartInfo);
            return UpdaterExitCodes.Success;
        }
    }
}