namespace Shared.GateManager;

public static class TaskCompletionSourceExtensions
{
    public static void CompleteSuccessfully(this TaskCompletionSource taskCompletionSource)
    {
        taskCompletionSource.TrySetResult();
    }
}