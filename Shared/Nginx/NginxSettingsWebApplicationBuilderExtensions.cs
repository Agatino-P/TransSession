using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Nginx;

public static class NginxSettingsWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder SharedAddNginxSettings(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<NginxSettings>()
            .Bind(builder.Configuration.GetSection("Nginx"))
            .Validate(s => !string.IsNullOrWhiteSpace(s.BaseAddress),
                $"{nameof(NginxSettings)}.{nameof(NginxSettings.BaseAddress)} must be configured.")
            .ValidateOnStart();

        return builder;
    }
}