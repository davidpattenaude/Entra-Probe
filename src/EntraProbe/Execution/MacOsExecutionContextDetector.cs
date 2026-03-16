namespace EntraProbe.Execution;

public sealed class MacOsExecutionContextDetector : ExecutionContextDetectorBase
{
    protected override RuntimeEnvironmentInfo BuildRuntimeEnvironment()
    {
        var isRemoteSession = HasAnyEnvironmentVariable("SSH_CLIENT", "SSH_CONNECTION", "SSH_TTY");

        return new RuntimeEnvironmentInfo(
            RuntimePlatform.MacOS,
            Environment.UserName,
            Environment.UserName,
            !Console.IsInputRedirected,
            Console.IsInputRedirected,
            isRemoteSession,
            !isRemoteSession);
    }
}
