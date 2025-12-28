using Shared.Database;
using Shared.GateManager;
using Shared.Nginx;
using Shared.NServiceBus;

namespace First.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);
        
        webApplicationBuilder.Services.AddScoped<TransactionalSessionFilter>();
        webApplicationBuilder.Services.AddControllers(
            o => o.Filters.AddService<TransactionalSessionFilter>()
        );

        webApplicationBuilder.Services.AddEndpointsApiExplorer();
        webApplicationBuilder.Services.AddSwaggerGen();

        webApplicationBuilder.SharedConfigureNServiceBus();
        webApplicationBuilder.SharedAddTransactionalSessionAwarePocDbContext();

        webApplicationBuilder.Services.SharedAddRepositories();
        webApplicationBuilder.SharedAddGateManager();
        
        webApplicationBuilder.SharedAddNginxSettings();
        
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