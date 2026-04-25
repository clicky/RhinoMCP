using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rhino;

namespace RhMcp.Tools;

public sealed class GetDimensionStylesTool : IMcpTool
{
    public string Name => "get_dimension_styles";
    public string Description => "List dimension styles in the document with units and core display settings.";
    public object InputSchema => new { type = "object", properties = new { } };

    public object Execute(JsonObject? args)
    {
        var doc    = RhinoDoc.ActiveDoc;
        var styles = doc.DimStyles
            .Where(s => !s.IsDeleted)
            .Select(s => new
            {
                id           = s.Id.ToString(),
                name         = s.Name,
                isCurrent    = s.Id == doc.DimStyles.CurrentId,
                units        = s.DimensionLengthDisplayUnit(doc.RuntimeSerialNumber).ToString(),
                textHeight   = s.TextHeight,
                arrowSize    = s.ArrowLength,
                precision    = s.LengthResolution,
                anglePrecision = s.AngleResolution,
                font         = s.Font?.QuartetName ?? ""
            })
            .ToArray();

        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(styles) } } };
    }
}
