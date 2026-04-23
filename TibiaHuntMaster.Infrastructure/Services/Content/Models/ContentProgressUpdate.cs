namespace TibiaHuntMaster.Infrastructure.Services.Content.Models
{
    public sealed record ContentProgressUpdate(
        string Step,
        string Message,
        double ProgressValue,
        bool IsIndeterminate);
}
