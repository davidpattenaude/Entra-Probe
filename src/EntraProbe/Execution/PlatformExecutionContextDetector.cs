namespace EntraProbe.Execution;

public sealed class PlatformExecutionContextDetector : IExecutionContextDetector
{
    private readonly IExecutionContextDetector _innerDetector;

    public PlatformExecutionContextDetector()
    {
        _innerDetector = CreateInnerDetector();
    }

    public ExecutionContextInfo Detect()
    {
        return _innerDetector.Detect();
    }

    private static IExecutionContextDetector CreateInnerDetector()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsExecutionContextDetector();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsExecutionContextDetector();
        }

        return new UnsupportedExecutionContextDetector();
    }
}
