namespace RhinoAI.Tools;

/// <summary>Tracks which destination inputs a batch has already wired into, so replace only clears wires that predate the call and several sources deliberately merged into one input still accumulate.</summary>
internal sealed class Rewiring(bool replace)
{

    private HashSet<Guid> Claimed { get; } = [];

    public int Replaced { get; private set; }

    public bool ShouldClear(Guid destination) => replace && Claimed.Add(destination);

    public void Note(int removed) => Replaced += removed;

    public string? Guidance => Replaced == 0
        ? null
        : $"Replaced {Replaced} source(s) that were already wired into the destination inputs";

}
