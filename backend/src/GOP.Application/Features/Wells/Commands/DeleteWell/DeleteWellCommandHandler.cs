using GOP.Application.Common.Interfaces;
using GOP.Domain.Common;
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
        var well = await dbContext.Wells
            .FirstOrDefaultAsync(w => w.Id == request.WellId, cancellationToken);

        if (well is null)
            return Result.Failure(DomainErrors.Well.NotFoundById(request.WellId));

        // RN-40: Guard — usa IsDeletable del dominio
        var deleteResult = well.SoftDelete();
        if (deleteResult.IsFailure)
            return deleteResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
