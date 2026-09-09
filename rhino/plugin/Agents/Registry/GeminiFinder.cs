using System.IO;

namespace RhinoAI;

internal class GeminiFinder : IAgentFinder
{

    private static string APPDATA => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string LOCALAPPDATA => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    // Examples I've seen in the wild
    // C:\Users\user\AppData\Roaming\Claude\claude-code\2.1.260\claude.exe
    // C:\Users\user\AppData\Local\Packages\Claude_{gibberish}}\LocalCache\Roaming\Claude\claude-code\2.1.260

    public List<string> Find()
    {
        List<string> paths = [];

        if (OperatingSystem.IsWindows())
        {
            // No Windows Desktop App?
        }

        // Mac
        else if (OperatingSystem.IsMacOS())
        {

        }

        // Ensure higher version is higher up
        paths.OrderDescending();


        return paths;
    }

}
