using Watchmen.Common.Types;

namespace Watchmen.Common.Interfaces;

public interface ISoftDeleteRepository<ID>
{
    ValueTask<Attempt> SoftDeleteAsync(ID id, CancellationToken token = default);
    ValueTask<Attempt> RestoreAsync(ID id, CancellationToken token = default);
}