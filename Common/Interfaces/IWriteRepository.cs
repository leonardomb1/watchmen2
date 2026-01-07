using Watchmen.Common.Types;

namespace Watchmen.Common.Interfaces;

public interface IWriteRepository<T, ID>
{
    ValueTask<Result<ID>> WriteAsync(T entity, CancellationToken cancellationToken = default);
}