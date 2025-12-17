using Second.Contracts.NServiceBus.Commands;

namespace Second.Api.Handlers;

public class SecondCommandHandler : IHandleMessages<SecondCommand>
{
    private readonly ILogger<SecondCommandHandler> _logger;

    public SecondCommandHandler(ILogger<SecondCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SecondCommand secondCommand, IMessageHandlerContext context)
    {
        _logger.LogInformation("{Handler}.{Method} received message: {Message}",
            this.GetType().Name, nameof(Handle),
            secondCommand);

        return Task.CompletedTask;
    }
}