using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Contracts.Commands;
using Shared.Infrastructure.Contracts.Dtos;
using Shared.Infrastructure.Database.Entities;
using Shared.Infrastructure.Database.Repository;
using Shared.Infrastructure.GateManager;

namespace First.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly IMessageSession _messageSession;
    private readonly IPocLogEntryRepository _pocLogEntryRepository;
    private readonly IGateManager _gateManager;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IMessageSession messageSession,
        IPocLogEntryRepository pocLogEntryRepository,
        IGateManager gateManager,
        ILogger<TestController> logger)
    {
        _messageSession = messageSession;
        _pocLogEntryRepository = pocLogEntryRepository;
        _gateManager = gateManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("{Controller}.{Method} was called",
            this.GetType().Name, nameof(Get));
        return Ok();
    }

    [HttpPost]
    [Route("Command")]
    public async Task<IActionResult> SendCommand([FromBody] FirstApiSendCommandDto firstApiSendCommandDto)
    {
        _logger.LogInformation("{Controller}.{Method} was called with {Command}",
            this.GetType().Name, nameof(SendCommand), firstApiSendCommandDto);

        SecondApiCommand secondApiCommand = new(firstApiSendCommandDto.Text, firstApiSendCommandDto.Number);
        await _messageSession.Send(secondApiCommand);

        string commandAsString = secondApiCommand.ToString();
        await _pocLogEntryRepository.AddEntry(LogEntryType.CommandSent, commandAsString);

        return Ok(commandAsString);
    }

    [HttpPost]
    [Route("Pause")]
    public async Task<IActionResult> Pause([FromBody] FirstApiPauseDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Controller}.{Method} was called with {Dto}",
            this.GetType().Name, nameof(Pause), dto);

        await _pocLogEntryRepository.AddEntry(LogEntryType.RestCallReceived, dto.Text);
        
        await _gateManager.GateReached(IGateManager.BeforeDoingWorkGate, cancellationToken);
        
        await _pocLogEntryRepository.AddEntry(LogEntryType.EntryAdded, "Controller - Gate Released");

        return Ok(dto.Text);
    }

    
}