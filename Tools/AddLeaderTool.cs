using System;
using System.Linq;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.Geometry;

namespace RhMcp.Tools;

public sealed class AddLeaderTool : IMcpTool
{
    public string Name => "add_leader";
    public string Description => "Add a leader (callout) with text. Points define the leader polyline; last point anchors the text.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            text   = new { type = "string", description = "Leader text" },
            points = new
            {
                type        = "array",
                description = "Leader points in order — at least 2",
                items       = new { type = "object" }
            },
            plane = new { type = "string", description = "Plane: worldxy, worldxz, worldyz, cplane (default worldxy)" }
        },
        required = new[] { "text", "points" }
    };

    public object Execute(JsonObject? args)
    {
        var text = args?["text"]?.GetValue<string>() ?? throw new ArgumentException("Missing text");
        var pointNodes = args?["points"]?.AsArray()
            ?? throw new ArgumentException("Missing points");

        var pts3d = pointNodes
            .Select(n => JsonHelpers.ParsePoint(n))
            .OfType<Point3d?>()
            .Select(p => p!.Value)
            .ToArray();

        if (pts3d.Length < 2)
            return new { content = new[] { new { type = "text", text = "Need at least 2 leader points." } } };

        var planeName = args?["plane"]?.GetValue<string>()?.ToLowerInvariant();
        var doc       = RhinoDoc.ActiveDoc;
        var plane     = planeName switch
        {
            "worldxz" => Plane.WorldZX,
            "worldyz" => Plane.WorldYZ,
            "cplane"  => doc.Views.ActiveView?.ActiveViewport.GetConstructionPlane().Plane ?? Plane.WorldXY,
            _         => Plane.WorldXY,
        };

        var pts2d = pts3d.Select(p =>
        {
            plane.ClosestParameter(p, out var s, out var t);
            return new Point2d(s, t);
        });

        var id = doc.Objects.AddLeader(text, plane, pts2d);
        if (id == Guid.Empty)
            return new { content = new[] { new { type = "text", text = "Failed to add leader." } } };

        doc.Views.Redraw();
        return new { content = new[] { new { type = "text", text = $"Added leader. id={id}" } } };
    }
}
