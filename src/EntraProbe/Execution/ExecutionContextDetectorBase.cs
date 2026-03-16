namespace EntraProbe.Execution;

public abstract class ExecutionContextDetectorBase : IExecutionContextDetector
{
    public ExecutionContextInfo Detect()
    {
        return ExecutionContextEvaluator.Evaluate(BuildRuntimeEnvironment());
    }

    protected abstract RuntimeEnvironmentInfo BuildRuntimeEnvironment();

    protected static bool HasAnyEnvironmentVariable(params string[] names)
    {
        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            {
                return true;
            }
        }

        return false;
    }
}
