using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Rhino.AI.Router;

/// <summary>Helps locate Rhino</summary>
internal static class RhinoLocator
{
    private sealed record VersionInstall(HashSet<string> Paths);

    private static Dictionary<string, VersionInstall> VersionMap { get; } = [];

    static RhinoLocator()
    {
        ScanInstalls();
    }
    
    // TODO : Only runs once, should be possible to run again
    private static void ScanInstalls()
    {
        VersionMap.Clear();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            IEnumerable<string> apps;
            try
            {
                apps = Directory.EnumerateDirectories("/Applications", "Rhino*.app").ToList();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                apps = [];
            }

            foreach (string app in apps)
            {
                string infoPlist = Path.Combine(app, "Contents", "Info.plist");
                if (!MacExtensions.TryGetVersionInfoFromPlist(infoPlist, out Version? v)) continue;
                string version = v.Major.ToString();
                VersionMap.TryGetValue(version, out VersionInstall? install);

                install ??= new([]);
                VersionMap[version] = install;

                install.Paths.Add(app);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            const string PATTERN = @"Rhino ([\d]+)";

            IEnumerable<string> apps;
            try
            {
                apps = Directory.EnumerateDirectories(@"C:\Program Files", "Rhino*").ToList();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                apps = [];
            }

            foreach (string app in apps)
            {
                // Windows has the Rhino SDK which _would_ match without this
                if (app.Contains("SDK", StringComparison.InvariantCultureIgnoreCase)) continue;
                Match match = Regex.Match(app, PATTERN);
                if (match.Groups.Count != 2) continue;
                if (!int.TryParse(match.Groups[1].Value, out int v)) continue;

                string fullPath = Path.Combine(app, "System", "Rhino.exe");

                string version = v.ToString();
                VersionMap.TryGetValue(version, out VersionInstall? install);

                install ??= new([]);
                VersionMap[version] = install;

                install.Paths.Add(fullPath);
            }
        }

        foreach (KeyValuePair<string, VersionInstall> kvp in VersionMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
        {
            foreach (string vers in new string[] { "WIP", "BETA" })
            {
                foreach (string path in kvp.Value.Paths)
                {
                    if (!path.Contains(vers, StringComparison.OrdinalIgnoreCase)) continue;

                    VersionMap.TryGetValue(kvp.Key, out VersionInstall? install);

                    install ??= new([]);
                    VersionMap[vers] = install; 

                    install.Paths.Add(path);
                }
            }
        }

        string? highestVersion = VersionMap.Keys.Order().LastOrDefault();
        if (!string.IsNullOrEmpty(highestVersion))
        {
            VersionMap.TryAdd("BETA", VersionMap[highestVersion]);
            VersionMap.TryAdd("WIP", VersionMap[highestVersion]);
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

        path = install.Paths.OrderBy(File.GetLastWriteTime).LastOrDefault()!;
        return !string.IsNullOrEmpty(path);
    }

}
