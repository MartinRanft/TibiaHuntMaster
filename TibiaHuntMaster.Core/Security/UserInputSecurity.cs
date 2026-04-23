namespace TibiaHuntMaster.Core.Security
{
    public static class UserInputLimits
    {
        public const int CharacterNameMaxLength = 64;
        public const int GoalTitleMaxLength = 100;
        public const int HuntEntryNameMaxLength = 100;
        public const int TeamMemberNameMaxLength = 100;
        public const int HuntAdjustmentNameMaxLength = 100;
        public const int LootTypeMaxLength = 64;
        public const int SessionNotesMaxLength = 4000;
        public const int HuntLogMaxLength = 300_000;
        public const int CoordinateInputMaxLength = 64;
    }

    public static class UserInputSanitizer
    {
        public static bool ExceedsLength(string? input, int maxLength)
        {
            if(maxLength <= 0)
            {
                return !string.IsNullOrEmpty(input);
            }

            return !string.IsNullOrEmpty(input) && input.Length > maxLength;
        }

        public static string Truncate(string? input, int maxLength)
        {
            if(string.IsNullOrEmpty(input) || maxLength <= 0)
            {
                return string.Empty;
            }

            return input.Length <= maxLength ? input : input[..maxLength];
        }

        public static string TrimAndTruncate(string? input, int maxLength)
        {
            if(string.IsNullOrWhiteSpace(input) || maxLength <= 0)
            {
                return string.Empty;
            }

            string trimmed = input.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        public static string? TrimAndTruncateOrNull(string? input, int maxLength)
        {
            if(string.IsNullOrWhiteSpace(input) || maxLength <= 0)
            {
                return null;
            }

            string trimmed = input.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }
    }
}
