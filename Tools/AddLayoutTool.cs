using System;
using System.Text.Json.Nodes;
using Rhino;

namespace RhMcp.Tools;

public sealed class AddLayoutTool : IMcpTool
{
    public string Name => "add_layout";
    public string Description => "Create a new page layout (paper space) with given name and dimensions in millimeters.";
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            name   = new { type = "string", description = "Layout name" },
            width  = new { type = "number", description = "Page width (mm, default 420 = A3)" },
            height = new { type = "number", description = "Page height (mm, default 297 = A3)" }
        },
        required = new[] { "name" }
    };

    public object Execute(JsonObject? args)
    {
        var name   = args?["name"]?.GetValue<string>() ?? throw new ArgumentException("Missing name");
        var width  = args?["width"]?.GetValue<double>()  ?? 420.0;
        var height = args?["height"]?.GetValue<double>() ?? 297.0;

        var doc  = RhinoDoc.ActiveDoc;
        var page = doc.Views.AddPageView(name, width, height);
        if (page == null)
            return new { content = new[] { new { type = "text", text = "Failed to create layout." } } };

        return new { content = new[] { new { type = "text", text = $"Created layout \"{page.PageName}\" ({width}x{height})." } } };
    }
}
