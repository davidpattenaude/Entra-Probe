namespace EntraProbe.Execution;

public sealed record RuntimeEnvironmentInfo(
    RuntimePlatform Platform,
    string? IdentityName,
    string? UserName,
    bool UserInteractive,
    bool InputRedirected,
    bool IsRemoteSession,
    bool HasGraphicalSession);
