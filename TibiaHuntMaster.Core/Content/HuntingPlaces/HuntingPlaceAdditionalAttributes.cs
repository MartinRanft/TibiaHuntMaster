#nullable enable
using System.Text.Json.Serialization;

namespace TibiaHuntMaster.Core.Content.HuntingPlaces
{
    /// <summary>
    /// DTO representing the AdditionalAttributesJson structure for hunting places.
    /// Contains lower level areas and other additional data.
    /// </summary>
    public sealed class HuntingPlaceAdditionalAttributes
    {
        [JsonPropertyName("LowerLevels")]
        public List<HuntingPlaceLowerLevel>? LowerLevels { get; set; }
    }
}
