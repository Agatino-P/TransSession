using First.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;
using Second.Contracts.NServiceBus.Commands;

namespace First.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger<TestController> _logger;
    
    public TestController(IMessageSession messageSession,ILogger<TestController> logger)
    {
        _messageSession = messageSession;
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
    public async Task<IActionResult> SendCommand([FromBody]SecondCommandDto secondCommandDto)
    {
        _logger.LogInformation("{Controller}.{Method} was called with {SecondCommand}",
            this.GetType().Name, nameof(SendCommand), secondCommandDto);
        
        SecondCommand secondCommand=new(secondCommandDto.Text,secondCommandDto.Number);
        await _messageSession.Send(secondCommand);        
        return Ok();
    }
}