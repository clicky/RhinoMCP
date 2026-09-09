using System.IO;

namespace RhinoAI;

internal class CodexFinder : IAgentFinder
{

    private static string APPDATA => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string LOCALAPPDATA => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public List<string> Find()
    {
        List<string> paths = [];

        // C:\Users\user\AppData\Local\OpenAI\Codex\bin\8618603f6caa97b3
        if (OperatingSystem.IsWindows())
        {
            string codexBinDir = @"C:\Users\user\AppData\Local\OpenAI\Codex\bin";
            if (Directory.Exists(codexBinDir))
            {
                string? codexCliPath = Directory.EnumerateFiles(codexBinDir, "codex.exe").FirstOrDefault();
                if (!string.IsNullOrEmpty(codexCliPath))
                {
                    paths.Add(codexCliPath);
                }
            }
        }

        // Mac
        // /Applications/ChatGPT.app/Contents/Resources/codex
        else if (OperatingSystem.IsMacOS())
        {
            string codexCliPath = "/Applications/ChatGPT.app/Contents/Resources/codex";
            if (File.Exists(codexCliPath))
            {
                paths.Add(codexCliPath);
            }
        }

        // Ensure higher version is higher up
        paths.OrderDescending();


        return paths;
    }

}
