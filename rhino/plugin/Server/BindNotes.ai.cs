using System.Threading;

namespace Rhino.AI.Server;

// The lenient converters run inside a shared JsonSerializerOptions with no return path to the caller, so a coerced value can only be reported through ambient state scoped to the synchronous bind.
internal static class BindNotes
{
    private static readonly AsyncLocal<List<string>?> Current = new();

    public static Call Begin() => new(Current.Value = []);

    public static void Add(string note) => Current.Value?.Add(note);

    public static Parameter For(string wireName) => new(wireName, Current.Value?.Count ?? 0);

    internal readonly struct Call(List<string> notes) : IDisposable
    {
        public IReadOnlyList<string> Notes => notes;

        public void Dispose() => Current.Value = null;
    }

    internal readonly struct Parameter(string wireName, int from) : IDisposable
    {
        public void Dispose()
        {
            if (Current.Value is not List<string> notes) return;

            for (int i = from; i < notes.Count; i++)
                notes[i] = $"{wireName} {notes[i]}";
        }
    }
}
