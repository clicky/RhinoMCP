using Rhino.Display;
using Rhino.Geometry;

namespace Rhino.AI.Tools;

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

        if (ids is null || ids.Length <= 0)
            return Failure(ToolError.BadArgument, "No ids passed to tool", "Pass in a list of object IDs");

        BoundingBox bb = BoundingBox.Empty;

        Coercions coercions = new();

        foreach (string idStr in ids)
        {
            if (!Guid.TryParse(idStr, out Guid guid))
            {
                coercions.Note($"{idStr} was not valid, Only pass in valid GUIDs");
                continue;
            }

            DocObjects.RhinoObject obj = doc.Objects.FindId(guid);
            if (obj?.Geometry is null)
            {
                coercions.Note($"Object with {idStr} had no geometry, it cannot be zoomed to.");
                continue;
            }

            bb.Union(obj.Geometry.GetBoundingBox(true));
        }

        if (!bb.IsValid)
        {
            return Failure(ToolError.RH_Object_NotFound, "No valid objects found.", coercions.Guidance);
        }

        // TODO : Create a viewport automatically?
        RhinoViewport? vp = doc.Views.ActiveView?.ActiveViewport;
        if (vp is null)
            return Failure(ToolError.RH_View_NotFound, guidance: "Ask the user to open a viewport");

        vp.ZoomBoundingBox(bb);
        doc.Views.Redraw();

        return Success(ContentBlock.CreateText($"Zoomed to {ids.Length} object(s)."));
    }
}
