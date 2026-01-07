using Watchmen.Common.Types;

namespace Watchmen.Common.Interfaces;

public interface IValidatable
{
    Attempt Validate();
}