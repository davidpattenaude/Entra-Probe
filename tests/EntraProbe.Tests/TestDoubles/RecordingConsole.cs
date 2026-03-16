using EntraProbe.ConsoleSupport;
using System.Text;

namespace EntraProbe.Tests;

public sealed class RecordingConsole : IConsoleWriter
{
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();

    public string StandardOutput => _stdout.ToString();

    public string StandardError => _stderr.ToString();

    public void WriteOutput(string value)
    {
        _stdout.Append(value);
    }

    public void WriteError(string value)
    {
        _stderr.AppendLine(value);
    }

    public void WriteVerbose(bool enabled, string value)
    {
        if (enabled)
        {
            WriteError(value);
        }
    }
}
