using RhinoAI.Resources;

using Grasshopper;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class GH1_StartTool
{

    [McpServerTool("g1_start", "Start Grasshopper 1", false, false)]
    [Description("Starts Grasshopper")]
    public static IToolResult Launch(RhinoDoc doc)
    {
        try
        {
            RhinoApp.RunScript(doc.RuntimeSerialNumber, "_Grasshopper", true);
            if (Instances.ActiveDocument is null)
            {
                Grasshopper.Kernel.GH_Document ghDoc = Instances.DocumentServer.AddNewDocument();
                Instances.ActiveCanvas.Document = ghDoc;
            }
        }
        catch (Exception ex)
        {
            return Failure(ex, "Grasshopper could not be started; ask the user to start it manually");
        }

        return Instances.ActiveCanvas is not null
            ? Success(ContentBlock.CreateText("Opened Grasshopper"))
            : Failure(ToolError.GH_NotAvailable, "Grasshopper did not open");
    }

}
