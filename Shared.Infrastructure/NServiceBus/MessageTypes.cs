using First.Contracts.NServiceBus.Events;
using Second.Contracts.NServiceBus.Commands;

namespace Shared.Infrastructure.NServiceBus;

public static class MessageTypes
{
    private static readonly HashSet<Type> Commands = new()
    {
        typeof(SecondCommand),
    };

    private static readonly HashSet<Type> Events = new()
    {
        typeof(FirstEvent),
    };
    
    public static bool IsCommand(this Type type) => Commands.Contains(type);
    public static bool IsEvent(this Type type) => Events.Contains(type);
}