using EntraProbe.Execution;
using Microsoft.Identity.Client;

namespace EntraProbe.Identity;

public sealed class SilentAccountResolver : ISilentAccountResolver
{
    public IAccount? Resolve(RuntimePlatform platform, IReadOnlyList<IAccount> accounts)
    {
        if (platform == RuntimePlatform.Windows)
        {
            // Use the current Windows OS account rather than an arbitrary cache entry so silent auth stays tied to the interactive user.
            return PublicClientApplication.OperatingSystemAccount;
        }

        if (platform == RuntimePlatform.MacOS)
        {
            return accounts.Count switch
            {
                0 => null,
                1 => accounts[0],
                _ => throw new AuthenticationException("Authentication failed: multiple cached signed-in accounts are available and the correct macOS account could not be selected silently.")
            };
        }

        return null;
    }
}
