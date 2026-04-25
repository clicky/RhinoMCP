using System;
using System.Linq;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

namespace RhMcp.Tools;

public sealed class AddDetailViewTool : IMcpTool
{
    public string Name => "add_detail_view";
    public string Description => "Add a detail view to a page layout. Detail shows model space at given projection and scale.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            layout     = new { type = "string", description = "Page layout name" },
            corner1    = new { type = "object", description = "First corner on page {x,y}" },
            corner2    = new { type = "object", description = "Opposite corner on page {x,y}" },
            view       = new { type = "string", description = "Projection: top, bottom, left, right, front, back, perspective (default top)" },
            scale      = new { type = "number", description = "Model:paper scale ratio (e.g. 0.01 for 1:100). Optional." },
            title      = new { type = "string", description = "Detail title (optional)" },
            lockDetail = new { type = "boolean", description = "Lock projection and scale (default true)" }
        },
        required = new[] { "layout", "corner1", "corner2" }
    };

    public object Execute(JsonObject? args)
    {
        var layoutName = args?["layout"]?.GetValue<string>() ?? throw new ArgumentException("Missing layout");
        var c1 = JsonHelpers.ParsePoint2d(args?["corner1"]) ?? throw new ArgumentException("Missing corner1");
        var c2 = JsonHelpers.ParsePoint2d(args?["corner2"]) ?? throw new ArgumentException("Missing corner2");
        var viewStr = args?["view"]?.GetValue<string>()?.ToLowerInvariant() ?? "top";
        var scale   = args?["scale"]?.GetValue<double>();
        var title   = args?["title"]?.GetValue<string>() ?? "";
        var lockDet = args?["lockDetail"]?.GetValue<bool>() ?? true;

        var proj = viewStr switch
        {
            "bottom"      => DefinedViewportProjection.Bottom,
            "left"        => DefinedViewportProjection.Left,
            "right"       => DefinedViewportProjection.Right,
            "front"       => DefinedViewportProjection.Front,
            "back"        => DefinedViewportProjection.Back,
            "perspective" => DefinedViewportProjection.Perspective,
            _             => DefinedViewportProjection.Top,
        };

        var doc  = RhinoDoc.ActiveDoc;
        var page = doc.Views.GetPageViews().FirstOrDefault(p => p.PageName == layoutName);
        if (page == null)
            return new { content = new[] { new { type = "text", text = $"Layout not found: {layoutName}" } } };

        var detail = page.AddDetailView(title, c1, c2, proj);
        if (detail == null)
            return new { content = new[] { new { type = "text", text = "Failed to add detail view." } } };

        if (scale.HasValue && detail.DetailGeometry != null)
        {
            var units = doc.ModelUnitSystem;
            detail.DetailGeometry.SetScale(1.0 / scale.Value, units, 1.0, units);
            detail.CommitChanges();
        }

        if (lockDet && detail.DetailGeometry != null)
        {
            detail.DetailGeometry.IsProjectionLocked = true;
            detail.CommitChanges();
        }

        page.Redraw();
        return new { content = new[] { new { type = "text", text = $"Added detail view to \"{layoutName}\". id={detail.Id}" } } };
    }
}
