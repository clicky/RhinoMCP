using System.IO;

namespace RhinoAI;

internal interface IAgentFinder
{
    public List<string> Find();
}

internal class ClaudeFinder : IAgentFinder
{

    private static string APPDATA => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string LOCALAPPDATA => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    // // C:\Users\user\AppData\Roaming\Claude\claude-code\2.1.260\claude.exe
    // C:\Users\sykes\AppData\Local\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude\claude-code\2.1.260

    public List<string> Find()
    {
        List<string> paths = [];

        string claudeCodeBaseDir = Path.Combine(APPDATA, "Claude", "claude-code");
        if (Directory.Exists(claudeCodeBaseDir))
        {
            foreach (string dir in Directory.EnumerateDirectories(claudeCodeBaseDir))
            {
                string dirName = new DirectoryInfo(dir).Name;
                if (!Version.TryParse(dirName, out Version? _)) continue;
                paths.Add(Path.Combine(dir, "claude.exe"));
            }
        }

        string claudeDesktopBaseDir = Path.Combine(LOCALAPPDATA, "Packages");
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
                    paths.Add(Path.Combine(dir, "claude.exe"));
                }
            }
        }

        // Ensure higher version is higher up
        paths.OrderDescending();


        return paths;
    }

}
