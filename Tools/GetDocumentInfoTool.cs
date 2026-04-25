using System.Text.Json;
using System.Text.Json.Nodes;
using Rhino;

namespace RhMcp.Tools;

public sealed class GetDocumentInfoTool : IMcpTool
{
    public string Name => "get_document_info";
    public string Description => "Return document metadata: units, tolerances, active layer, file path.";
    public object InputSchema => new { type = "object", properties = new { } };

    public object Execute(JsonObject? args)
    {
        var doc = RhinoDoc.ActiveDoc;
        var info = new
        {
            name                  = doc.Name,
            path                  = doc.Path,
            units                 = doc.ModelUnitSystem.ToString(),
            absoluteTolerance     = doc.ModelAbsoluteTolerance,
            angleToleranceDegrees = doc.ModelAngleToleranceDegrees,
            activeLayer           = doc.Layers[doc.Layers.CurrentLayerIndex].FullPath,
            objectCount           = doc.Objects.Count
        };

        return new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(info) } } };
    }
}
