using System.Threading.Tasks;

using RhinoAI.Resources;

using Grasshopper2.Doc;

namespace RhinoAI.Tools;

[McpServerToolType]
public static class GH2_SolveTool
{

    [McpServerTool("g2_solve_canvas", "Solve GH2 Canvas", false, false)]
    [Description("Solves the active GH2 canvas and reads back per-component diagnostics. Returns {Solved, Phase, Errors, Warnings, Diagnostics[]}. Each diagnostic is {Id, Name, Nickname, Level (Remark|Warning|Error|Fault), Message}. Solved is true only when the solution completed with no Error or Fault. Use this to see exactly which components failed and why, then fix them.")]
    public static async Task<IToolResult> SolveCanvas(RhinoDoc rhDoc)
    {
        if (!GH2_Utils.TryGetDoc(rhDoc, out Document ghDoc))
            return GH2_Failures.NoDocument;

        GH2SolveSummary result = await GH2_Diagnostics.SolveAsync(ghDoc);

        return result.Solved
            ? Success(result)
            : Failure(
                ToolError.GH_Solution_Failed,
                ContentBlock.CreateJson(result),
                "The solution completed with errors",
                "Read the diagnostics to see which components failed and why");
    }
}
