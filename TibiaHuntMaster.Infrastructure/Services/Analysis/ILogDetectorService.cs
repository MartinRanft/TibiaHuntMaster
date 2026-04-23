namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    /// <summary>
    ///     Service for detecting the type of hunt log from clipboard text.
    /// </summary>
    public interface ILogDetectorService
    {
        /// <summary>
        ///     Detects the type of hunt log (Solo, Team, or None).
        /// </summary>
        /// <param name="clipboardText">Text from clipboard.</param>
        /// <returns>Detected log type.</returns>
        DetectedLogType DetectType(string clipboardText);
    }
}
