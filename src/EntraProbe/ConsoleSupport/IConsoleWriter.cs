namespace EntraProbe.ConsoleSupport;

public interface IConsoleWriter
{
    void WriteOutput(string value);

    void WriteError(string value);

    void WriteVerbose(bool enabled, string value);
}
