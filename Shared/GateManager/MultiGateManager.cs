using System.Collections.Concurrent;

namespace Shared.Infrastructure.GateManager;

public sealed class MultiGateManager : IGateManager
{
    private sealed class Gate
    {
        public TaskCompletionSource ReachedCompletionSource { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleasedCompletionSource { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly ConcurrentDictionary<string, Gate> _gates = new();

    private Gate Get(string name)
    {
        return _gates.GetOrAdd(name, _ => new Gate());
    }

    public async Task GateReached(string name, CancellationToken cancellationToken)
    {
        Gate gate = Get(name);

        gate.ReachedCompletionSource.CompleteSuccessfully();
        // Signal that execution has reached this gate

        await gate.ReleasedCompletionSource.Task.WaitAsync(cancellationToken);
        // Block until the test explicitly releases this gate
    }

    public Task WaitUntilReached(string name, CancellationToken cancellationToken = default)
    {
        return Get(name).ReachedCompletionSource.Task.WaitAsync(cancellationToken);
    }

    public void ReleaseGate(string name)
    {
        Get(name).ReleasedCompletionSource.CompleteSuccessfully();
    }
}