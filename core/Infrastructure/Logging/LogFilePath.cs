namespace Niuro.Core.Infrastructure.Logging;

/// <summary>
/// Resuelve la ruta del archivo de log compartido por API y worker contra la raíz del repo.
/// El API y el worker deben escribir al MISMO archivo rolling (logs/niuro-backend-*.log),
/// sin importar desde qué directorio se lance el proceso (dotnet run usa el cwd del proyecto).
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