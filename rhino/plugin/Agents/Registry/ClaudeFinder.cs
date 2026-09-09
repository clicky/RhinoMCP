using System.IO;

namespace Rhino.AI;

internal class ClaudeFinder : IAgentFinder
{

    private static string USER_PROFILE => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static string APPDATA => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string LOCAL_APPDATA => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public List<string> Find()
    {
        List<string> paths = [];

        // Examples I've seen in the wild
        // C:\Users\user\AppData\Roaming\Claude\claude-code\2.1.260\claude.exe
        // C:\Users\user\AppData\Local\Packages\Claude_{gibberish}}\LocalCache\Roaming\Claude\claude-code\2.1.260
        if (OperatingSystem.IsWindows())
        {
            string claudeDesktopBaseDir = Path.Combine(LOCAL_APPDATA, "Packages");
            if (Directory.Exists(claudeDesktopBaseDir))
            {
                foreach (string claudeDir in Directory.EnumerateDirectories(claudeDesktopBaseDir, "*laude*"))
                {
                    string claudeCodeDir = Path.Combine(claudeDir, "LocalCache", "Roaming", "Claude", "claude-code");
                    if (!Directory.Exists(claudeCodeDir)) continue;
                    foreach (string dir in Directory.EnumerateDirectories(claudeCodeDir))
                    {

                        string dirName = new DirectoryInfo(dir).Name;
                        if (!Version.TryParse(dirName, out Version? _)) continue;
                        string claudeExePath = Path.Combine(dir, "claude.exe");
                        if (!File.Exists(claudeExePath)) continue;
                        paths.Add(claudeExePath);
                    }
                }
            }

            string claudeCodeBaseDir = Path.Combine(APPDATA, "Claude", "claude-code");
            if (Directory.Exists(claudeCodeBaseDir))
            {
                foreach (string dir in Directory.EnumerateDirectories(claudeCodeBaseDir))
                {
                    string dirName = new DirectoryInfo(dir).Name;
                    if (!Version.TryParse(dirName, out Version? _)) continue;
                    string claudeExePath = Path.Combine(dir, "claude.exe");
                    if (!File.Exists(claudeExePath)) continue;
                    paths.Add(claudeExePath);
                }
            }
        }
        // Mac
        //  /Users/user/.local/bin/claude -> symlink -> ~/.local/share/claude/versions/<version>
        else if (OperatingSystem.IsMacOS())
        {
            string localBin = Path.Combine(USER_PROFILE, ".local", "bin");
            if (Directory.Exists(localBin))
            {
                string claudeAlias = Path.Combine(localBin, "claude");
                if (File.Exists(claudeAlias))
                {
                    paths.Add(claudeAlias);
                }
            }
        }

        // Ensure higher version is higher up
        paths.OrderDescending();


        return paths;
    }

}
