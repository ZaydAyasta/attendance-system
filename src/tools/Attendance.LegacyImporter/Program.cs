using Attendance.LegacyImporter;

namespace Attendance.LegacyImporter;

public static class LegacyImporterEntryPoint
{
    public static Task<int> Main(string[] args)
        => LegacyImporterProgram.RunAsync(args, Console.Out, CancellationToken.None);
}
