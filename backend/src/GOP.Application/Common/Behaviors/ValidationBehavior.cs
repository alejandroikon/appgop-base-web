using FluentValidation;
using GOP.Domain.Common;
using MediatR;

namespace GOP.Application.Common.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length != 0)
            return CreateValidationResult<TResponse>(failures);

        return await next(cancellationToken);
    }

    private static TResult CreateValidationResult<TResult>(
        FluentValidation.Results.ValidationFailure[] failures)
        where TResult : Result
    {
        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
        var error = new Error("Validation.Failed", errorMessage);

        if (typeof(TResult) == typeof(Result))
            return (TResult)(object)Result.Failure(error);

        // Result<TValue> — usar reflexión para construir la instancia genérica
        var resultType = typeof(TResult);
        var valueType = resultType.GenericTypeArguments[0];
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
            .MakeGenericMethod(valueType);

        return (TResult)failureMethod.Invoke(null, [error])!;
    }
}
