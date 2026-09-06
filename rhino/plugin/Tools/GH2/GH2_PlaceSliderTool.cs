using RhinoAI.Resources;

using Eto.Drawing;

using Grasshopper2.Doc;
using Grasshopper2.Parameters.Special;
using Grasshopper2.UI;

namespace RhinoAI.Tools;

[McpServerToolType]
internal static class GH2_PlaceSliderTool
{
    public record struct SliderInfo(Guid Id, double Min, double Value, double Max, int Decimals, float X, float Y);

    [McpServerTool("g2_place_slider", "Place GH2 Number Slider", false, false)]
    [Description("Place a Number Slider on the active GH2 canvas with the given range and current value.")]
    public static IToolResult Place(
        RhinoDoc rhDoc,
        [Description("Minimum slider value.")] double min,
        [Description("Initial slider value.")] double value,
        [Description("Maximum slider value.")] double max,
        [Description("Canvas X position in pixels.")] float x = 100,
        [Description("Canvas Y position in pixels.")] float y = 100,
        [Description("Number of decimal places (0 for integer behaviour). Range: 0..12.")] int decimals = 3,
        [Description("Optional UserName for the slider.")] string? name = null,
        [Description("If true, trigger a new solution after placing. Set false to batch multiple operations and solve once at the end.")] bool solve = true)
    {
        Coercions coerced = new();

        decimals = coerced.Clamp("decimals", decimals, 0, 12);
        (min, value, max) = coerced.SliderRange(min, value, max);

        if (!GH2_Utils.TryGetDoc(rhDoc, out Document doc))
            return GH2_Failures.NoDocument;

        UiNumber number = new(decimals, (decimal)value, (decimal)min, (decimal)max);
        NumberSliderObject slider = new(name ?? "num", number);

        doc.Objects.Add(slider, new PointF(x, y));
        if (solve)
            doc.Solution.Start();
        GH2_Utils.Redraw();

        UiNumber current = slider.InternalNumber;
        return Success(
            new SliderInfo(
                slider.InstanceId,
                (double)current.Lower,
                (double)current.Value,
                (double)current.Upper,
                current.Decimals,
                x,
                y),
            coerced.Guidance);
    }
}
