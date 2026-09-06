using RhinoAI.Tools;

namespace RhinoAI.Server;

/// <summary>
/// Renders a tool's result as the MCP call result on the wire.
/// </summary>
internal static class ToolResultFormatter
{

    // The envelope leads so the agent reads what happened before whatever the tool
    // attached; isError marks a failure as "ran and failed" rather than a transport
    // error, matching what McpEndpoint does with a thrown tool.
    public static CallToolResult Format(IToolResult result)
    {
        List<ContentBlock> content = new(result.Attachments.Count + 1);

        if (Envelope(result) is string envelope)
            content.Add(ContentBlock.CreateText(envelope));

        content.AddRange(result.Attachments);

        if (content.Count == 0)
            content.Add(ContentBlock.CreateText(""));

        return new CallToolResult { Content = content, IsError = result.IsFailure };
    }

    // Null for the common success, where the attachments say everything and an
    // envelope would only be noise. The serializer drops the null members.
    private static string? Envelope(IToolResult result)
    {
        if (result.Error is null && result.Message is null && result.Guidance is null)
            return null;

        return JsonSerializer.Serialize(
            new { result.Error, result.Message, result.Guidance },
            McpSerializer.Options);
    }

}
