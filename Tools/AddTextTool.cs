using System;
using System.Text.Json.Nodes;
using Rhino;
using Rhino.Geometry;

namespace RhMcp.Tools;

public sealed class AddTextTool : IMcpTool
{
    public string Name => "add_text";
    public string Description => "Add text annotation at a location with given height. Plane defaults to world XY at the location.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            text     = new { type = "string", description = "Text content" },
            location = new { type = "object", description = "Anchor point {x,y,z}" },
            height   = new { type = "number", description = "Text height (default 1.0)" },
            font     = new { type = "string", description = "Font name (default Arial)" },
            bold     = new { type = "boolean" },
            italic   = new { type = "boolean" },
            plane    = new { type = "string", description = "Plane: worldxy, worldxz, worldyz, cplane (default worldxy)" }
        },
        required = new[] { "text", "location" }
    };

    public object Execute(JsonObject? args)
    {
        var text     = args?["text"]?.GetValue<string>() ?? throw new ArgumentException("Missing text");
        var location = JsonHelpers.ParsePoint(args?["location"]) ?? throw new ArgumentException("Missing location");
        var height   = args?["height"]?.GetValue<double>() ?? 1.0;
        var font     = args?["font"]?.GetValue<string>()   ?? "Arial";
        var bold     = args?["bold"]?.GetValue<bool>()     ?? false;
        var italic   = args?["italic"]?.GetValue<bool>()   ?? false;
        var planeName = args?["plane"]?.GetValue<string>()?.ToLowerInvariant();

        var doc   = RhinoDoc.ActiveDoc;
        var basis = planeName switch
        {
            "worldxz" => Plane.WorldZX,
            "worldyz" => Plane.WorldYZ,
            "cplane"  => doc.Views.ActiveView?.ActiveViewport.GetConstructionPlane().Plane ?? Plane.WorldXY,
            _         => Plane.WorldXY,
        };
        basis.Origin = location;

        var id = doc.Objects.AddText(text, basis, height, font, bold, italic);
        if (id == Guid.Empty)
            return new { content = new[] { new { type = "text", text = "Failed to add text." } } };

        doc.Views.Redraw();
        return new { content = new[] { new { type = "text", text = $"Added text. id={id}" } } };
    }
}
