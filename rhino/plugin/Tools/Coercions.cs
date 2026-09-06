using System.Globalization;

namespace RhinoAI.Tools;

/// <summary>Records every adjustment a tool made to out-of-range input, so a call that was accepted rather than refused can still say what it actually did.</summary>
internal sealed class Coercions
{

    private List<string> Notes { get; } = [];

    public bool Any => Notes.Count > 0;

    public string? Guidance => Notes.Count == 0 ? null : string.Join("; ", Notes);

    public void Note(string note) => Notes.Add(note);

    public double Clamp(string name, double value, double min, double max)
    {
        if (double.IsNaN(value))
        {
            Note($"{name} was not a number, used {Text(min)}");
            return min;
        }

        double clamped = Math.Clamp(value, min, max);
        if (clamped != value)
            Note($"{name} {Text(value)} was clamped to {Text(clamped)}");

        return clamped;
    }

    public int Clamp(string name, int value, int min, int max)
    {
        int clamped = Math.Clamp(value, min, max);
        if (clamped != value)
            Note($"{name} {value} was clamped to {clamped}");

        return clamped;
    }

    public (double Min, double Value, double Max) SliderRange(double min, double value, double max)
    {
        if (double.IsNaN(min) || double.IsNaN(max) || min > max)
        {
            if (min > max)
            {
                Note($"min {Text(min)} was above max {Text(max)}, so they were swapped");
                (min, max) = (max, min);
            }
            else
            {
                Note("a slider bound was not a number, used 0 to 1");
                (min, max) = (0, 1);
            }
        }

        return (min, Clamp("value", value, min, max), max);
    }

    private static string Text(double value) => value.ToString("G6", CultureInfo.InvariantCulture);

}
