namespace Rhino.AI.Tools;

// Pairs with Coercions: that one records what a call changed, this one records why it cannot run.
// Collected rather than returned one at a time, so three bad arguments cost one round trip.
internal sealed class Refusals
{

    private List<(string Problem, string? Guidance)> Problems { get; } = [];

    public bool Any => Problems.Count > 0;

    public void Add(string problem, string? guidance = null)
    {
        if (Problems.Any(p => string.Equals(p.Problem, problem, StringComparison.OrdinalIgnoreCase))) return;
        Problems.Add((problem, guidance));
    }

    public IToolResult Rejection
    {
        get
        {
            IEnumerable<(string Problem, string? Guidance)> distinct = Problems.Distinct();
            string message = string.Join("; ", distinct.Select(p => p.Problem));
            string guidance = string.Join("; ", distinct.Select(p => p.Guidance ?? "-"));
            return Failure(ToolError.BadArgument, message, guidance);

        }
    }

}
