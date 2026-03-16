namespace EntraProbe.ConsoleSupport;

public sealed class SystemConsole : IConsoleWriter
{
    public void WriteOutput(string value)
    {
        Console.Out.Write(value);
    }

    public void WriteError(string value)
    {
        Console.Error.WriteLine(value);
    }

    public void WriteVerbose(bool enabled, string value)
    {
        if (enabled)
        {
            WriteError(value);
        }
    }
}
