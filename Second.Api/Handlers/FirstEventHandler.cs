using First.Contracts.NServiceBus.Events;

namespace Second.Api.Handlers;

public class FirstEventHandler : IHandleMessages<FirstEvent>
{
    private readonly ILogger<FirstEventHandler> _logger;

    public FirstEventHandler(ILogger<FirstEventHandler> logger)
    {
        _logger = logger;
    }
    
    public Task Handle(FirstEvent firstEvent, IMessageHandlerContext context)
    {
        _logger.LogInformation("{Handler}.{Method} received message: {Message}",
            this.GetType().Name, nameof(Handle),
            firstEvent);
        
        return Task.CompletedTask;
    }
}