namespace EntraProbe.Execution;

public sealed record ExecutionContextInfo(
    RuntimePlatform Platform,
    bool IsSupported,
    bool IsSystem,
    bool IsInteractive,
    bool CanPrompt,
    string? Message);
