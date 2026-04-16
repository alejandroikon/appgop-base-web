using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
using GOP.Domain.Enums;
using GOP.Domain.Errors;
using GOP.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GOP.Application.Features.Wells.Commands.DeleteWell;

internal sealed class DeleteWellCommandHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteWellCommand, Result>
{
    public async Task<Result> Handle(
        DeleteWellCommand request, CancellationToken cancellationToken)
    {
        // 1. Buscar el pozo
        var well = await dbContext.Wells
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure(DomainErrors.Well.NotFoundById(request.WellId));

        // 2. Verificar que está en estado Borrador
        if (well.Estado != WellStatus.Borrador)
            return Result.Failure(DomainErrors.Well.DeleteInvalidStatus);

        // 3. Soft delete
        well.IsDeleted = true;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
