using EntraProbe.Configuration;
using EntraProbe.Execution;
using Microsoft.Identity.Client;
using System.Net.Http;

namespace EntraProbe.Identity;

public sealed class MsalAuthenticationService : IAuthenticationService
{
    private static readonly string[] Scopes = ["User.Read"];
    private readonly IPublicClientApplicationFactory _publicClientApplicationFactory;
    private readonly ISilentAccountResolver _silentAccountResolver;

    public MsalAuthenticationService(
        IPublicClientApplicationFactory publicClientApplicationFactory,
        ISilentAccountResolver silentAccountResolver)
    {
        _publicClientApplicationFactory = publicClientApplicationFactory;
        _silentAccountResolver = silentAccountResolver;
    }

    public async Task<AuthenticationResultData> AcquireAccessTokenAsync(
        EffectiveOptions options,
        ExecutionContextInfo executionContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var app = _publicClientApplicationFactory.Create(options, executionContext.Platform);
            var accounts = await app.GetAccountsAsync().ConfigureAwait(false);
            var account = _silentAccountResolver.Resolve(executionContext.Platform, accounts.ToList());

            if (account is null)
            {
                // The tool must stay non-interactive for managed-platform use, so a cache miss is a terminal auth failure.
                throw new AuthenticationException("Authentication failed: no cached signed-in account is available for silent token acquisition.");
            }

            try
            {
                var silentResult = await app
                    .AcquireTokenSilent(Scopes, account)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new AuthenticationResultData(silentResult.AccessToken);
            }
            catch (MsalUiRequiredException ex)
            {
                throw new AuthenticationException("Authentication failed: silent token acquisition was not possible and interactive prompting is disabled.", ex);
            }
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new AuthenticationException("Authentication failed: network unavailable.", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AuthenticationException("Authentication failed: request timed out.", ex);
        }
        catch (MsalException ex)
        {
            if (HasNetworkFailure(ex))
            {
                throw new AuthenticationException("Authentication failed: network unavailable.", ex);
            }

            throw new AuthenticationException($"Authentication failed: {ex.Message}", ex);
        }
    }

    private static bool HasNetworkFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException)
            {
                return true;
            }
        }

        return false;
    }
}
