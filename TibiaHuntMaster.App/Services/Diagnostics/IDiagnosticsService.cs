namespace TibiaHuntMaster.App.Services.Diagnostics
{
    public sealed record DiagnosticsExportResult(string ArchivePath, int LogFilesIncluded, int CrashFilesIncluded);

    public interface IDiagnosticsService
    {
        string LogsDirectory { get; }

        string DiagnosticsExportsDirectory { get; }

        void CaptureExceptionReport(Exception exception, string source, bool isTerminating = false);

        Task<DiagnosticsExportResult> ExportDiagnosticsAsync(CancellationToken cancellationToken = default);
    }
}
