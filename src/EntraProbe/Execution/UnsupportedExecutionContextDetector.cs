namespace EntraProbe.Execution;

public sealed class UnsupportedExecutionContextDetector : IExecutionContextDetector
{
    public ExecutionContextInfo Detect()
    {
        return ExecutionContextEvaluator.Evaluate(new RuntimeEnvironmentInfo(
            RuntimePlatform.Unknown,
            null,
            null,
            false,
            Console.IsInputRedirected,
            false,
            false));
    }
}
