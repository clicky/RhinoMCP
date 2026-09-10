namespace Rhino.AI;

/// <summary>
/// Single source for the version token the router keys slots by. The listener
/// </summary>
internal static class RhinoVersion
{
    public static string Token => RhinoApp.Version.Major.ToString();
}
