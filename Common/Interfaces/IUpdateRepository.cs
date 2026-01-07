using Watchmen.Common.Types;

namespace Watchmen.Common.Interfaces;

public interface IUpdateRepository<T, ID>
{
    ValueTask<Attempt> UpdateAsync(T entity, ID id, CancellationToken cancellationToken = default);
}