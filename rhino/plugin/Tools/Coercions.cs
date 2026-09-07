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

    public decimal Clamp(string name, decimal value, decimal min, decimal max)
    {
        decimal clamped = Math.Clamp(value, min, max);
        if (clamped != value)
            Note($"{name} {Text(value)} was clamped to {Text(clamped)}");

        return clamped;
    }

    public (decimal Min, decimal Value, decimal Max) SliderRange(decimal min, decimal value, decimal max, string? label = null)
    {
        string prefix = label is null ? string.Empty : $"{label} ";

        if (min > max)
        {
            Note($"{prefix}min {Text(min)} was above max {Text(max)}, so they were swapped");
            (min, max) = (max, min);
        }

        return (min, Clamp($"{prefix}value", value, min, max), max);
    }

    public void NoteAdjusted(string name, decimal requested, decimal actual)
    {
        if (requested != actual)
            Note($"{name} {Text(requested)} was adjusted to {Text(actual)} to fit the slider's accuracy and range");
    }

    private static string Text(double value) => value.ToString("G6", CultureInfo.InvariantCulture);

    private static string Text(decimal value) => value.ToString("G6", CultureInfo.InvariantCulture);

}
