using RhinoAI.Tools;

using static RhinoAI.Tools.ToolResult;

namespace RhinoAI.Server;

internal sealed class ArgumentBindingException(string wireName, string problem, Exception? inner = null)
    : ArgumentException(problem, inner)
{
    public string WireName { get; } = wireName;

    public string Problem { get; } = problem;
}

// A bad argument is the caller's mistake rather than a crash, so it is answered with the tool's signature instead of a stack trace the agent cannot act on.
internal static class BindingFailure
{
    public static IToolResult Describe(
        string tool, IReadOnlyList<ParameterDescriptor> parameters, ArgumentBindingException failure)
        => Describe(tool, parameters, [failure]);

    // Every argument is bound before any is reported, so a call that got three of them wrong is
    // told all three rather than one per round trip.
    public static IToolResult Describe(
        string tool, IReadOnlyList<ParameterDescriptor> parameters, IReadOnlyList<ArgumentBindingException> failures)
        => Failure(
            ToolError.BadArgument,
            $"{tool}: {string.Join("; ", failures.Select(f => f.Problem))}",
            Signature(tool, parameters));

    private static string Signature(string tool, IReadOnlyList<ParameterDescriptor> parameters)
    {
        string[] required = Names(parameters, wanted: true);
        string[] optional = Names(parameters, wanted: false);

        if (required.Length == 0 && optional.Length == 0)
            return $"'{tool}' takes no arguments.";

        List<string> parts = new(2)
        {
            required.Length > 0
                ? $"'{tool}' requires {string.Join(", ", required)}"
                : $"'{tool}' has no required arguments",
        };

        if (optional.Length > 0)
            parts.Add($"Optional: {string.Join(", ", optional)}");

        return string.Join(". ", parts) + ".";
    }

    private static string[] Names(IReadOnlyList<ParameterDescriptor> parameters, bool wanted)
        => parameters
            .Where(p => p.IncludeInSchema && p.IsRequired == wanted)
            .Select(p => $"{p.WireName} ({SchemaBuilder.TypeName(p.ParameterType)})")
            .ToArray();
}
