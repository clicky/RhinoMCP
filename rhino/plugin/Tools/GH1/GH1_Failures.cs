namespace RhinoAI.Tools;

internal static class GH1_Failures
{

    public static IToolResult NoDocument => Failure(ToolError.GH_Document_NotFound, guidance: "Start Grasshopper with g1_start, or ask the user for assistance");

}
