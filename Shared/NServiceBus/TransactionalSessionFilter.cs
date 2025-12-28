using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.TransactionalSession;

namespace Shared.NServiceBus;

public sealed class TransactionalSessionFilter : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var transactionalSession = context.HttpContext.RequestServices.GetRequiredService<ITransactionalSession>();

        await transactionalSession.Open(new SqlPersistenceOpenSessionOptions());

        var executed = await next();

        if (executed.Exception is null)
        {
            await transactionalSession.Commit();
        }
    }
}