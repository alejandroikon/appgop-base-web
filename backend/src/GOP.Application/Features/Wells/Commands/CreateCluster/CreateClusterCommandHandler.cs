using GOP.Application.Common.Interfaces;
using GOP.Application.Features.Catalogs.Queries.GetClusters;
using GOP.Domain.Common;
using GOP.Domain.Entities;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Commands.CreateCluster;

internal sealed class CreateClusterCommandHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateClusterCommand, Result<ClusterItemDto>>
{
    public async Task<Result<ClusterItemDto>> Handle(
        CreateClusterCommand request, CancellationToken cancellationToken)
    {
        // Verificar que el campo existe
        var campoExists = await dbContext.Campos
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CampoId, cancellationToken);

        if (!campoExists)
            return Result.Failure<ClusterItemDto>(new Error("Cluster.CampoNotFound",
                "El campo especificado no existe."));

        // Verificar unicidad del nombre en el campo
        var duplicate = await dbContext.Clusters
            .AsNoTracking()
            .AnyAsync(c => c.CampoId == request.CampoId &&
                           c.Nombre.ToLower() == request.Nombre.ToLower(), cancellationToken);

        if (duplicate)
            return Result.Failure<ClusterItemDto>(DomainErrors.Cluster.DuplicateInCampo);

        // Generar abreviatura: 2 primeras letras del nombre (solo alfabéticas, MAYÚSCULAS)
        var abreviatura = GenerateAbreviatura(request.Nombre);

        var cluster = new Cluster
        {
            Nombre = request.Nombre.Trim(),
            Abreviatura = abreviatura,
            CampoId = request.CampoId
        };

        dbContext.Clusters.Add(cluster);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new GOP.Application.Features.Catalogs.Queries.GetClusters.ClusterItemDto(
            Id: cluster.Id,
            Nombre: cluster.Nombre,
            Abreviatura: cluster.Abreviatura,
            CampoId: cluster.CampoId
        ));
    }

    /// <summary>
    /// Genera la abreviatura de 2 chars tomando la primera letra de cada palabra significativa.
    /// Ej: "Locación A" → "LA", "Cluster Norte" → "CN", "Pad Sur" → "PS".
    /// </summary>
    private static string GenerateAbreviatura(string nombre)
    {
        var words = nombre.Split(' ', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        var letters = new System.Text.StringBuilder(2);
        foreach (var word in words)
        {
            var firstLetter = word.FirstOrDefault(char.IsLetter);
            if (firstLetter != '\0')
            {
                letters.Append(char.ToUpperInvariant(firstLetter));
                if (letters.Length == 2)
                    break;
            }
        }
        return letters.ToString().PadRight(2, 'X')[..2];
    }
}
