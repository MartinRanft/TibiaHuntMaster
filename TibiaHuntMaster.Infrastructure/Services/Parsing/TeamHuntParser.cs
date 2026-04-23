using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using TibiaHuntMaster.Core.Security;
using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Parsing
{
    public sealed partial class TeamHuntParser(ILogger<TeamHuntParser> logger)
    {
        private const int RegexTimeoutMilliseconds = 2000;
        private static readonly string[] SessionDataHeaders = ["Session data:", "Sitzungsdaten:", "Dane sesji:", "Datos de sesión:", "Dados da sessão:", "Sessionsdata:"];
        private static readonly string[] SessionLabels = ["Session", "Sitzung", "Sesja", "Sesión", "Sessão"];
        private static readonly string[] LootTypeLabels = ["Loot Type", "Beutetyp", "Typ łupu", "Tipo de botín", "Tipo de saque", "Bytestyp"];
        private static readonly string[] SoloIndicators = ["Killed Monsters:", "Looted Items:", "Getötete Monster:", "Erbeutete Gegenstände:", "Zabite potwory:", "Zdobyte przedmioty:", "Monstruos asesinados:", "Objetos saqueados:", "Monstros mortos:", "Itens saqueados:", "Dödade monster:", "Plundrade föremål:"];
        private static readonly string[] LeaderSuffixes = ["(Leader)", "(Anführer)", "(Dowódca)", "(Líder)", "(Lider)", "(Ledare)"];

        public bool TryParse(string rawInput, int characterId, out TeamHuntSessionEntity? session, out string error)
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

            // --- CRITICAL FIX ---
            // Ein Team-Log enthält NIEMALS "Killed Monsters" oder "Looted Items".
            // Wenn wir das finden, ist es garantiert ein Solo Log -> Abbruch.
            if(ContainsAny(rawInput, SoloIndicators))
            {
                error = "Input contains Solo-Hunt keywords (Monsters/Items).";
                return false;
            }
            // --------------------

            try
            {
                string cleanLog = rawInput
                                  .Replace('\u00A0', ' ')
                                  .Replace('\t', ' ')
                                  .Replace('–', '-')
                                  .Replace('—', '-');

                session = new TeamHuntSessionEntity
                {
                    CharacterId = characterId,
                    ImportedAt = DateTimeOffset.UtcNow,
                    RawInput = UserInputSanitizer.Truncate(rawInput, UserInputLimits.HuntLogMaxLength)
                };

                Match dateMatch = FirstIsoDateRegex().Match(cleanLog);
                if(dateMatch.Success)
                {
                    string d = dateMatch.Groups["val"].Value;
                    string t = dateMatch.Groups["time"].Success ? dateMatch.Groups["time"].Value : "00:00:00";
                    if(DateTime.TryParse($"{d} {t}", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    {
                        dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        session.SessionStartTime = new DateTimeOffset(dt, TimeSpan.Zero);
                    }
                }

                string[] lines = cleanLog.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                TeamHuntMemberEntity? currentMember = null;
                bool hasRecognizedStats = false;

                foreach(string line in lines)
                {
                    string trimmed = line.Trim();
                    if(string.IsNullOrWhiteSpace(trimmed))
                    {
                        continue;
                    }

                    // Sicherheits-Check: Zeilen die mit Zahlen beginnen (z.B. "12x Gold") sind keine Spielernamen
                    if(char.IsDigit(trimmed[0]))
                    {
                        continue;
                    }

                    if(StartsWithAny(trimmed, SessionDataHeaders))
                    {
                        continue;
                    }

                    if(StartsWithAny(trimmed, SessionLabels))
                    {
                        session.Duration = ParseDuration(trimmed);
                        continue;
                    }

                    if(TryExtractAfterLabel(trimmed, LootTypeLabels, out string? lootType))
                    {
                        session.LootType = UserInputSanitizer.TrimAndTruncate(lootType, UserInputLimits.LootTypeMaxLength);
                        continue;
                    }

                    Match statMatch = StatRegex().Match(trimmed);

                    if(statMatch.Success)
                    {
                        string key = statMatch.Groups["key"].Value.Trim();
                        string valStr = statMatch.Groups["val"].Value;
                        long value = ParseLongValue(valStr);
                        TeamStatType statType = NormalizeStatKey(key);
                        if (statType != TeamStatType.Unknown)
                        {
                            hasRecognizedStats = true;
                        }

                        if(currentMember == null)
                        {
                            if(statType == TeamStatType.Loot)
                            {
                                session.TotalLoot = value;
                            }
                            else if(statType == TeamStatType.Supplies)
                            {
                                session.TotalSupplies = value;
                            }
                            else if(statType == TeamStatType.Balance)
                            {
                                session.TotalBalance = value;
                            }
                        }
                        else
                        {
                            if(statType == TeamStatType.Loot)
                            {
                                currentMember.Loot = value;
                            }
                            else if(statType == TeamStatType.Supplies)
                            {
                                currentMember.Supplies = value;
                            }
                            else if(statType == TeamStatType.Balance)
                            {
                                currentMember.Balance = value;
                            }
                            else if(statType == TeamStatType.Damage)
                            {
                                currentMember.Damage = value;
                            }
                            else if(statType == TeamStatType.Healing)
                            {
                                currentMember.Healing = value;
                            }
                        }
                    }
                    else
                    {
                        // Neuer Spielername gefunden
                        currentMember = new TeamHuntMemberEntity();
                        string rawName = trimmed;

                        if(EndsWithAny(rawName, LeaderSuffixes, out string? matchedSuffix))
                        {
                            currentMember.IsLeader = true;
                            currentMember.Name = UserInputSanitizer.TrimAndTruncate(rawName[..^matchedSuffix!.Length], UserInputLimits.TeamMemberNameMaxLength);
                        }
                        else
                        {
                            currentMember.IsLeader = false;
                            currentMember.Name = UserInputSanitizer.TrimAndTruncate(rawName, UserInputLimits.TeamMemberNameMaxLength);
                        }

                        if(string.IsNullOrWhiteSpace(currentMember.Name))
                        {
                            currentMember = null;
                            continue;
                        }

                        session.Members.Add(currentMember);
                    }
                }

                if(session.Members.Count == 0)
                {
                    error = "No party members found.";
                    return false;
                }

                if (!hasRecognizedStats)
                {
                    error = "No recognized team hunt stats found.";
                    return false;
                }

                return true;
            }
            catch(RegexMatchTimeoutException ex)
            {
                logger.LogWarning(ex, "Parser regex timeout while processing team hunt input.");
                error = "Parser timed out while processing input.";
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse Team Hunt");
                error = ex.Message;
                return false;
            }
        }

        private enum TeamStatType
        {
            Unknown,
            Loot,
            Supplies,
            Balance,
            Damage,
            Healing
        }

        private static TeamStatType NormalizeStatKey(string rawKey)
        {
            string key = rawKey.Trim().ToLowerInvariant();

            if (ContainsAnyToken(key, "loot", "beute", "łup", "botín", "botin", "saque", "byte"))
            {
                return TeamStatType.Loot;
            }

            if (ContainsAnyToken(key, "supplies", "vorräte", "vorrate", "zaopatrzenie", "suministros", "suprimentos", "förnödenheter", "fornodenheter"))
            {
                return TeamStatType.Supplies;
            }

            if (ContainsAnyToken(key, "balance", "bilanz", "bilans", "balanço", "balanco", "balans"))
            {
                return TeamStatType.Balance;
            }

            if (ContainsAnyToken(key, "damage", "schaden", "obrażenia", "obrazenia", "daño", "dano", "skada"))
            {
                return TeamStatType.Damage;
            }

            if (ContainsAnyToken(key, "healing", "heilung", "leczenie", "curación", "curacion", "cura", "läkning", "lakning"))
            {
                return TeamStatType.Healing;
            }

            return TeamStatType.Unknown;
        }

        private static bool ContainsAnyToken(string source, params string[] tokens)
        {
            foreach (string token in tokens)
            {
                if (source.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool StartsWithAny(string source, IReadOnlyList<string> labels)
        {
            foreach (string label in labels)
            {
                if (source.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsAny(string source, IReadOnlyList<string> labels)
        {
            foreach (string label in labels)
            {
                if (source.Contains(label, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EndsWithAny(string source, IReadOnlyList<string> suffixes, out string? matchedSuffix)
        {
            foreach (string suffix in suffixes)
            {
                if (source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    matchedSuffix = suffix;
                    return true;
                }
            }

            matchedSuffix = null;
            return false;
        }

        private static bool TryExtractAfterLabel(string line, IReadOnlyList<string> labels, out string? value)
        {
            foreach (string label in labels)
            {
                if (!line.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string rest = line[label.Length..].TrimStart();
                if (rest.StartsWith(":", StringComparison.Ordinal))
                {
                    rest = rest[1..];
                }

                value = rest.Trim();
                return true;
            }

            value = null;
            return false;
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

        private static TimeSpan ParseDuration(string line)
        {
            Match match = SessionTimeRegex().Match(line);
            if(match.Success && TimeSpan.TryParse(match.Groups["val"].Value, CultureInfo.InvariantCulture, out TimeSpan ts))
            {
                return ts;
            }
            return TimeSpan.Zero;
        }

        [GeneratedRegex(@"(?<key>[^:]+):\s*(?<val>-?[\d,.]+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
        private static partial Regex StatRegex();

        [GeneratedRegex(@"(?<val>\d{1,2}:\d{2})h?", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
        private static partial Regex SessionTimeRegex();

        [GeneratedRegex(@"From\s+(?<val>\d{4}-\d{2}-\d{2})(?:[,]?\s+(?<time>\d{2}:\d{2}:\d{2}))?", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
        private static partial Regex DateFromRegex();

        [GeneratedRegex(@"(?<val>\d{4}-\d{2}-\d{2})(?:\D+(?<time>\d{2}:\d{2}:\d{2}))?", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMilliseconds)]
        private static partial Regex FirstIsoDateRegex();
    }
}
