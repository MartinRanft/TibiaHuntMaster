namespace TibiaHuntMaster.Infrastructure.Services.Content.Models
{
    public sealed record ContentInitializationResult(
        bool WasInitializationRequired,
        ContentOperationResult Items,
        ContentOperationResult Creatures,
        ContentOperationResult HuntingPlaces)
    {
        public ContentOperationResult Total => ContentOperationResult.Combine(Items, Creatures, HuntingPlaces);

        public static ContentInitializationResult NotRequired()
        {
            ContentOperationResult empty = ContentOperationResult.Empty();
            return new ContentInitializationResult(false, empty, empty, empty);
        }
    }
}
