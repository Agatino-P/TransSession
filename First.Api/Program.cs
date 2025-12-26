using Shared.Infrastructure.Database;
using Shared.Infrastructure.GateManager;
using Shared.Infrastructure.NServiceBus;

namespace First.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);
        webApplicationBuilder.Services.AddControllers();
        webApplicationBuilder.Services.AddEndpointsApiExplorer();
        webApplicationBuilder.Services.AddSwaggerGen();

        webApplicationBuilder.SharedConfigureNServiceBus();
        webApplicationBuilder.SharedAddDbContext();

        webApplicationBuilder.Services.SharedAddRepositories();
        webApplicationBuilder.SharedAddGateManager();
        
        WebApplication app = webApplicationBuilder.Build();
        
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();

        await app.EnsurePocDbCreatedAsync();
        await app.RunAsync();
    }
}