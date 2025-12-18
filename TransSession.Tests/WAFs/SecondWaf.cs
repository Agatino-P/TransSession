using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace TransSession.Tests.WAFs;

public class SecondWaf : BaseWaf<Second.Api.Program>
{

}