namespace Rhino.AI.Tools;

internal static class GH2_Failures
{

    public static IToolResult NoDocument => Failure(ToolError.GH_Document_NotFound, guidance: "Start GH2 with g2_start, or ask the user for assistance");

}
