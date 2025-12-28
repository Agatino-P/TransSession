using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.GateManager;

public static class GateManagerExtensions
{
    public static WebApplicationBuilder SharedAddGateManager(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddSingleton<IGateManager, NoOpGateManager>();

        return webApplicationBuilder;
    }
}