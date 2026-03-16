using EntraProbe.Execution;
using Microsoft.Identity.Client;

namespace EntraProbe.Identity;

public interface ISilentAccountResolver
{
    IAccount? Resolve(RuntimePlatform platform, IReadOnlyList<IAccount> accounts);
}
