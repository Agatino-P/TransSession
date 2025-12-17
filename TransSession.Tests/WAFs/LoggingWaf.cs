using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace TransSession.Tests.WAFs;

public abstract class LoggingWaf<T> : WebApplicationFactory<T>
    where T : class
{
    public HttpClient CreateClientWithXunitLogging(ITestOutputHelper output, LogLevel minLevel = LogLevel.Information)
        => this.WithWebHostBuilder(webHostBuilder =>
                webHostBuilder.ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddXUnit(output);
                    loggingBuilder.AddConsole();
                    loggingBuilder.SetMinimumLevel(minLevel);
                }))
            .CreateClient();

}