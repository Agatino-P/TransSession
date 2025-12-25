using Shared.Infrastructure.Contracts.Events;

namespace Second.Api.Handlers;

public class FirstEventHandler : IHandleMessages<FirstApiEvent>
{
    private readonly ILogger<FirstEventHandler> _logger;

    public FirstEventHandler(ILogger<FirstEventHandler> logger)
    {
        _logger = logger;
    }
    
    public Task Handle(FirstApiEvent firstApiEvent, IMessageHandlerContext context)
    {
        _logger.LogInformation("{Handler}.{Method} received message: {Message}",
            this.GetType().Name, nameof(Handle),
            firstApiEvent);
        
        return Task.CompletedTask;
    }
}