using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Entities;
using GOP.Domain.Interfaces.Services;
using GOP.Domain.ValueObjects;

namespace GOP.Infrastructure.Services;

internal sealed class UwiGenerator(IApplicationDbContext dbContext) : IUwiGenerator
{
    public Task<Result<string>> GenerateAsync(Well well, CancellationToken cancellationToken = default)
    {
        // Los códigos DANE ya están desnormalizados en la entidad Well (se guardaron en CreateWell)
        var daneDpto = well.Location.CodigoDaneDpto;
        var daneMpio = well.Location.CodigoDaneMpio;
        var denominacion = well.Denominacion;
        var consecutivo = well.Consecutivo;
        var trayectoria = well.TipoTrayectoria.ToString();

        var uwiResult = Uwi.Create(daneDpto, daneMpio, denominacion, consecutivo, trayectoria);

        if (uwiResult.IsFailure)
            return Task.FromResult(Result.Failure<string>(uwiResult.Error));

        return Task.FromResult(Result.Success(uwiResult.Value.Value));
    }
}
