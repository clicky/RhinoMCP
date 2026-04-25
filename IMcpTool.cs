using System.Text.Json.Nodes;

namespace RhMcp;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    object InputSchema { get; }
    object Execute(JsonObject? args);
}
