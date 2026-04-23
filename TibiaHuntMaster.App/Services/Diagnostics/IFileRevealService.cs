using System.Threading;
using System.Threading.Tasks;

namespace TibiaHuntMaster.App.Services.Diagnostics
{
    public interface IFileRevealService
    {
        Task RevealFileAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
