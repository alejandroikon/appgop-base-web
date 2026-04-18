using GOP.Domain.Common;
using GOP.Domain.Entities;
using GOP.Domain.Interfaces.Services;

namespace GOP.Infrastructure.Services;

// TODO-ITER-9.3: Este servicio es legacy (V1). La generación de UWI V2.0 se hace directamente
// en los handlers usando el Value Object Uwi.Generate() del Domain. Mantener solo para que
// IUwiGenerator compile mientras TransitionWell esté activo.
internal sealed class UwiGenerator : IUwiGenerator
{
    public Task<Result<string>> GenerateAsync(Well well, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<string>(new Error(
            "UwiGenerator.Legacy",
            "Use el método Uwi.Generate() del Domain directamente en los handlers V2.0.")));
    }
}
