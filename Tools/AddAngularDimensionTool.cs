using System;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.Geometry;

namespace RhMcp.Tools;

public sealed class AddAngularDimensionTool : IMcpTool
{
    public string Name => "add_angular_dimension";
    public string Description => "Create an angular dimension from a vertex and two ray points. arcPoint sets radius and side.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            vertex   = new { type = "object", description = "Angle vertex {x,y,z}" },
            p1       = new { type = "object", description = "Point on first ray {x,y,z}" },
            p2       = new { type = "object", description = "Point on second ray {x,y,z}" },
            arcPoint = new { type = "object", description = "Point on dimension arc — controls radius/side {x,y,z}" }
        },
        required = new[] { "vertex", "p1", "p2", "arcPoint" }
    };

    public object Execute(JsonObject? args)
    {
        var vertex   = JsonHelpers.ParsePoint(args?["vertex"])   ?? throw new ArgumentException("Missing vertex");
        var p1       = JsonHelpers.ParsePoint(args?["p1"])       ?? throw new ArgumentException("Missing p1");
        var p2       = JsonHelpers.ParsePoint(args?["p2"])       ?? throw new ArgumentException("Missing p2");
        var arcPoint = JsonHelpers.ParsePoint(args?["arcPoint"]) ?? throw new ArgumentException("Missing arcPoint");

        var v1 = p1 - vertex; v1.Unitize();
        var v2 = p2 - vertex; v2.Unitize();
        var radius = vertex.DistanceTo(arcPoint);
        if (radius < RhinoMath.ZeroTolerance)
            return new { content = new[] { new { type = "text", text = "arcPoint coincides with vertex." } } };

        var startPt = vertex + v1 * radius;
        var endPt   = vertex + v2 * radius;

        var arc = new Arc(startPt, arcPoint, endPt);
        if (!arc.IsValid)
            return new { content = new[] { new { type = "text", text = "Could not build dimension arc — points may be collinear." } } };

        var dim = new AngularDimension(arc, 0);
        var doc = RhinoDoc.ActiveDoc;
        var id  = doc.Objects.AddAngularDimension(dim);
        if (id == Guid.Empty)
            return new { content = new[] { new { type = "text", text = "Failed to create angular dimension." } } };

        doc.Views.Redraw();
        return new { content = new[] { new { type = "text", text = $"Added angular dimension. id={id}" } } };
    }
}
