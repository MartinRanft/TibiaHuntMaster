using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content.Interfaces
{
    public interface IContentProgressService
    {
        ContentProgressUpdate Current { get; }

        event Action<ContentProgressUpdate>? ProgressChanged;

        void Report(string step, string message, double progressValue, bool isIndeterminate = false);

        void Reset();
    }
}
