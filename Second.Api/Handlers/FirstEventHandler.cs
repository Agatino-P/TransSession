using Shared.Contracts.Events;
using Shared.Database;

namespace Second.Api.Handlers;

public class FirstEventHandler : IHandleMessages<FirstApiEvent>
{
    private readonly ILogger<FirstEventHandler> _logger;
    private readonly PocDbContext _dbContext;

    public FirstEventHandler(ILogger<FirstEventHandler> logger, PocDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
    
    public Task Handle(FirstApiEvent firstApiEvent, IMessageHandlerContext context)
    {
        _logger.LogInformation("{Handler}.{Method} received message: {Message}",
            this.GetType().Name, nameof(Handle),
            firstApiEvent);
        
        return Task.CompletedTask;
    }
}