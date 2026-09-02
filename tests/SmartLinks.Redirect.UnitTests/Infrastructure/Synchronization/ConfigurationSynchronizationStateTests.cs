using SmartLinks.Redirect.Infrastructure.Synchronization;

namespace SmartLinks.Redirect.UnitTests.Infrastructure.Synchronization;

public sealed class ConfigurationSynchronizationStateTests
{
    /// <summary>
    /// Проверяет переход в состояние готовности после первоначальной синхронизации
    /// </summary>
    [Fact]
    public void MarkReadyChangesStateFromNotReadyToReady()
    {
        var state = new ConfigurationSynchronizationState();

        Assert.False(state.IsReady);

        state.MarkReady();

        Assert.True(state.IsReady);
    }
}