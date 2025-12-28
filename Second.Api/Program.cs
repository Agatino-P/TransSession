using Shared.Database;
using Shared.GateManager;
using Shared.NServiceBus;

namespace Second.Api;

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