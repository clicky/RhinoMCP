using System;
using System.IO;
using System.Reflection;

namespace Rhino.AI;

// A plugin-owned CODEX_HOME, which is what keeps the user's own ~/.codex/config.toml and its MCP servers out of the Rhino agent.
internal static class CodexHome
{
    private const string ConfigResourceName = "Rhino.AI.codex-config.toml";

    private static string? Prepared { get; set; }

    public static string Prepare()
    {
        if (Prepared is string ready)
            return ready;

        string home = RhinoApp.GetDataDirectory(
            localUser: true,
            forceDirectoryCreation: true,
            subDirectory: Path.Combine("RhinoAI", "CodexHome"));

        Directory.CreateDirectory(home);
        File.WriteAllText(Path.Combine(home, "config.toml"), ShippedConfig());
        LinkAuth(home);

        Prepared = home;
        return home;
    }

    private static string ShippedConfig()
    {
        using Stream stream = typeof(CodexHome).Assembly.GetManifestResourceStream(ConfigResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ConfigResourceName}' is missing.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    // A link, never a copy: it keeps a refreshed token writing through to the user's one real login.
    private static void LinkAuth(string home)
    {
        string real = Path.Combine(UserCodexHome(), "auth.json");
        string link = Path.Combine(home, "auth.json");

        if (!File.Exists(real))
            return;
        if (File.Exists(link) && ResolvesTo(link, real))
            return;

        try
        {
            File.Delete(link);
            File.CreateSymbolicLink(link, real);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            RhinoApp.WriteLine($"[codex] could not link auth.json ({ex.Message}); copied it instead, so a refreshed login may need re-running.");
            File.Copy(real, link, overwrite: true);
        }
    }

    private static bool ResolvesTo(string link, string target)
    {
        try
        {
            return new FileInfo(link).LinkTarget == target;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static string UserCodexHome() =>
        Environment.GetEnvironmentVariable("CODEX_HOME") is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
}
