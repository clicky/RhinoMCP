using System;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.Geometry;

namespace RhMcp.Tools;

public sealed class AddLinearDimensionTool : IMcpTool
{
    public string Name => "add_linear_dimension";
    public string Description => "Create a linear dimension between two points with a dimension-line offset point. Defaults to world XY plane.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            p1       = new { type = "object", description = "Extension point 1 {x,y,z}" },
            p2       = new { type = "object", description = "Extension point 2 {x,y,z}" },
            dimLine  = new { type = "object", description = "Point on dimension line {x,y,z} (controls offset)" },
            plane    = new { type = "string", description = "Plane: worldxy, worldxz, worldyz, cplane (default worldxy)" }
        },
        required = new[] { "p1", "p2", "dimLine" }
    };

    public object Execute(JsonObject? args)
    {
        var p1      = JsonHelpers.ParsePoint(args?["p1"])      ?? throw new ArgumentException("Missing p1");
        var p2      = JsonHelpers.ParsePoint(args?["p2"])      ?? throw new ArgumentException("Missing p2");
        var dimLine = JsonHelpers.ParsePoint(args?["dimLine"]) ?? throw new ArgumentException("Missing dimLine");
        var planeName = args?["plane"]?.GetValue<string>()?.ToLowerInvariant();

        var doc   = RhinoDoc.ActiveDoc;
        var plane = ResolvePlane(planeName, doc);

        plane.ClosestParameter(p1,      out var s1, out var t1);
        plane.ClosestParameter(p2,      out var s2, out var t2);
        plane.ClosestParameter(dimLine, out var sd, out var td);

        var dim = new LinearDimension(plane, new Point2d(s1, t1), new Point2d(s2, t2), new Point2d(sd, td));
        var id  = doc.Objects.AddLinearDimension(dim);
        if (id == Guid.Empty)
            return new { content = new[] { new { type = "text", text = "Failed to create linear dimension." } } };

        doc.Views.Redraw();
        return new { content = new[] { new { type = "text", text = $"Added linear dimension. id={id}" } } };
    }

    private static Plane ResolvePlane(string? name, RhinoDoc doc) => name switch
    {
        "worldxz" => Plane.WorldZX,
        "worldyz" => Plane.WorldYZ,
        "cplane"  => doc.Views.ActiveView?.ActiveViewport.GetConstructionPlane().Plane ?? Plane.WorldXY,
        _         => Plane.WorldXY,
    };
}
