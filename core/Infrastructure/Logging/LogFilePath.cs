namespace Niuro.Core.Infrastructure.Logging;

/// <summary>
/// Resolves the path of the log file shared by API and worker against the repo root.
/// The API and the worker must write to the SAME rolling file (logs/niuro-backend-*.log),
/// regardless of the directory the process is launched from (dotnet run uses the project cwd).
/// </summary>
public static class LogFilePath
{
    private const string SolutionFileName = "NiuroTest.slnx";

    public static string Resolve(string fileName)
    {
        var repoRoot = FindRepoRoot(AppContext.BaseDirectory) ?? Directory.GetCurrentDirectory();
        var logDir = Path.Combine(repoRoot, "logs");
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, fileName);
    }

    private static string? FindRepoRoot(string? start)
    {
        if (string.IsNullOrWhiteSpace(start))
        {
            return null;
        }

        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}