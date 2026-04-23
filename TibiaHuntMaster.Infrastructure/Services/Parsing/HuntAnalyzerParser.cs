using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Parsing
{
    public sealed partial class HuntAnalyzerParser(ILogger<HuntAnalyzerParser> logger)
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MaxSessionDuration = TimeSpan.FromHours(24);
        private const long MaxAbsoluteXpGain = 1_000_000_000_000;
        private const double XpPerHourDeviationTolerance = 0.15d;
        private static readonly string[] SessionDataHeaders = ["Session data:", "Sitzungsdaten:", "Dane sesji:", "Datos de sesión:", "Dados da sessão:", "Sessionsdata:"];
        private static readonly string[] SessionLabels = ["Session", "Sitzung", "Sesja", "Sesión", "Sessão"];
        private static readonly string[] RawXpGainLabels = ["Raw XP Gain", "Raw XP Gewinn", "Surowy przyrost XP", "Ganancia bruta de XP", "Ganho bruto de XP", "Raw XP-vinst"];
        private static readonly string[] XpGainLabels = ["XP Gain", "XP Gewinn", "Przyrost XP", "Ganancia de XP", "Ganho de XP", "XP-vinst"];
        private static readonly string[] XpPerHourLabels = ["XP/h", "XP pro Stunde", "XP/hora", "XP por hora", "XP/timme"];
        private static readonly string[] LootLabels = ["Loot", "Beute", "Łup", "Botín", "Saque", "Byte"];
        private static readonly string[] SuppliesLabels = ["Supplies", "Vorräte", "Zaopatrzenie", "Suministros", "Suprimentos", "Förnödenheter"];
        private static readonly string[] BalanceLabels = ["Balance", "Bilanz", "Bilans", "Balanço", "Balans"];
        private static readonly string[] DamageLabels = ["Damage", "Schaden", "Obrażenia", "Daño", "Dano", "Skada"];
        private static readonly string[] HealingLabels = ["Healing", "Heilung", "Leczenie", "Curación", "Cura", "Läkning"];
        private static readonly string[] KilledMonstersHeaders = ["Killed Monsters:", "Getötete Monster:", "Zabite potwory:", "Monstruos asesinados:", "Monstros mortos:", "Dödade monster:"];
        private static readonly string[] LootedItemsHeaders = ["Looted Items:", "Erbeutete Gegenstände:", "Zdobyte przedmioty:", "Objetos saqueados:", "Itens saqueados:", "Plundrade föremål:"];

        public bool TryParse(string rawInput, int characterId, out HuntSessionEntity? session, out string error)
        {
            session = null;
            error = string.Empty;

            if(string.IsNullOrWhiteSpace(rawInput))
            {
                error = "Input is empty.";
                return false;
            }

            if(UserInputSanitizer.ExceedsLength(rawInput, UserInputLimits.HuntLogMaxLength))
            {
                error = $"Input is too large (max {UserInputLimits.HuntLogMaxLength} characters).";
                return false;
            }

            string cleanLog = SanitizeInput(rawInput);
            if(string.IsNullOrWhiteSpace(cleanLog))
            {
                error = "Sanitized log is empty (Header not found).";
                return false;
            }

            try
            {
                // Bereinigung
                string parseInput = cleanLog
                                    .Replace('\u00A0', ' ')
                                    .Replace('\t', ' ')
                                    .Replace('–', '-')
                                    .Replace('—', '-');

                // Datum parsen
                DateTimeOffset startTime = DateTimeOffset.UtcNow;
                Match dateMatch = FirstIsoDateRegex().Match(parseInput);

                if(dateMatch.Success)
                {
                    string datePart = dateMatch.Groups["val"].Value;
                    string timePart = dateMatch.Groups["time"].Success ? dateMatch.Groups["time"].Value : "00:00:00";

                    // FIX: DateTimeStyles.None statt AssumeUniversal
                    // Wir parsen das Datum "wie es ist" (Unspecified).
                    if(DateTime.TryParse($"{datePart} {timePart}", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    {
                        // Wir erzwingen UTC Kind, damit DateTimeOffset(..., TimeSpan.Zero) nicht meckert.
                        // Damit speichern wir die Zeit exakt so, wie sie im Log steht, als UTC.
                        dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        startTime = new DateTimeOffset(dt, TimeSpan.Zero);
                    }
                }

                // Werte extrahieren (Dumm & Robust via ExtractXp)
                long xpGain = ExtractXp(parseInput, XpGainLabels);
                long loot = ParseLongByLabels(parseInput, LootLabels);
                TimeSpan duration = ParseDurationByLabels(parseInput, SessionLabels);

                session = new HuntSessionEntity
                {
                    CharacterId = characterId,
                    ImportedAt = DateTimeOffset.UtcNow,
                    SessionStartTime = startTime,
                    RawInput = UserInputSanitizer.Truncate(cleanLog, UserInputLimits.HuntLogMaxLength),

                    Duration = duration,
                    RawXpGain = ParseOptionalLongByLabels(parseInput, RawXpGainLabels),
                    XpGain = xpGain,
                    XpPerHour = ExtractXp(parseInput, XpPerHourLabels),
                    Loot = loot,
                    Supplies = ParseLongByLabels(parseInput, SuppliesLabels),
                    Balance = ParseLongByLabels(parseInput, BalanceLabels),
                    Damage = ParseLongByLabels(parseInput, DamageLabels),
                    Healing = ParseLongByLabels(parseInput, HealingLabels),

                    KilledMonsters = ParseMonstersBlock(parseInput),
                    LootItems = ParseLootBlock(parseInput)
                };

                if(!TryValidatePlausibility(parseInput, session, out error))
                {
                    return false;
                }

                return true;
            }
            catch(RegexMatchTimeoutException ex)
            {
                logger.LogWarning(ex, "Parser regex timeout while processing hunt analyzer input.");
                error = "Parser timed out while processing input.";
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Parser error.");
                error = $"Exception: {ex.Message}";
                return false;
            }
        }

        private long ExtractXp(string input, IReadOnlyList<string> labels)
        {
            foreach(string label in labels)
            {
                string pattern = $@"(?:Raw\s+)?{Regex.Escape(label)}\s*:\s*(?<val>-?[\d,.]+)";
                MatchCollection matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase, RegexTimeout);

                foreach(Match m in matches)
                {
                    if(m.Value.StartsWith("Raw", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return ParseLongValue(m.Groups["val"].Value);
                }
            }

            return 0;
        }

        private long? ParseOptionalLongByLabels(string input, IReadOnlyList<string> labels)
        {
            long value = ParseLongByLabels(input, labels);
            return value == 0 ? null : value;
        }

        private bool TryValidatePlausibility(string input, HuntSessionEntity session, out string error)
        {
            error = string.Empty;

            if(HasExplicitSessionDurationLine(input) && session.Duration <= TimeSpan.Zero)
            {
                error = "Parser extraction failed. Session duration is 0.";
                return false;
            }

            if(session.Duration > MaxSessionDuration)
            {
                error = "Parser plausibility failed. Session duration exceeds 24h.";
                return false;
            }

            bool hasAnyStructuredData =
                session.RawXpGain.HasValue ||
                session.XpGain != 0 ||
                session.XpPerHour != 0 ||
                session.Loot != 0 ||
                session.Supplies != 0 ||
                session.Balance != 0 ||
                session.Damage != 0 ||
                session.Healing != 0 ||
                session.KilledMonsters.Count > 0 ||
                session.LootItems.Count > 0;

            if(!hasAnyStructuredData)
            {
                error = "Parser extraction failed. No measurable hunt data was extracted.";
                return false;
            }

            if(Math.Abs(session.XpGain) > MaxAbsoluteXpGain)
            {
                error = $"Parser plausibility failed. XP Gain exceeds plausible maximum ({MaxAbsoluteXpGain:N0}).";
                return false;
            }

            if(session.RawXpGain.HasValue && Math.Abs(session.RawXpGain.Value) > MaxAbsoluteXpGain)
            {
                error = $"Parser plausibility failed. Raw XP Gain exceeds plausible maximum ({MaxAbsoluteXpGain:N0}).";
                return false;
            }

            if(session.Duration > TimeSpan.Zero && session.XpGain != 0 && session.XpPerHour != 0)
            {
                long calculatedXpPerHour = (long)Math.Round(session.XpGain / session.Duration.TotalHours, MidpointRounding.AwayFromZero);
                long deviation = Math.Abs(calculatedXpPerHour - session.XpPerHour);
                long expectedAbsolute = Math.Max(1L, Math.Abs(calculatedXpPerHour));

                if(deviation > expectedAbsolute * XpPerHourDeviationTolerance)
                {
                    double deviationPercent = deviation * 100d / expectedAbsolute;
                    logger.LogWarning(
                        "Parser plausibility warning. Reported XP/h deviates from XP Gain/Duration. Reported={ReportedXpPerHour} Calculated={CalculatedXpPerHour} DeviationPercent={DeviationPercent:F1}",
                        session.XpPerHour,
                        calculatedXpPerHour,
                        deviationPercent);
                }
            }

            return true;
        }

        private static bool HasExplicitSessionDurationLine(string input)
        {
            foreach(string label in SessionLabels)
            {
                if(Regex.IsMatch(input, $@"^\s*{Regex.Escape(label)}\s*:", RegexOptions.IgnoreCase | RegexOptions.Multiline, RegexTimeout))
                {
                    return true;
                }
            }

            return false;
        }

        private string SanitizeInput(string input)
        {
            int startIndex = FindFirstIndex(input, SessionDataHeaders);
            if (startIndex == -1)
            {
                startIndex = FindFirstIndex(input, SessionLabels);
            }

            if(startIndex == -1)
            {
                return UserInputSanitizer.Truncate(input.Trim(), UserInputLimits.HuntLogMaxLength);
            }

            // Trimmen, um falsche Newlines am Ende zu entfernen (One-Liner Fix)
            string workingText = input.Substring(startIndex).Trim();

            // One-Liner Check
            if(!workingText.Contains('\n') && !workingText.Contains('\r'))
            {
                return UserInputSanitizer.Truncate(workingText, UserInputLimits.HuntLogMaxLength);
            }

            // Multi-Line Logik
            int lootIndex = FindFirstIndex(workingText, LootedItemsHeaders);
            if(lootIndex == -1)
            {
                return UserInputSanitizer.Truncate(workingText, UserInputLimits.HuntLogMaxLength);
            }

            string headerPart = workingText.Substring(0, lootIndex);
            string lootPart = workingText.Substring(lootIndex);

            StringBuilder sb = new(headerPart);
            string[] lines = lootPart.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            foreach(string line in lines)
            {
                string trimmed = line.Trim();
                if(StartsWithAny(trimmed, LootedItemsHeaders) ||
                   LootLineCheckRegex().IsMatch(trimmed))
                {
                    sb.AppendLine(trimmed);
                }
                else
                {
                    break;
                }
            }

            return UserInputSanitizer.Truncate(sb.ToString().Trim(), UserInputLimits.HuntLogMaxLength);
        }

        private List<HuntMonsterEntry> ParseMonstersBlock(string input)
        {
            List<HuntMonsterEntry> list = new();
            string block = ExtractBlock(input, KilledMonstersHeaders, LootedItemsHeaders);
            if(string.IsNullOrEmpty(block))
            {
                return list;
            }

            MatchCollection matches = ListEntryRegex().Matches(block);
            foreach(Match match in matches)
            {
                if(int.TryParse(match.Groups["amount"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
                {
                    string monsterName = UserInputSanitizer.TrimAndTruncate(match.Groups["name"].Value, UserInputLimits.HuntEntryNameMaxLength);
                    if(string.IsNullOrWhiteSpace(monsterName))
                    {
                        continue;
                    }

                    list.Add(new HuntMonsterEntry
                    {
                        Amount = amount,
                        MonsterName = monsterName
                    });
                }
            }
            return list;
        }

        private List<HuntLootEntry> ParseLootBlock(string input)
        {
            List<HuntLootEntry> list = new();
            string block = ExtractBlock(input, LootedItemsHeaders, null);
            if(string.IsNullOrEmpty(block))
            {
                return list;
            }

            MatchCollection matches = ListEntryRegex().Matches(block);
            foreach(Match match in matches)
            {
                if(int.TryParse(match.Groups["amount"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
                {
                    string name = match.Groups["name"].Value.Trim();
                    if(name.StartsWith("a ", StringComparison.OrdinalIgnoreCase))
                    {
                        name = name[2..];
                    }
                    else if(name.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
                    {
                        name = name[3..];
                    }

                    name = UserInputSanitizer.TrimAndTruncate(name, UserInputLimits.HuntEntryNameMaxLength);
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    list.Add(new HuntLootEntry
                    {
                        Amount = amount,
                        ItemName = name
                    });
                }
            }
            return list;
        }

        private string ExtractBlock(string input, IReadOnlyList<string> startKeys, IReadOnlyList<string>? endKeys)
        {
            int startIndex = FindFirstIndex(input, startKeys);
            if(startIndex == -1)
            {
                return string.Empty;
            }

            string startKey = startKeys.FirstOrDefault(k => input.IndexOf(k, StringComparison.OrdinalIgnoreCase) == startIndex) ?? startKeys[0];
            startIndex += startKey.Length;
            int endIndex = -1;
            if(endKeys is { Count: > 0 })
            {
                foreach (string endKey in endKeys)
                {
                    int candidate = input.IndexOf(endKey, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (candidate >= 0 && (endIndex == -1 || candidate < endIndex))
                    {
                        endIndex = candidate;
                    }
                }
            }
            if(endIndex == -1)
            {
                return input.Substring(startIndex);
            }
            return input.Substring(startIndex, endIndex - startIndex);
        }

        private static long ParseLongByLabels(string input, IReadOnlyList<string> labels)
        {
            foreach(string label in labels)
            {
                string pattern = $@"{Regex.Escape(label)}\s*:\s*(?<val>-?[\d,.]+)";
                Match match = Regex.Match(input, pattern, RegexOptions.IgnoreCase, RegexTimeout);
                if(match.Success)
                {
                    return ParseLongValue(match.Groups["val"].Value);
                }
            }

            return 0;
        }

        private static TimeSpan ParseDurationByLabels(string input, IReadOnlyList<string> labels)
        {
            foreach(string label in labels)
            {
                string pattern = $@"{Regex.Escape(label)}\s*:\s*(?<val>\d{{1,2}}:\d{{2}})h?";
                Match match = Regex.Match(input, pattern, RegexOptions.IgnoreCase, RegexTimeout);
                if(match.Success && TryParseDurationValue(match.Groups["val"].Value, out TimeSpan ts))
                {
                    return ts;
                }
            }

            return TimeSpan.Zero;
        }

        private static bool TryParseDurationValue(string value, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;

            string[] parts = value.Split(':', StringSplitOptions.TrimEntries);
            if(parts.Length != 2 ||
               !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours) ||
               !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
            {
                return false;
            }

            if(hours < 0 || minutes is < 0 or > 59)
            {
                return false;
            }

            duration = new TimeSpan(hours, minutes, 0);
            return true;
        }

        private static int FindFirstIndex(string input, IReadOnlyList<string> labels)
        {
            int min = -1;

            foreach (string label in labels)
            {
                int idx = input.IndexOf(label, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && (min == -1 || idx < min))
                {
                    min = idx;
                }
            }

            return min;
        }

        private static bool StartsWithAny(string input, IReadOnlyList<string> labels)
        {
            foreach (string label in labels)
            {
                if (input.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static long ParseLong(Regex regex, string input)
        {
            Match match = regex.Match(input);
            if(!match.Success)
            {
                return 0;
            }
            return ParseLongValue(match.Groups["val"].Value);
        }

        private static long ParseLongValue(string val)
        {
            string clean = val.Replace(",", "").Replace(".", "").Trim();
            if(long.TryParse(clean, NumberStyles.Integer, CultureInfo.InvariantCulture, out long res))
            {
                return res;
            }
            return 0;
        }

        private static TimeSpan ParseDuration(string input) => ParseDurationByLabels(input, SessionLabels);

        // --- REGEX ---

        [GeneratedRegex(@"^\s*\d+x\s+.+", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex LootLineCheckRegex();

        [GeneratedRegex(@"(?<amount>\d+)x\s+(?<name>.+?)(?=\s+\d+x|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 2000)]
        private static partial Regex ListEntryRegex();

        // Wir brauchen keine XP Gain Regexes mehr hier, da ExtractXp das dynamisch macht!

        [GeneratedRegex(@"Loot\s*:\s*(?<val>-?[\d,.]+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex LootRegex();

        [GeneratedRegex(@"Supplies\s*:\s*(?<val>[\d,.]+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex SuppliesRegex();

        [GeneratedRegex(@"Balance\s*:\s*(?<val>-?[\d,.]+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex BalanceRegex();

        [GeneratedRegex(@"Damage\s*:\s*(?<val>[\d,.]+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex DamageRegex();

        [GeneratedRegex(@"Healing\s*:\s*(?<val>[\d,.]+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex HealingRegex();

        [GeneratedRegex(@"Session\s*:\s*(?<val>\d{1,2}:\d{2})h?", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex SessionTimeRegex();

        [GeneratedRegex(@"From\s+(?<val>\d{4}-\d{2}-\d{2})(?:[,]?\s+(?<time>\d{2}:\d{2}:\d{2}))?", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex DateFromRegex();

        [GeneratedRegex(@"(?<val>\d{4}-\d{2}-\d{2})(?:\D+(?<time>\d{2}:\d{2}:\d{2}))?", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex FirstIsoDateRegex();

        // XP Gain Regex für ExtractXp nicht benötigt, da String-Pattern
        [GeneratedRegex(@"(?<!Raw\s+)XP\s+Gain\s*:\s*(?<val>-?[\d,.]+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex XpGainRegex(); // Kann bleiben für Legacy/Fallback, wird aber nicht mehr aktiv genutzt

        [GeneratedRegex(@"(?<!Raw\s+)XP/h\s*:\s*(?<val>-?[\d,.]+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
        private static partial Regex XpPerHourRegex(); // Dito
    }
}
