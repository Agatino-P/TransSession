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
        return Ok();
    }
    
    [HttpPost]
    [Route("Command")]
    public async Task<IActionResult> Post([FromBody]SecondCommandDto secondCommand)
    {
        SecondCommand command=new(secondCommand.Text,secondCommand.Number);
        await _messageSession.Send(command);        
        return Ok();
    }
}