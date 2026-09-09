using System.Drawing;

using Rhino.AI.Resources;

using Grasshopper.GUI.Base;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;

namespace Rhino.AI.Tools;

[McpServerToolType]
internal static class GH1_PlaceSliderTool
{
    public record struct SliderInfo(Guid Id, decimal Min, decimal Value, decimal Max, string Type, int X, int Y);

    [McpServerTool("g1_place_slider", "Place GH1 Number Slider", false, false)]
    [Description("Place a Number Slider on the active GH1 canvas with the given range and current value. type: 'float' | 'int' | 'even' | 'odd'.")]
    public static IToolResult Place(
        RhinoDoc rhDoc,
        [Description("Minimum slider value.")] decimal min,
        [Description("Initial slider value.")] decimal value,
        [Description("Maximum slider value.")] decimal max,
        [Description("Canvas X position in pixels.")] int x = 100,
        [Description("Canvas Y position in pixels.")] int y = 100,
        [Description("Slider accuracy: 'float', 'int', 'even', or 'odd'.")] string type = "float",
        [Description("Optional NickName for the slider.")] string? name = null,
        [Description("If true, trigger a new solution after placing. Set false to batch multiple operations and solve once at the end.")] bool solve = true)
    {
        Coercions coerced = new();

        if (!TryParseAccuracy(type, out GH_SliderAccuracy accuracy))
            coerced.Note($"type '{type}' is not one of 'float', 'int', 'even', 'odd', so 'float' was used");

        (min, value, max) = coerced.SliderRange(min, value, max);

        if (!GH1_Utils.TryGetOrCreateDoc(rhDoc, out GH_Document doc))
            return GH1_Failures.NoDocument;

        GH_NumberSlider slider = new();
        slider.CreateAttributes();

        slider.Slider.Minimum = min;
        slider.Slider.Maximum = max;
        slider.Slider.Value = value;
        slider.Slider.Type = accuracy;

        if (!string.IsNullOrEmpty(name))
            slider.NickName = name;

        slider.Attributes.Pivot = new PointF(x, y);

        doc.AddObject(slider, false);
        if (solve) doc.NewSolution(false);
        GH1_Utils.ZoomExtents();

        coerced.NoteAdjusted("min", min, slider.Slider.Minimum);
        coerced.NoteAdjusted("max", max, slider.Slider.Maximum);
        coerced.NoteAdjusted("value", value, slider.Slider.Value);

        return Success(
            new SliderInfo(
                slider.InstanceGuid,
                slider.Slider.Minimum,
                slider.Slider.Value,
                slider.Slider.Maximum,
                FormatAccuracy(slider.Slider.Type),
                x,
                y),
            coerced.Guidance);
    }

    private static string FormatAccuracy(GH_SliderAccuracy accuracy) => accuracy switch
    {
        GH_SliderAccuracy.Float => "float",
        GH_SliderAccuracy.Integer => "int",
        GH_SliderAccuracy.Even => "even",
        GH_SliderAccuracy.Odd => "odd",
        _ => accuracy.ToString(),
    };

    private static bool TryParseAccuracy(string type, out GH_SliderAccuracy accuracy)
    {
        switch (type?.ToLowerInvariant())
        {
            case "float":
                accuracy = GH_SliderAccuracy.Float;
                return true;
            case "int":
                accuracy = GH_SliderAccuracy.Integer;
                return true;
            case "even":
                accuracy = GH_SliderAccuracy.Even;
                return true;
            case "odd":
                accuracy = GH_SliderAccuracy.Odd;
                return true;
            default:
                accuracy = GH_SliderAccuracy.Float;
                return false;
        }
    }
}
