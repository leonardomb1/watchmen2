using Watchmen.Common.Types;

namespace Watchmen.Common.Interfaces;

public interface IReadRepository<T, ID>
{
    ValueTask<Result<T>> GetByIdAsync(ID id, CancellationToken cancellationToken = default);
    ValueTask<Result<PagedQuery<T>>> ListAllAsync(IQueryCollection? query, CancellationToken cancellationToken = default);
}