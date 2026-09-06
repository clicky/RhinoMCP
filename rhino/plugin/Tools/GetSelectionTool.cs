namespace RhinoAI.Tools;

[McpServerToolType]
public static class GetSelectionTool
{
    [McpServerTool("get_selection", "Get Selection", true, false)]
    [Description("Return all currently selected objects in Rhino.")]
    public static IToolResult GetSelection(RhinoDoc doc) =>
        Success(GetContextTool.SelectionOf(doc));
}
