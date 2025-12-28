using Shared.Contracts.Commands;
using Shared.Database.Entities;
using Shared.Database.Repository;

namespace Second.Api.Handlers;

public class SecondCommandHandler : IHandleMessages<SecondApiCommand>
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

    public async Task Handle(SecondApiCommand secondApiCommand, IMessageHandlerContext context)
    {
        _logger.LogInformation("{Handler}.{Method} received message: {Message}",
            this.GetType().Name, nameof(Handle),
            secondApiCommand);

        string commandAsString=secondApiCommand.ToString();
        await _pocLogEntryRepository.AddEntry(LogEntryType.CommandReceived, commandAsString);
    }
}