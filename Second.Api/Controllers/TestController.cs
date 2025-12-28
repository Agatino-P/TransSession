using Microsoft.AspNetCore.Mvc;
using Shared.Database.Entities;
using Shared.Database.Repository;
using Shared.GateManager;

namespace Second.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly IPocLogEntryRepository _pocLogEntryRepository;
    private readonly IGateManager _gateManager;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IPocLogEntryRepository pocLogEntryRepository,
        IGateManager gateManager,
        ILogger<TestController> logger)
    {
        _pocLogEntryRepository = pocLogEntryRepository;
        _gateManager = gateManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
    
    [HttpGet]
    [Route("SayWhenDone")]
    public async Task<IActionResult> SayWhenDone(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Controller}.{Method} was called",
            this.GetType().Name, nameof(SayWhenDone));

        await _pocLogEntryRepository.AddEntry(LogEntryType.RestCallReceived, nameof(SayWhenDone));
        
        await _gateManager.GateReached(IGateManager.SecondApiSayWhenDoneGate, cancellationToken);
        
        return Ok();
    }
}