using System.Diagnostics;

namespace Vanta.Diagnostics;

internal static class StartupTrace
{
    private static readonly string Path = System.IO.Path.Combine(AppContext.BaseDirectory, "vanta-startup.log");

    [Conditional("DEBUG")]
    public static void Mark(string message)
    {
        try
        {
            File.AppendAllText(Path, $"{DateTimeOffset.Now:O}  {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}
