namespace EntraProbe.Execution;

public static class ExecutionContextEvaluator
{
    public static ExecutionContextInfo Evaluate(RuntimeEnvironmentInfo environment)
    {
        if (environment.Platform == RuntimePlatform.Unknown)
        {
            return new ExecutionContextInfo(environment.Platform, false, false, false, false, "This tool is supported on Windows and macOS only.");
        }

        var isSystem = environment.Platform == RuntimePlatform.Windows
            && string.Equals(environment.IdentityName, @"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase);
        var hasUserIdentity = !string.IsNullOrWhiteSpace(environment.UserName);
        var isInteractive = environment.UserInteractive && hasUserIdentity;
        // The current tool is silent-only, but keeping promptability in the model makes future auth-mode changes explicit.
        var canPrompt = isInteractive
            && !isSystem
            && !environment.InputRedirected
            && !environment.IsRemoteSession
            && environment.HasGraphicalSession;

        if (isSystem)
        {
            return new ExecutionContextInfo(environment.Platform, false, true, isInteractive, false, "This tool must run in an interactive user session, not as SYSTEM.");
        }

        if (!isInteractive)
        {
            return new ExecutionContextInfo(environment.Platform, false, false, false, false, "This tool must run in an interactive user session.");
        }

        return new ExecutionContextInfo(environment.Platform, true, false, true, canPrompt, null);
    }
}
