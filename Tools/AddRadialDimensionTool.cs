using System;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.Geometry;

namespace RhMcp.Tools;

public sealed class AddRadialDimensionTool : IMcpTool
{
    public string Name => "add_radial_dimension";
    public string Description => "Create a radius or diameter dimension on an existing arc/circle object.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            id          = new { type = "string",  description = "GUID of arc or circle object" },
            leaderPoint = new { type = "object",  description = "Leader end point {x,y,z}" },
            diameter    = new { type = "boolean", description = "true = diameter dim, false = radius (default false)" }
        },
        required = new[] { "id", "leaderPoint" }
    };

    public object Execute(JsonObject? args)
    {
        var idStr = args?["id"]?.GetValue<string>()
            ?? throw new ArgumentException("Missing id");
        var leaderPt = JsonHelpers.ParsePoint(args?["leaderPoint"])
            ?? throw new ArgumentException("Missing leaderPoint");
        var diameter = args?["diameter"]?.GetValue<bool>() ?? false;

        if (!Guid.TryParse(idStr, out var guid))
            return new { content = new[] { new { type = "text", text = $"Invalid GUID: {idStr}" } } };

        var doc = RhinoDoc.ActiveDoc;
        var obj = doc.Objects.FindId(guid);
        if (obj?.Geometry is not Curve crv)
            return new { content = new[] { new { type = "text", text = "Object not found or not a curve." } } };

        Point3d center;
        double  radius;

        if (crv.TryGetCircle(out var circle))
        {
            center = circle.Center;
            radius = circle.Radius;
        }
        else if (crv.TryGetArc(out var arc))
        {
            center = arc.Center;
            radius = arc.Radius;
        }
        else
        {
            return new { content = new[] { new { type = "text", text = "Curve is not a circle or arc." } } };
        }

        var dirToLeader = leaderPt - center;
        if (!dirToLeader.Unitize())
            return new { content = new[] { new { type = "text", text = "leaderPoint coincides with center." } } };

        var radiusPoint = diameter
            ? center - dirToLeader * radius
            : center + dirToLeader * radius;
        var dimEndPoint = diameter
            ? center + dirToLeader * radius
            : center + dirToLeader * radius;

        var dim = new RadialDimension(AnnotationType.Ordinate, Plane.WorldXY, diameter ? center : radiusPoint, dimEndPoint, leaderPt);
        var id  = doc.Objects.AddRadialDimension(dim);
        if (id == Guid.Empty)
            return new { content = new[] { new { type = "text", text = "Failed to create radial dimension." } } };

        doc.Views.Redraw();
        return new { content = new[] { new { type = "text", text = $"Added {(diameter ? "diameter" : "radius")} dimension. id={id}" } } };
    }
}
