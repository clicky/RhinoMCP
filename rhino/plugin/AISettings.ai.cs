using System.Collections.Generic;
using System.Text.Json;
using Rhino.PlugIns;

namespace Rhino.AI;

internal static class AISettings
{

    private static Guid PluginId { get; } = new("2668d7ed-f507-4a68-8295-8172147a0e39");

    private static PersistentSettings Settings =>
        PlugIn.Find(PluginId) is PlugIn plugin
            ? plugin.Settings
            : throw new InvalidOperationException("RhinoAI plugin is not loaded; AISettings is unavailable.");

    // Name of the agent the registry prefers when resolving the active one.
    public static string DefaultAgentName
    {
        get => Settings.GetString(nameof(DefaultAgentName), "claude");
        set => Settings.SetString(nameof(DefaultAgentName), value);
    }

    // Tools hidden from in-Rhino agents (Part 3). Empty = nothing hidden.
    public static string[] DisabledTools
    {
        get => Settings.GetStringList(nameof(DisabledTools), []);
        set => Settings.SetStringList(nameof(DisabledTools), value);
    }

    // Overrides the username-derived name of the plugin manage_plugin_commands writes to.
    public static string? ScriptPluginName
    {
        get
        {
            string stored = Settings.GetString(nameof(ScriptPluginName), string.Empty);
            return string.IsNullOrWhiteSpace(stored) ? null : stored;
        }
        set => Settings.SetString(nameof(ScriptPluginName), value ?? string.Empty);
    }

    // External MCP servers merged into each agent's own config beside `rhino` (Part 4).
    public static string ExtraMcpServersJson
    {
        get => Settings.GetString(nameof(ExtraMcpServersJson), "{\"mcpServers\":{}}");
        set => Settings.SetString(nameof(ExtraMcpServersJson), value);
    }

    // Per-session conversation history lives under its own child node (Part 6).
    public static PersistentSettings Conversations =>
        Settings.TryGetChild(nameof(Conversations), out PersistentSettings child)
            ? child
            : Settings.AddChild(nameof(Conversations));

    public static int StartingPort
    {
        get => Settings.GetInteger(nameof(StartingPort), 10500);
        set => Settings.SetInteger(nameof(StartingPort), value);
    }

    public const int MinPort = 1;
    public const int MaxPort = 65535;


    public static string AcpCommand
    {
        get => Settings.GetString(nameof(AcpCommand), "npx");
        set => Settings.SetString(nameof(AcpCommand), value);
    }

    public static string AcpPackage
    {
        get => Settings.GetString(nameof(AcpPackage), "@agentclientprotocol/claude-agent-acp");
        set => Settings.SetString(nameof(AcpPackage), value);
    }

    public static string ClaudeCommandFileName
    {
        get => Settings.GetString(nameof(ClaudeCommandFileName), "claude");
        set => Settings.SetString(nameof(ClaudeCommandFileName), value);
    }

    public static string CodexCommandFileName
    {
        get => Settings.GetString(nameof(CodexCommandFileName), "codex");
        set => Settings.SetString(nameof(CodexCommandFileName), value);
    }

    public static TimeSpan AgentStartupTimeout
    {
        get => TimeSpan.FromMilliseconds(Settings.GetInteger(nameof(AgentStartupTimeout), 60_000));
        set => Settings.SetInteger(nameof(AgentStartupTimeout), (int)value.TotalMilliseconds);
    }


    public static TimeSpan ScriptCleanupDelay
    {
        get => TimeSpan.FromMilliseconds(Settings.GetInteger(nameof(ScriptCleanupDelay), 15_000));
        set => Settings.SetInteger(nameof(ScriptCleanupDelay), (int)value.TotalMilliseconds);
    }

    public static TimeSpan CommandHelpHttpTimeout
    {
        get => TimeSpan.FromMilliseconds(Settings.GetInteger(nameof(CommandHelpHttpTimeout), 2_000));
        set => Settings.SetInteger(nameof(CommandHelpHttpTimeout), (int)value.TotalMilliseconds);
    }

    public static TimeSpan SlotCloseTempDeleteDelay
    {
        get => TimeSpan.FromMilliseconds(Settings.GetInteger(nameof(SlotCloseTempDeleteDelay), 1_000));
        set => Settings.SetInteger(nameof(SlotCloseTempDeleteDelay), (int)value.TotalMilliseconds);
    }

    public static TimeSpan RouterExitDelay
    {
        get => TimeSpan.FromMilliseconds(Settings.GetInteger(nameof(RouterExitDelay), 200));
        set => Settings.SetInteger(nameof(RouterExitDelay), (int)value.TotalMilliseconds);
    }

    public static int MaxCommandResults
    {
        get => Settings.GetInteger(nameof(MaxCommandResults), 200);
        set => Settings.SetInteger(nameof(MaxCommandResults), value);
    }

    public static int DefaultObjectListLimit
    {
        get => Settings.GetInteger(nameof(DefaultObjectListLimit), 1000);
        set => Settings.SetInteger(nameof(DefaultObjectListLimit), value);
    }

    public static int DefaultViewportImageWidth
    {
        get => Settings.GetInteger(nameof(DefaultViewportImageWidth), 480);
        set => Settings.SetInteger(nameof(DefaultViewportImageWidth), value);
    }

    public static int DefaultViewportImageHeight
    {
        get => Settings.GetInteger(nameof(DefaultViewportImageHeight), 270);
        set => Settings.SetInteger(nameof(DefaultViewportImageHeight), value);
    }

    public const int MaxViewportImageWidth = 1280;
    public const int MaxViewportImageHeight = 720;
}
