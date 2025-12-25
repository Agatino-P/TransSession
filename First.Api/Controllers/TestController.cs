using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Contracts.Commands;
using Shared.Infrastructure.Contracts.Dtos;
using Shared.Infrastructure.Database.Entities;
using Shared.Infrastructure.Database.Repository;

namespace First.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly IMessageSession _messageSession;
    private readonly IPocLogEntryRepository _pocLogEntryRepository;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IMessageSession messageSession,
        IPocLogEntryRepository pocLogEntryRepository,
        ILogger<TestController> logger)
    {
        _messageSession = messageSession;
        _pocLogEntryRepository = pocLogEntryRepository;
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
        _logger.LogInformation("{Controller}.{Method} was called with {SecondCommand}",
            this.GetType().Name, nameof(SendCommand), firstApiSendCommandDto);

        SecondApiCommand secondApiCommand = new(firstApiSendCommandDto.Text, firstApiSendCommandDto.Number);
        await _messageSession.Send(secondApiCommand);

        string commandAsString=secondApiCommand.ToString();
        await _pocLogEntryRepository.AddEntry(LogEntryType.CommandSent, commandAsString);
        
        return Ok(commandAsString);
    }
}