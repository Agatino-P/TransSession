using First.Contracts.NServiceBus.Events;
using Second.Contracts.NServiceBus.Commands;

namespace Second.Api.Handlers;

public class FirstEventHandler : IHandleMessages<FirstEvent>
{
    public Task Handle(FirstEvent firstEvent, IMessageHandlerContext context)
    {
        Console.Write(firstEvent);
        return Task.CompletedTask;
    }
}

public class SecondCommandHandler : IHandleMessages<SecondCommand>
{
    public Task Handle(SecondCommand secondCommand, IMessageHandlerContext context)
    {
        Console.Write(secondCommand);
        return Task.CompletedTask;
    }
}