using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TibiaHuntMaster.Infrastructure.Data.Mapper.Helpers
{
    /// <summary>
    ///     Provides functionality to compute a unique hash-based fingerprint for a given <c>CreatureEntity</c> instance.
    /// </summary>
    public class CreatureFingerprint
    {
        /// <summary>
        ///     A static readonly instance of <see cref="JsonSerializerOptions" /> configured for
        ///     producing stable and consistent JSON output during serialization.
        /// </summary>
        /// <remarks>
        ///     The configuration applies the following settings:
        ///     - <see cref="JsonSerializerOptions.WriteIndented" /> is set to <c>false</c>, disabling pretty-printing.
        ///     - <see cref="JsonSerializerOptions.Encoder" /> is set to
        ///     <c>System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping</c>,
        ///     allowing certain characters such as "", ">", and "" to be included unescaped in JSON output.
        ///     This configuration ensures a deterministic and compact JSON representation,
        ///     which is suitable for use cases requiring consistent hashing or comparison operations.
        /// </remarks>
        private static readonly JsonSerializerOptions StableJson = new()
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        ///     Computes a unique fingerprint hash for the specified <see cref="CreatureEntity" /> object.
        ///     The fingerprint is generated based on the serialized JSON representation of the creature's properties.
        /// </summary>
        /// <param name="c">The <see cref="CreatureEntity" /> instance to compute the fingerprint for.</param>
        /// <returns>A string representing the SHA-256 hash of the serialized creature data in hexadecimal format.</returns>
        public static string Compute(CreatureEntity c)
        {
            var payload = new
            {
                c.Name,
                c.ActualName,
                c.Article,
                c.TemplateType,
                c.PrimaryType,
                c.CreatureClass,
                c.IsBoss,
                c.Hp,
                c.Exp,
                c.Armor,
                c.Mitigation,
                c.MaxDmg,
                c.SummonMana,
                c.ConvinceMana,
                c.SenseInvis,
                c.ParaImmune,
                c.Illusionable,
                c.Pushable,
                c.PushObjects,
                c.WalksThrough,
                c.WalksAround,
                c.RunsAt,
                c.Behaviour,
                c.AttackType,
                c.UsedElements,
                c.Location,
                c.Strategy,
                c.Notes,
                c.ImplementedVersion,
                Damage = new
                {
                    c.Damage.PhysicalFactor,
                    c.Damage.FireFactor,
                    c.Damage.IceFactor,
                    c.Damage.EnergyFactor,
                    c.Damage.EarthFactor,
                    c.Damage.HolyFactor,
                    c.Damage.DeathFactor,
                    c.Damage.HpDrainFactor,
                    c.Damage.DrownFactor,
                    c.Damage.HealFactor
                },
                Loot = c.Loot
                        .OrderBy(l => l.ItemName)
                        .ThenBy(l => l.MinAmount).ThenBy(l => l.MaxAmount)
                        .ThenBy(l => l.Rarity)
                        .Select(l => new
                        {
                            l.ItemName,
                            l.MinAmount,
                            l.MaxAmount,
                            l.Rarity
                        }),
                Sounds = c.Sounds.OrderBy(s => s.Text).Select(s => s.Text)
            };

            return ComputeFromPayload(payload);
        }

        public static string ComputeFromPayload(object payload)
        {
            string json = JsonSerializer.Serialize(payload, StableJson);
            using SHA256 sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
