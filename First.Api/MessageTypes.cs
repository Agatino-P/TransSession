using First.Contracts.NServiceBus.Events;
using Second.Contracts.NServiceBus.Commands;

namespace TransSession.First.Api;

public static class MessageTypes
{
    public static readonly HashSet<Type> Commands = new()
    {
        typeof(SecondCommand),
    };

    public static readonly HashSet<Type> Events = new()
    {
        typeof(FirstEvent),
    };
    
    public static bool IsCommand(this Type type) => Commands.Contains(type);
    public static bool IsEvent(this Type type) => Events.Contains(type);
}