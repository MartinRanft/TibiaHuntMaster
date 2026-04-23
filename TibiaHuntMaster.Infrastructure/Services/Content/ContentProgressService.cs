using TibiaHuntMaster.Infrastructure.Services.Content.Interfaces;
using TibiaHuntMaster.Infrastructure.Services.Content.Models;

namespace TibiaHuntMaster.Infrastructure.Services.Content
{
    public sealed class ContentProgressService : IContentProgressService
    {
        private readonly object _sync = new();
        private ContentProgressUpdate _current = new(string.Empty, string.Empty, 0, true);

        public ContentProgressUpdate Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public event Action<ContentProgressUpdate>? ProgressChanged;

        public void Report(string step, string message, double progressValue, bool isIndeterminate = false)
        {
            ContentProgressUpdate update = new(step, message, progressValue, isIndeterminate);

            lock (_sync)
            {
                _current = update;
            }

            ProgressChanged?.Invoke(update);
        }

        public void Reset()
        {
            Report(string.Empty, string.Empty, 0, true);
        }
    }
}
