using Shared.Contracts.Events;
using Shared.Database.Entities;
using Shared.Database.Repository;

namespace Second.Api.Handlers;

public class FirstApiEventHandler : IHandleMessages<FirstApiEvent>
{
    private readonly ILogger<FirstApiEventHandler> _logger;
    private readonly IPocLogEntryRepository _repository;

    public FirstApiEventHandler(ILogger<FirstApiEventHandler> logger, IPocLogEntryRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }
    
    public async Task Handle(FirstApiEvent firstApiEvent, IMessageHandlerContext context)
    {
        _logger.LogInformation("{Handler}.{Method} received message: {Message}",
            this.GetType().Name, nameof(Handle),
            firstApiEvent);

        await _repository.AddEntry(LogEntryType.EventReceived, firstApiEvent.ToString());
        
       throw new Exception("Simulating failure to test Transactional Session rollback");
        
        await _repository.AddEntry(LogEntryType.EventCompleted, firstApiEvent.ToString());
    }
}