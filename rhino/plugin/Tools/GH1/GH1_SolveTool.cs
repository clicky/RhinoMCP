using RhinoAI.Resources;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class GH1_SolveTool
{
    [McpServerTool("g1_solve_graph", "Solve GH1 Graph", false, false)]
    [Description("Solves the active GH canvas. zoom_views controls whether Rhino viewports zoom to the new preview: true=always, false=never, null=auto (zoom only when nothing was previewed before the solve).")]
    public static IToolResult Solve(
        RhinoDoc rhinoDoc,
        [Description("Auto-zoom every Rhino viewport to the GH preview after solving. true=always, false=never, null=zoom only when nothing was visible pre-solve.")] bool? zoom_views = null)
    {
        if (!GH1_Utils.TryGetOrCreateDoc(rhinoDoc, out GH_Document ghDoc))
            return GH1_Failures.NoDocument;

        int activeCount = ghDoc.ActiveObjects().Count;
        if (activeCount <= 0)
        {
            return Failure(ToolError.GH_Canvas_Empty, "No Active Objects");
        }

        BoundingBox preBbox = GetPreviewBoundingBox(ghDoc);
        bool wasPreviewVisible = preBbox.IsValid && preBbox.Diagonal.Length > 0;

        try
        {
            ghDoc.NewSolution(true);

            bool shouldZoom = zoom_views ?? !wasPreviewVisible;
            if (shouldZoom)
                ZoomViewsToPreview(rhinoDoc, ghDoc);
        }
        catch (Exception ex)
        {
            return Failure(ex);
        }

        List<GH1_Utils.ComponentStatus> statuses = GH1_Utils.GetCanvasStatus(ghDoc);
        if (statuses.Count == 0)
            return Success();

        return Failure(
            ToolError.GH_Solution_Failed,
            ContentBlock.CreateJson(statuses),
            "The solution reported component errors",
            "Read the statuses to see which components failed and why");
    }

    private static BoundingBox GetPreviewBoundingBox(GH_Document doc)
    {
        var bbox = BoundingBox.Empty;
        foreach (IGH_DocumentObject obj in doc.Objects)
        {
            if (obj is IGH_PreviewObject preview && preview.IsPreviewCapable && !preview.Hidden)
            {
                try
                {
                    var b = preview.ClippingBox;
                    if (b.IsValid) bbox.Union(b);
                }
                catch { /* a few proxies throw before first solve */ }
            }
        }
        return bbox;
    }

    private static void ZoomViewsToPreview(RhinoDoc rhinoDoc, GH_Document doc)
    {
        var bbox = GetPreviewBoundingBox(doc);
        if (!bbox.IsValid || bbox.Diagonal.Length <= 0) return;

        foreach (var view in rhinoDoc.Views)
        {
            view.ActiveViewport.ZoomBoundingBox(bbox);
            view.Redraw();
        }
    }
}
