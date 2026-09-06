using Rhino.Commands;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class RunCommandTool
{
    [McpServerTool("run_command", "Run Rhino Command", false, true)]
    [Description("Execute any Rhino command string and return command window output. Example: \"_Box 0,0,0 10,10,10 _Enter\"")]
    public static IToolResult RunCommand(
        RhinoDoc doc,
        [Description("Rhino command string to execute")] string command)
    {
        RhinoApp.CommandWindowCaptureEnabled = true;
        RhinoApp.RunScript(doc.RuntimeSerialNumber, command, false);
        string[] lines = RhinoApp.CapturedCommandWindowStrings(true);
        RhinoApp.CommandWindowCaptureEnabled = false;

        return Success(ContentBlock.CreateText(lines is { Length: > 0 } ? string.Concat(lines) : "Done."));
    }
}
