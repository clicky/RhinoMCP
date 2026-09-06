#if R9
#endif

namespace RhinoAI.Tools;

public static partial class GetContextTool
{
    public sealed record GrasshopperSummary(string Version, bool CanvasOpen, int ComponentCount, int WireCount);
}
