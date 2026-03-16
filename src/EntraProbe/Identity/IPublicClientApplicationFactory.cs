using EntraProbe.Configuration;
using EntraProbe.Execution;
using Microsoft.Identity.Client;

namespace EntraProbe.Identity;

public interface IPublicClientApplicationFactory
{
    IPublicClientApplication Create(EffectiveOptions options, RuntimePlatform platform);
}
