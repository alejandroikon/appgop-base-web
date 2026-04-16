using System.Diagnostics;
using GOP.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GOP.Application.Common.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUser
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "Handling {RequestName} by User {UserId} Tenant {TenantId}",
            requestName, currentUser.UserId, currentUser.TenantId);

        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        sw.Stop();

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms",
            requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
