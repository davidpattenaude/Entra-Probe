using EntraProbe.Configuration;
using EntraProbe.Graph;

namespace EntraProbe.App;

public interface IProfileOutputFormatter
{
    ProfileOutputSelection Select(EffectiveOptions options, SignedInUserProfile profile);
}
