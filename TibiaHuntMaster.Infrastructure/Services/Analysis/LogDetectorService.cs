using TibiaHuntMaster.Infrastructure.Services.Parsing;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public enum DetectedLogType
    {
        None,
        SoloHunt,
        TeamHunt
    }

    public sealed class LogDetectorService(HuntAnalyzerParser soloParser, TeamHuntParser teamParser) : ILogDetectorService
    {
        public DetectedLogType DetectType(string clipboardText)
        {
            if(string.IsNullOrWhiteSpace(clipboardText))
            {
                return DetectedLogType.None;
            }

            // Versuch Team Parser (strikt)
            // UPDATE: Signatur angepasst (Dummy ID 0, Error verworfen)
            if(teamParser.TryParse(clipboardText, 0, out _, out _))
            {
                return DetectedLogType.TeamHunt;
            }

            // Versuch Solo Parser 
            // UPDATE: Signatur angepasst (Error verworfen)
            if(soloParser.TryParse(clipboardText, 0, out _, out _))
            {
                return DetectedLogType.SoloHunt;
            }

            return DetectedLogType.None;
        }
    }
}
