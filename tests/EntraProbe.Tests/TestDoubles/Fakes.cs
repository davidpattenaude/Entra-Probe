using EntraProbe.App;
using EntraProbe.Configuration;
using EntraProbe.Execution;
using EntraProbe.Graph;
using EntraProbe.Identity;

namespace EntraProbe.Tests;

public sealed class FakeExecutionContextDetector : IExecutionContextDetector
{
    public ExecutionContextInfo Context { get; set; } = new(RuntimePlatform.Windows, true, false, true, true, null);

    public ExecutionContextInfo Detect() => Context;
}

public sealed class FakeAuthenticationService : IAuthenticationService
{
    public AuthenticationResultData Result { get; set; } = new("token");

    public Exception? ExceptionToThrow { get; set; }

    public Task<AuthenticationResultData> AcquireAccessTokenAsync(
        EffectiveOptions options,
        ExecutionContextInfo executionContext,
        CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(Result);
    }
}

public sealed class FakeGraphProfileService : IGraphProfileService
{
    public SignedInUserProfile Profile { get; set; } = new("Finance", "OU=Users,DC=contoso,DC=com");

    public Exception? ExceptionToThrow { get; set; }

    public Task<SignedInUserProfile> GetProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(Profile);
    }
}

public sealed class FakeProfileOutputFormatter : IProfileOutputFormatter
{
    public ProfileOutputSelection Selection { get; set; } = ProfileOutputSelection.Present("Finance");

    public ProfileOutputSelection Select(EffectiveOptions options, SignedInUserProfile profile) => Selection;
}

public static class TestOptions
{
    public static EffectiveOptions Create(
        IReadOnlyList<OutputProperty>? properties = null,
        bool verbose = false,
        bool showHelp = false,
        string? tenantId = null,
        string? clientId = null)
    {
        return new EffectiveOptions(
            tenantId ?? Guid.NewGuid().ToString(),
            clientId ?? Guid.NewGuid().ToString(),
            properties ?? [OutputProperty.Department],
            verbose,
            showHelp);
    }
}

public static class TestApplicationRunner
{
    public static ApplicationRunner Create(
        RecordingConsole console,
        FakeExecutionContextDetector? contextDetector = null,
        FakeAuthenticationService? authService = null,
        FakeGraphProfileService? graphService = null,
        IProfileOutputFormatter? formatter = null)
    {
        return new ApplicationRunner(
            contextDetector ?? new FakeExecutionContextDetector(),
            authService ?? new FakeAuthenticationService(),
            graphService ?? new FakeGraphProfileService(),
            formatter ?? new ProfileOutputFormatter(),
            console);
    }
}
