using Shared.Infrastructure.Contracts.Commands;
using Shared.Infrastructure.Contracts.Events;

namespace First.Api;

public static class MessageTypes
{
    public static readonly HashSet<Type> Commands = new()
    {
        typeof(SecondApiCommand),
    };

    public static readonly HashSet<Type> Events = new()
    {
        typeof(FirstApiEvent),
    };
    
    public static bool IsCommand(this Type type) => Commands.Contains(type);
    public static bool IsEvent(this Type type) => Events.Contains(type);
}