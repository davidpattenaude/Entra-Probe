using EntraProbe.Execution;

namespace EntraProbe.Tests.Execution;

[TestClass]
public sealed class ExecutionContextInfoTests
{
    [TestMethod]
    public void Evaluate_ReturnsUnsupportedForSystem()
    {
        var result = ExecutionContextEvaluator.Evaluate(new RuntimeEnvironmentInfo(
            RuntimePlatform.Windows,
            @"NT AUTHORITY\SYSTEM",
            "SYSTEM",
            true,
            false,
            false,
            true));

        Assert.IsFalse(result.IsSupported);
        Assert.IsTrue(result.IsSystem);
        Assert.IsFalse(result.CanPrompt);
        Assert.AreEqual("This tool must run in an interactive user session, not as SYSTEM.", result.Message);
    }

    [TestMethod]
    public void Evaluate_ReturnsUnsupportedForNonInteractiveSession()
    {
        var result = ExecutionContextEvaluator.Evaluate(new RuntimeEnvironmentInfo(
            RuntimePlatform.Windows,
            @"CONTOSO\User",
            "User",
            false,
            false,
            false,
            true));

        Assert.IsFalse(result.IsSupported);
        Assert.IsFalse(result.IsSystem);
        Assert.IsFalse(result.IsInteractive);
        Assert.IsFalse(result.CanPrompt);
    }

    [TestMethod]
    public void Evaluate_ReturnsSupportedForInteractiveWindowsUser()
    {
        var result = ExecutionContextEvaluator.Evaluate(new RuntimeEnvironmentInfo(
            RuntimePlatform.Windows,
            @"CONTOSO\User",
            "User",
            true,
            false,
            false,
            true));

        Assert.IsTrue(result.IsSupported);
        Assert.IsTrue(result.IsInteractive);
        Assert.IsTrue(result.CanPrompt);
    }

    [TestMethod]
    public void Evaluate_ReturnsSupportedForInteractiveMacOsUser()
    {
        var result = ExecutionContextEvaluator.Evaluate(new RuntimeEnvironmentInfo(
            RuntimePlatform.MacOS,
            "alice",
            "alice",
            true,
            false,
            false,
            true));

        Assert.IsTrue(result.IsSupported);
        Assert.IsTrue(result.IsInteractive);
        Assert.IsTrue(result.CanPrompt);
    }

    [TestMethod]
    public void Evaluate_ReturnsUnsupportedForUnknownPlatform()
    {
        var result = ExecutionContextEvaluator.Evaluate(new RuntimeEnvironmentInfo(
            RuntimePlatform.Unknown,
            "alice",
            "alice",
            true,
            false,
            false,
            true));

        Assert.IsFalse(result.IsSupported);
        Assert.IsFalse(result.CanPrompt);
        Assert.AreEqual("This tool is supported on Windows and macOS only.", result.Message);
    }
}
