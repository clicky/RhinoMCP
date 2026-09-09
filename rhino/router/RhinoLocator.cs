using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RhinoAI.Router;

/// <summary>Helps locate Rhino</summary>
internal static class RhinoLocator
{
    private sealed record VersionInstall(HashSet<string> Paths);

    private static Dictionary<string, VersionInstall> VersionMap { get; } = [];

    static RhinoLocator()
    {
        ScanInstalls();
    }

    private static void ScanInstalls()
    {
        VersionMap.Clear();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            IEnumerable<string> apps = Directory.EnumerateDirectories("/Applications", "Rhino*.app");
            foreach (string app in apps)
            {
                string infoPlist = Path.Combine(app, "Contents", "Info.plist");
                if (!MacExtensions.TryGetVersionInfoFromPlist(infoPlist, out Version v)) continue;
                string version = v.Major.ToString();
                VersionMap.TryGetValue(version, out VersionInstall? install);

                install ??= new([]);
                VersionMap[version] = install;

                install.Paths.Add(app);

                break;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            const string PATTERN = @"Rhino ([\d]+)";

            IEnumerable<string> apps = Directory.EnumerateDirectories(@"C:\Program Files", "Rhino*");
            foreach (string app in apps)
            {
                // Windows has the Rhino SDK which _would_ match without this
                if (app.Contains("SDK", StringComparison.InvariantCultureIgnoreCase)) continue;
                Match match = Regex.Match(app, PATTERN);
                if (match.Groups.Count != 2) continue;
                if (!int.TryParse(match.Groups[1].Value, out int v)) continue;

                string version = v.ToString();
                VersionMap.TryGetValue(version, out VersionInstall? install);

                install ??= new([]);
                VersionMap[version] = install;

                install.Paths.Add(app);
            }
        }
    }

    public static string ResolveRhinoExe(string version, IReadOnlyDictionary<string, string>? overrides = null)
    {
        if (TryResolve(version, overrides, out string path))
            return path;

        throw new FileNotFoundException(
            $"Could not locate Rhino executable for version '{version}'. " +
            $"Installed versions found: {string.Join(", ", VersionMap.Keys)}");
    }

    // TODO : This is very badly named
    public static bool IsOverride(string version, IReadOnlyDictionary<string, string>? overrides) =>
        TryOverride(version, overrides, out _);

    private static bool TryOverride(string version, IReadOnlyDictionary<string, string>? overrides, out string path)
    {
        path = string.Empty;
        if (overrides is null)
            return false;

        foreach ((string key, string candidate) in overrides)
        {
            if (VersionMatch.IsCompatible(key, version) &&
                (File.Exists(candidate) || Directory.Exists(candidate)))
            {
                path = candidate;
                return true;
            }
        }
        return false;
    }

    private static bool TryResolve(string version, IReadOnlyDictionary<string, string>? overrides, out string path)
    {
        if (TryOverride(version, overrides, out path))
            return true;

        path = string.Empty;

        if (!VersionMap.TryGetValue(version, out VersionInstall? install))
            return false;

        path = install.Paths.LastOrDefault()!;
        return !string.IsNullOrEmpty(path);
    }

}
