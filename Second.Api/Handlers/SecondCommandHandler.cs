using Second.Contracts.NServiceBus.Commands;
using Shared.Infrastructure.Database.Entities;
using Shared.Infrastructure.Database.Repository;

namespace Second.Api.Handlers;

public class SecondCommandHandler : IHandleMessages<SecondCommand>
{
    private readonly IPocLogEntryRepository _pocLogEntryRepository;
    private readonly ILogger<SecondCommandHandler> _logger;

    public SecondCommandHandler(
        IPocLogEntryRepository _pocLogEntryRepository,
        ILogger<SecondCommandHandler> logger)
    {
        this._pocLogEntryRepository = _pocLogEntryRepository;
        _logger = logger;
    }

    public async Task Handle(SecondCommand secondCommand, IMessageHandlerContext context)
    {
        _logger.LogInformation("{Handler}.{Method} received message: {Message}",
            this.GetType().Name, nameof(Handle),
            secondCommand);

        string commandAsString=secondCommand.ToString();
        await _pocLogEntryRepository.AddEntry(LogEntryType.CommandReceived, commandAsString);
    }
}