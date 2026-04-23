using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Characters
{
    public class TibiaDataCharacterResponse
    {
        [JsonPropertyName("character")]public TibiaDataCharacterContainer Character { get; set; } = new();

        [JsonPropertyName("information")]public TibiaDataInformation Information { get; set; } = new();
    }

    public sealed class TibiaDataCharacterContainer
    {
        [JsonPropertyName("account_badges")]public List<TibiaAccountBadge> AccountBadges { get; set; } = new();

        [JsonPropertyName("account_information")]
        public TibiaAccountInformation? AccountInformation { get; set; }

        [JsonPropertyName("achievements")]public List<TibiaAchievement> Achievements { get; set; } = new();

        [JsonPropertyName("character")]public TibiaCharacterDetails Character { get; set; } = new();

        [JsonPropertyName("deaths")]public List<TibiaDeath> Deaths { get; set; } = new();

        [JsonPropertyName("deaths_truncated")]public bool DeathsTruncated { get; set; }

        [JsonPropertyName("other_characters")]public List<TibiaOtherCharacter> OtherCharacters { get; set; } = new();
    }

    #region Sub-Models

    public class TibiaAccountBadge
    {
        [JsonPropertyName("description")]public string Description { get; set; } = string.Empty;

        [JsonPropertyName("icon_url")]public string IconUrl { get; set; } = string.Empty;

        [JsonPropertyName("name")]public string Name { get; set; } = string.Empty;
    }

    public class TibiaAccountInformation
    {
        [JsonPropertyName("created")]public string Created { get; set; } = string.Empty;

        [JsonPropertyName("loyalty_title")]public string LoyaltyTitle { get; set; } = string.Empty;

        [JsonPropertyName("position")]public string Position { get; set; } = string.Empty;
    }

    public class TibiaAchievement
    {
        [JsonPropertyName("grade")]public int Grade { get; set; }

        [JsonPropertyName("name")]public string Name { get; set; } = string.Empty;

        [JsonPropertyName("secret")]public bool Secret { get; set; }
    }

    public sealed class TibiaCharacterDetails
    {
        [JsonPropertyName("account_status")]public string AccountStatus { get; set; } = string.Empty;

        [JsonPropertyName("achievement_points")]
        public int AchievementPoints { get; set; }

        [JsonPropertyName("comment")]public string Comment { get; set; } = string.Empty;

        [JsonPropertyName("deletion_date")]public string? DeletionDate { get; set; }

        [JsonPropertyName("former_names")]public List<string> FormerNames { get; set; } = new();

        [JsonPropertyName("former_worlds")]public List<string> FormerWorlds { get; set; } = new();

        [JsonPropertyName("guild")]public TibiaGuildInfo? Guild { get; set; }

        [JsonPropertyName("houses")]public List<TibiaHouse> Houses { get; set; } = new();

        [JsonPropertyName("last_login")]public string? LastLogin { get; set; }

        [JsonPropertyName("level")]public int Level { get; set; }

        [JsonPropertyName("married_to")]public string MarriedTo { get; set; } = string.Empty;

        [JsonPropertyName("name")]public string Name { get; set; } = string.Empty;

        [JsonPropertyName("position")]public string Position { get; set; } = string.Empty;

        [JsonPropertyName("residence")]public string Residence { get; set; } = string.Empty;

        [JsonPropertyName("sex")]public string Sex { get; set; } = string.Empty;

        [JsonPropertyName("title")]public string Title { get; set; } = string.Empty;

        [JsonPropertyName("traded")]public bool Traded { get; set; }

        [JsonPropertyName("unlocked_titles")]public int UnlockedTitles { get; set; }

        [JsonPropertyName("vocation")]public string Vocation { get; set; } = string.Empty;

        [JsonPropertyName("world")]public string World { get; set; } = string.Empty;
    }

    public class TibiaGuildInfo
    {
        [JsonPropertyName("name")]public string Name { get; set; } = string.Empty;

        [JsonPropertyName("rank")]public string Rank { get; set; } = string.Empty;
    }

    public class TibiaHouse
    {
        [JsonPropertyName("houseid")]public int HouseId { get; set; }

        [JsonPropertyName("name")]public string Name { get; set; } = string.Empty;

        [JsonPropertyName("paid")]public string Paid { get; set; } = string.Empty;

        [JsonPropertyName("town")]public string Town { get; set; } = string.Empty;
    }

    public class TibiaDeath
    {
        [JsonPropertyName("assists")]public List<TibiaKiller> Assists { get; set; } = new();

        [JsonPropertyName("killers")]public List<TibiaKiller> Killers { get; set; } = new();

        [JsonPropertyName("level")]public int Level { get; set; }

        [JsonPropertyName("reason")]public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("time")]public string Time { get; set; } = string.Empty;
    }

    public class TibiaKiller
    {
        [JsonPropertyName("name")]public string Name { get; set; } = string.Empty;

        [JsonPropertyName("player")]public bool Player { get; set; }

        [JsonPropertyName("summon")]public string Summon { get; set; } = string.Empty;

        [JsonPropertyName("traded")]public bool Traded { get; set; }
    }

    public class TibiaOtherCharacter
    {
        [JsonPropertyName("deleted")]public bool Deleted { get; set; }

        [JsonPropertyName("main")]public bool Main { get; set; }

        [JsonPropertyName("name")]public string Name { get; set; } = string.Empty;

        [JsonPropertyName("position")]public string Position { get; set; } = string.Empty;

        [JsonPropertyName("status")]public string Status { get; set; } = string.Empty;

        [JsonPropertyName("traded")]public bool Traded { get; set; }

        [JsonPropertyName("world")]public string World { get; set; } = string.Empty;
    }

    #endregion

    public sealed class TibiaDataInformation
    {
        [JsonPropertyName("api")]public TibiaDataApi Api { get; set; } = new();

        [JsonPropertyName("status")]public TibiaDataStatus Status { get; set; } = new();

        [JsonPropertyName("tibia_urls")]public List<string> TibiaUrls { get; set; } = new();

        [JsonPropertyName("timestamp")]public string Timestamp { get; set; } = string.Empty;
    }

    public sealed class TibiaDataApi
    {
        [JsonPropertyName("commit")]public string Commit { get; set; } = string.Empty;

        [JsonPropertyName("release")]public string Release { get; set; } = string.Empty;

        [JsonPropertyName("version")]public int Version { get; set; }
    }

    public sealed class TibiaDataStatus
    {
        [JsonPropertyName("error")]public int Error { get; set; }

        [JsonPropertyName("http_code")]public int HttpCode { get; set; }

        [JsonPropertyName("message")]public string Message { get; set; } = string.Empty;
    }
}