using Watchmen.Common.Types;

namespace Watchmen.Common.Interfaces;

public interface IDeleteRepository<ID>
{
    ValueTask<Attempt> DeleteAsync(ID id, CancellationToken token = default);
}