namespace Rhino.AI.Router;

public static class VersionMatch
{
    
    private static HashSet<string> Rhino9Family { get; } = new(StringComparer.OrdinalIgnoreCase) { "9", "BETA", "WIP" };

    public static bool IsCompatible(string actual, string required)
    {
        if (string.Equals(actual, required, StringComparison.OrdinalIgnoreCase)) return true;

        return Rhino9Family.Contains(actual) && Rhino9Family.Contains(required);
    }
}
