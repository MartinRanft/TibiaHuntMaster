namespace TibiaHuntMaster.Infrastructure.Services.Content.Models
{
    public sealed record ContentReInitializationResult(
        ContentOperationResult Items,
        ContentOperationResult Creatures,
        ContentOperationResult HuntingPlaces)
    {
        public ContentOperationResult Total => ContentOperationResult.Combine(Items, Creatures, HuntingPlaces);
    }
}
