using Rhino.Commands;

namespace Rhino.AI.Tools;

[McpServerToolType]
internal static class RunCommandTool
{
    [McpServerTool("run_command", "Run Rhino Command", false, true)]
    [Description("Execute any Rhino command string and return command window output. Example: \"_Box 0,0,0 10,10,10 _Enter\"")]
    public static IToolResult RunCommand(
        RhinoDoc doc,
        [Description("Rhino command string to execute")] string command)
    {
        // TODO : RhinoApp.CommandWindowCaptureEnabled is not document specific
        
        RhinoApp.CommandWindowCaptureEnabled = true;
        bool ran = RhinoApp.RunScript(doc.RuntimeSerialNumber, command, false);
        string[] lines = RhinoApp.CapturedCommandWindowStrings(true);
        RhinoApp.CommandWindowCaptureEnabled = false;

        ContentBlock output = ContentBlock.CreateText(lines is { Length: > 0 } ? string.Concat(lines) : "Done.");

        return ran
            ? Success(output)
            : Failure(ToolError.Failed, output, $"Rhino did not run '{command}' to completion");
    }
}
