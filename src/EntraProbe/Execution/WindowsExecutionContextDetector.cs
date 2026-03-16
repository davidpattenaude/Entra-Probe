using System.Runtime.Versioning;
using System.Security.Principal;

namespace EntraProbe.Execution;

[SupportedOSPlatform("windows")]
public sealed class WindowsExecutionContextDetector : ExecutionContextDetectorBase
{
    protected override RuntimeEnvironmentInfo BuildRuntimeEnvironment()
    {
        return new RuntimeEnvironmentInfo(
            RuntimePlatform.Windows,
            GetWindowsIdentityName(),
            Environment.UserName,
            Environment.UserInteractive,
            Console.IsInputRedirected,
            HasAnyEnvironmentVariable("SSH_CLIENT", "SSH_CONNECTION", "SSH_TTY"),
            Environment.UserInteractive);
    }

    [SupportedOSPlatform("windows")]
    private static string? GetWindowsIdentityName()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return identity.Name;
    }
}
