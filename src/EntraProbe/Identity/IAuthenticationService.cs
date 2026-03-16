using EntraProbe.Configuration;
using EntraProbe.Execution;

namespace EntraProbe.Identity;

public interface IAuthenticationService
{
    Task<AuthenticationResultData> AcquireAccessTokenAsync(
        EffectiveOptions options,
        ExecutionContextInfo executionContext,
        CancellationToken cancellationToken);
}
