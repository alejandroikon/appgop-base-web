using GOP.Domain.Common;
using GOP.Domain.Entities;

namespace GOP.Domain.Interfaces.Services;

public interface IUwiGenerator
{
    Task<Result<string>> GenerateAsync(Well well, CancellationToken cancellationToken = default);
}
