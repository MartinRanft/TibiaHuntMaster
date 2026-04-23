using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Services.Analysis
{
    public interface IHuntSessionVerificationService
    {
        Task<HuntSessionVerificationResult> VerifyAsync(HuntSessionEntity session, CancellationToken ct = default);
    }
}
