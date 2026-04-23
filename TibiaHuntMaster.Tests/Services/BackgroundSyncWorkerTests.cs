using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TibiaHuntMaster.Infrastructure.Data;
using TibiaHuntMaster.Infrastructure.Services.Content;
using TibiaHuntMaster.Infrastructure.Services.System;

namespace TibiaHuntMaster.Tests.Services
{
    public sealed class BackgroundSyncWorkerTests
    {
        [Fact]
        public async Task RunOnceAsync_ShouldNotifyCriticalState_WhenUnexpectedExceptionOccurs()
        {
            Mock<IDbContextFactory<AppDbContext>> dbFactoryMock = new();
            dbFactoryMock
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

            BackgroundSyncWorker worker = new(
                dbFactoryMock.Object,
                null!,
                new ContentProgressService(),
                null!,
                null!,
                NullLogger<BackgroundSyncWorker>.Instance);

            (bool IsCritical, bool IsSyncing, bool IsRetry, string Message)? lastState = null;
            worker.OnStateChanged = (isCritical, isSyncing, isRetry, message) =>
            {
                lastState = (isCritical, isSyncing, isRetry, message);
            };

            await worker.RunOnceAsync();

            lastState.Should().NotBeNull();
            lastState!.Value.IsCritical.Should().BeTrue();
            lastState.Value.IsSyncing.Should().BeFalse();
            lastState.Value.IsRetry.Should().BeTrue();
            lastState.Value.Message.Should().Contain("failed");
        }
    }
}
