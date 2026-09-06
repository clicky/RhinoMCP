using Rhino.Display;
using Rhino.Geometry;

namespace RhinoAI.Tools;

[McpServerToolType]
internal static class ZoomToObjectTool
{
    [McpServerTool("zoom_to_object", "Zoom To Object", false, false)]
    [Description("Zoom the active viewport to fit one or more objects by GUID.")]
    public static IToolResult ZoomToObject(
        RhinoDoc doc,
        [Description("Object GUIDs to zoom to")] string[] ids)
    {
        if (doc.IsHeadless)
            return Failure(ToolError.RH_Doc_Headless);

        BoundingBox bb = BoundingBox.Empty;

        foreach (string idStr in ids)
        {
            if (!Guid.TryParse(idStr, out Guid guid)) continue;
            Rhino.DocObjects.RhinoObject obj = doc.Objects.FindId(guid);
            if (obj?.Geometry is null) continue;
            bb.Union(obj.Geometry.GetBoundingBox(true));
        }

        if (!bb.IsValid)
            return Failure(ToolError.RH_Object_NotFound, "No valid objects found.");

        RhinoViewport? vp = doc.Views.ActiveView?.ActiveViewport;
        if (vp is null)
            return Failure(ToolError.RH_View_NotFound, guidance: "Ask the user to open a viewport");

        vp.ZoomBoundingBox(bb);
        doc.Views.Redraw();

        return Success(ContentBlock.CreateText($"Zoomed to {ids.Length} object(s)."));
    }
}
