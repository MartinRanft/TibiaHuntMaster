namespace TibiaHuntMaster.App.Services.Changelog
{
    internal interface IChangelogService
    {
        Task<string?> GetChangelogSectionAsync(string version, string? releasePageUrl, CancellationToken cancellationToken = default);
    }
}
