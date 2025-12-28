using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Contracts.Commands;
using Shared.Contracts.Dtos;
using Shared.Contracts.Events;
using Shared.Database.Entities;
using Shared.Database.Repository;
using Shared.GateManager;
using Shared.Nginx;

namespace First.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly IMessageSession _messageSession;
    private readonly IPocLogEntryRepository _pocLogEntryRepository;
    private readonly IGateManager _gateManager;
    private readonly NginxSettings _nginxSettings;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IMessageSession messageSession,
        IPocLogEntryRepository pocLogEntryRepository,
        IGateManager gateManager,
        IOptions<NginxSettings> nginxSettingsOptions,
        ILogger<TestController> logger)
    {
        _messageSession = messageSession;
        _pocLogEntryRepository = pocLogEntryRepository;
        _gateManager = gateManager;
        _nginxSettings = nginxSettingsOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("{Controller}.{Method} was called",
            this.GetType().Name, nameof(Get));
        
        await _pocLogEntryRepository.AddEntry(LogEntryType.RestCallReceived, nameof(Get));
        
        await _messageSession.Publish(new FirstApiEvent("Hello from TestController", DateTime.UtcNow));

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

    [HttpGet]
    [Route("SayWhenDone")]
    public async Task<IActionResult> SayWhenDone(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Controller}.{Method} was called",
            this.GetType().Name, nameof(SayWhenDone));

        await _pocLogEntryRepository.AddEntry(LogEntryType.RestCallReceived, nameof(SayWhenDone));

        await _gateManager.GateReached(IGateManager.FistApiSayWhenDoneGate, cancellationToken);

        return Ok();
    }

    [HttpPost]
    [Route("CanFail")]
    public async Task<IActionResult> CanFail([FromBody] FirstApiCanFailDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Controller}.{Method} was called with {Dto}",
            this.GetType().Name, nameof(CanFail), dto);

        await _pocLogEntryRepository.AddEntry(LogEntryType.RestCallReceived,
            $"{nameof(TestController)}.{nameof(CanFail)}, Dto: {dto}");

        await _pocLogEntryRepository.AddEntry(LogEntryType.AppGateReached, IGateManager.BeforeDoingWorkGate);
        await _gateManager.GateReached(IGateManager.BeforeDoingWorkGate, cancellationToken);

        using HttpClient httpClient = new() { BaseAddress = new Uri(_nginxSettings.BaseAddress) };
        HttpResponseMessage response = await httpClient.GetAsync("/", cancellationToken);
        response.EnsureSuccessStatusCode();

        await _pocLogEntryRepository.AddEntry(LogEntryType.RestCallCompleted, nameof(CanFail));

        return Ok(dto.Text);
    }
}