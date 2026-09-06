using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoAI.Tools;

[McpServerToolType]
internal static class ZoomToLayerTool
{
    [McpServerTool("zoom_to_layer", "Zoom To Layer", false, false)]
    [Description("Zoom the active viewport to fit all objects on a layer (full path).")]
    public static IToolResult ZoomToLayer(
        RhinoDoc doc,
        [Description("Layer full path")] string layer)
    {
        if (doc.IsHeadless)
            return Failure(ToolError.RH_Doc_Headless);

        int idx = doc.Layers.FindByFullPath(layer, RhinoMath.UnsetIntIndex);

        if (idx < 0)
            return Failure(ToolError.RH_Layer_NotFound, $"Layer not found: {layer}");

        ObjectEnumeratorSettings settings = new()
        {
            ActiveObjects = true,
            HiddenObjects = true,
            LockedObjects = true,
            DeletedObjects = false,
            IncludeLights = false,
            IncludeGrips = false,
            IncludePhantoms = false,
            LayerIndexFilter = idx,
        };

        BoundingBox bb = BoundingBox.Empty;
        int count = 0;

        foreach (RhinoObject obj in doc.Objects.GetObjectList(settings))
        {
            if (obj.Geometry is null) continue;
            bb.Union(obj.Geometry.GetBoundingBox(true));
            count++;
        }

        if (!bb.IsValid)
            return Failure(ToolError.RH_Nothing_Visible, $"No geometry on layer: {layer}");

        RhinoViewport? vp = doc.Views.ActiveView?.ActiveViewport;
        if (vp is null)
            return Failure(ToolError.RH_View_NotFound, guidance: "Ask the user to open a viewport");

        vp.ZoomBoundingBox(bb);
        doc.Views.Redraw();

        return Success(ContentBlock.CreateText($"Zoomed to {count} object(s) on layer \"{layer}\"."));
    }
}
