using EntraProbe.Configuration;
using EntraProbe.Execution;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace EntraProbe.Identity;

public sealed class RuntimePlatformPublicClientApplicationFactory : IPublicClientApplicationFactory
{
    public IPublicClientApplication Create(EffectiveOptions options, RuntimePlatform platform)
    {
        return platform switch
        {
            RuntimePlatform.Windows => WindowsPublicClientApplicationFactory.Create(options),
            RuntimePlatform.MacOS => MacOsPublicClientApplicationFactory.Create(options),
            _ => throw new InvalidOperationException("This tool is supported on Windows and macOS only.")
        };
    }

    private static class WindowsPublicClientApplicationFactory
    {
        public static IPublicClientApplication Create(EffectiveOptions options)
        {
            // On Windows, prefer WAM so silent acquisition can reuse the signed-in Entra session without any custom token handling.
            return PublicClientApplicationBuilder
                .Create(options.ClientId!)
                .WithAuthority(AzureCloudInstance.AzurePublic, options.TenantId)
                .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                .WithDefaultRedirectUri()
                .Build();
        }
    }

    private static class MacOsPublicClientApplicationFactory
    {
        public static IPublicClientApplication Create(EffectiveOptions options)
        {
            // The macOS broker relies on Company Portal and an unsigned script-style redirect URI for console executables.
            return PublicClientApplicationBuilder
                .Create(options.ClientId!)
                .WithAuthority(AzureCloudInstance.AzurePublic, options.TenantId)
                .WithRedirectUri(BrokerRedirectUris.MacOsUnsignedBroker)
                .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.OSX)
                {
                    ListOperatingSystemAccounts = true,
                    MsaPassthrough = false,
                    Title = "EntraProbe"
                })
                .Build();
        }
    }
}
