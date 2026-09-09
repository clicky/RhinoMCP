using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RhinoAI;

// Discovery result for one definition; kept separate so AgentDefinition stays free of
// computed/probed state.
internal sealed record ResolvedAgent(AgentDefinition Definition, bool Available);

// Seeds built-in agent definitions, overlays custom entries from settings, probes each
// definition's search paths for availability, and resolves the active agent (first
// Enabled && Available, Claude-default-first). Re-runnable on load and on settings change.
internal static class AgentRegistry
{
    private static IReadOnlyList<ResolvedAgent> ChainBacking { get; set; } = [];

    public static IReadOnlyList<ResolvedAgent> Chain => ChainBacking;

    public static void Refresh() =>
        ChainBacking = AISettings.GetAgents()
            .Select(static def => new ResolvedAgent(def, ProbeAvailable(def)))
            .ToArray();

    // The built-in definitions in chain order (Claude first, Codex second), reproducing
    // today's hardcoded install-dir candidates and empty model/args so out-of-the-box launch
    // behavior is unchanged. AISettings.GetAgents re-seeds from here on every read.
    public static IReadOnlyList<AgentDefinition> Builtins() =>
    [
        Builtin("claude", AgentAdapter.Claude, "claude", new ClaudeFinder()),
        Builtin("codex", AgentAdapter.Codex, "codex", new CodexFinder()),
        Builtin("gemini", AgentAdapter.Gemini, "gemini", new GeminiFinder()),
    ];

    private static AgentDefinition Builtin(string name, AgentAdapter adapter, string command, IAgentFinder finder) =>
        new(name, adapter, command, finder.Find(), string.Empty, [], string.Empty, true, true);

    // The full chain: built-ins (always present, in their seed order) overlaid with custom entries.
    // A custom entry that aliases a built-in name overrides it in place (keeping the built-in's
    // IsBuiltin), never duplicated; a custom entry with a new name is appended after the built-ins.
    // Pure so AISettings.GetAgents and the headless test shim share one source of truth for the
    // invariant rather than each reimplementing it.
    public static IReadOnlyList<AgentDefinition> Overlay(IReadOnlyList<AgentDefinition> builtins, IReadOnlyList<AgentDefinition> custom)
    {
        List<AgentDefinition> chain = builtins.ToList();
        foreach (AgentDefinition entry in custom)
        {
            int existing = chain.FindIndex(a => a.Name == entry.Name);
            if (existing >= 0)
                chain[existing] = entry with { IsBuiltin = chain[existing].IsBuiltin };
            else
                chain.Add(entry);
        }
        return chain;
    }

    private static bool ProbeAvailable(AgentDefinition def) => def.AgentPaths.Any();

    // First Enabled && Available in chain order, then the configured default, then the
    // first enabled+available built-in. Falls back to nothing only when discovery is empty.
    public static bool TryResolveActive(out AgentDefinition def)
    {
        // TODO : May be worth checking running processes for an angent matching what we support, would give a good idea of what the user uses.

        string preferred = AISettings.DefaultAgentName;
        ResolvedAgent[] usable = ChainBacking.Where(static r => r.Definition.Enabled && r.Available).ToArray();

        ResolvedAgent? match = usable.FirstOrDefault(r => r.Definition.Name == preferred)
            ?? usable.FirstOrDefault();
        if (match is not null)
        {
            def = match.Definition;
            return true;
        }

        def = default!;
        return false;
    }

    public static bool TryGet(string name, out AgentDefinition def)
    {
        ResolvedAgent? match = ChainBacking.FirstOrDefault(r => r.Definition.Name == name);
        if (match is not null)
        {
            def = match.Definition;
            return true;
        }

        def = default!;
        return false;
    }
}
