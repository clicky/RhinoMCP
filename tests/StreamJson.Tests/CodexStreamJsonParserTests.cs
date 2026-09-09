using System.Diagnostics;
using System.Text.Json;
using Acp;
using RhinoAI;

namespace RhinoAI.StreamJson.Tests;

// Every canned line below is a verbatim event captured from codex-cli 0.153.4, not a guess at the shape.
[TestFixture]
public sealed class CodexStreamJsonParserTests
{
    private static CodexStreamJsonParser NewParser() =>
        new(
            new AgentDefinition(
                Name: "codex",
                Adapter: AgentAdapter.Codex,
                Command: "codex",
                AgentPaths: [],
                Model: "",
                ExtraArgs: [],
                SystemPrompt: "",
                Enabled: true,
                IsBuiltin: true),
            codexHome: "/tmp/codex-home");

    private static List<string> Arguments(bool resume)
    {
        ProcessStartInfo psi = new();
        NewParser().ConfigureArguments(psi, "http://localhost:1234/agent", "01a087f2-08c9-7800-aeb9-38adb051b15b", [], resume);
        return [.. psi.ArgumentList];
    }

    [Test]
    public void A_fresh_spawn_runs_exec_and_a_later_one_resumes_the_minted_thread()
    {
        List<string> fresh = Arguments(resume: false);
        Assert.That(fresh[0], Is.EqualTo("exec"));
        Assert.That(fresh, Does.Not.Contain("resume"));
        Assert.That(fresh, Does.Not.Contain("--session-id"), "the CLI mints the id; it does not accept one");

        List<string> again = Arguments(resume: true);
        Assert.That(again[0], Is.EqualTo("exec"));
        Assert.That(again[1], Is.EqualTo("resume"), "resume is a subcommand, so it has to lead the args");
        Assert.That(again[2], Is.EqualTo("01a087f2-08c9-7800-aeb9-38adb051b15b"));
        Assert.That(again, Does.Not.Contain("--resume"));
    }

    [Test]
    public void The_prompt_placeholder_stays_last_because_it_is_positional()
    {
        Assert.That(Arguments(resume: false)[^1], Is.EqualTo("-"));
        Assert.That(Arguments(resume: true)[^1], Is.EqualTo("-"));
    }

    [Test]
    public void Codex_home_is_an_environment_variable_so_it_reaches_resume_too()
    {
        ProcessStartInfo psi = new();
        NewParser().ConfigureArguments(psi, "http://localhost:1234/agent", "abc", [], resume: true);

        Assert.That(psi.Environment["CODEX_HOME"], Is.EqualTo("/tmp/codex-home"));
    }

    [Test]
    public void Only_the_rhino_url_is_passed_since_the_rest_is_in_the_shipped_config()
    {
        List<string> args = Arguments(resume: false);

        Assert.That(args, Does.Contain("mcp_servers.rhino.url=\"http://localhost:1234/agent\""));
        Assert.That(args, Does.Contain("--json"));
        Assert.That(args, Does.Not.Contain("--experimental-json"));
    }

    [Test]
    public void A_user_added_server_is_pre_approved_or_every_call_to_it_would_be_refused()
    {
        ProcessStartInfo psi = new();
        NewParser().ConfigureArguments(
            psi,
            "http://localhost:1234/agent",
            "abc",
            ["""{"docs":{"url":"http://localhost:9/mcp","type":"http"}}"""],
            resume: false);

        Assert.That(psi.ArgumentList, Does.Contain("mcp_servers.docs.url=\"http://localhost:9/mcp\""));
        Assert.That(psi.ArgumentList, Does.Contain("mcp_servers.docs.default_tools_approval_mode=\"approve\""));
    }

    [Test]
    public void Thread_started_surfaces_the_id_the_cli_minted_for_itself()
    {
        ParsedLine parsed = NewParser().Parse(
            """{"type":"thread.started","thread_id":"01a087f2-08c9-7800-aeb9-38adb051b15b"}""");

        Assert.That(parsed.SessionId, Is.EqualTo("01a087f2-08c9-7800-aeb9-38adb051b15b"));
        Assert.That(parsed.IsTurnComplete, Is.False);
        Assert.That(parsed.Updates, Is.Empty);
    }

    [Test]
    public void A_completed_agent_message_emits_one_chunk()
    {
        ParsedLine parsed = NewParser().Parse(
            """{"type":"item.completed","item":{"id":"item_1","type":"agent_message","text":"working on it"}}""");

        Assert.That(parsed.Updates, Has.Count.EqualTo(1));
        AgentMessageChunkSessionUpdate chunk = (AgentMessageChunkSessionUpdate)parsed.Updates[0];
        Assert.That(((TextContentBlock)chunk.Content).Text, Is.EqualTo("working on it"));
    }

    [Test]
    public void A_started_tool_call_opens_a_chip_keyed_on_the_item_id()
    {
        ParsedLine parsed = NewParser().Parse(
            """{"type":"item.started","item":{"id":"item_0","type":"mcp_tool_call","server":"rhino","tool":"add_box","arguments":{"size":10},"result":null,"error":null,"status":"in_progress"}}""");

        Assert.That(parsed.Updates, Has.Count.EqualTo(1));
        ToolCallSessionUpdate call = (ToolCallSessionUpdate)parsed.Updates[0];
        Assert.That(call.ToolCallId, Is.EqualTo("item_0"), "the id correlates the later result, not the tool name");
        Assert.That(call.Title, Is.EqualTo("add_box"));
        Assert.That(call.RawInput?.GetProperty("size").GetInt32(), Is.EqualTo(10));
    }

    [Test]
    public void A_completed_tool_call_folds_its_result_into_the_same_chip()
    {
        ParsedLine parsed = NewParser().Parse(
            """{"type":"item.completed","item":{"id":"item_0","type":"mcp_tool_call","server":"rhino","tool":"add_box","arguments":{"size":10},"result":{"content":[{"type":"text","text":"created id 42"}]},"error":null,"status":"completed"}}""");

        Assert.That(parsed.Updates, Has.Count.EqualTo(1));
        ToolCallUpdateSessionUpdate update = (ToolCallUpdateSessionUpdate)parsed.Updates[0];
        Assert.That(update.ToolCallId, Is.EqualTo("item_0"));
        Assert.That(update.Status, Is.EqualTo(ToolCallStatus.Completed));
        Assert.That(update.RawOutput?.ToString(), Does.Contain("created id 42"));
    }

    [Test]
    public void A_failed_tool_call_forwards_the_error_because_result_is_null()
    {
        ParsedLine parsed = NewParser().Parse(
            """{"type":"item.completed","item":{"id":"item_0","type":"mcp_tool_call","server":"rhino","tool":"add_box","arguments":{},"result":null,"error":{"message":"MCP tool call requires approval, but approval policy is never"},"status":"failed"}}""");

        ToolCallUpdateSessionUpdate update = (ToolCallUpdateSessionUpdate)parsed.Updates[0];
        Assert.That(update.Status, Is.EqualTo(ToolCallStatus.Failed));
        Assert.That(update.RawOutput?.ToString(), Does.Contain("requires approval"),
            "a null RawOutput would drop the chip's result entirely");
    }

    [Test]
    public void Turn_completed_is_the_terminal_event_and_carries_the_tokens()
    {
        ParsedLine parsed = NewParser().Parse(
            """{"type":"turn.completed","usage":{"input_tokens":15249,"cached_input_tokens":11008,"cache_write_input_tokens":0,"output_tokens":6,"reasoning_output_tokens":0}}""");

        Assert.That(parsed.IsTurnComplete, Is.True);
        Assert.That(parsed.Reason, Is.EqualTo(StopReason.EndTurn));
        Assert.That(parsed.Usage.InputTokens, Is.EqualTo(15249));
        Assert.That(parsed.Usage.OutputTokens, Is.EqualTo(6));
        Assert.That(parsed.Usage.CostUsd, Is.Null); // Codex reports tokens only
    }

    [Test]
    public void Turn_completed_without_usage_degrades_rather_than_faulting()
    {
        ParsedLine parsed = NewParser().Parse("""{"type":"turn.completed"}""");

        Assert.That(parsed.IsTurnComplete, Is.True);
        Assert.That(parsed.Usage.IsEmpty, Is.True);
    }

    [Test]
    public void Turn_failed_still_ends_the_turn_so_the_prompt_cannot_hang()
    {
        ParsedLine parsed = NewParser().Parse("""{"type":"turn.failed","error":{"message":"nope"}}""");

        Assert.That(parsed.IsTurnComplete, Is.True);
        Assert.That(parsed.Reason, Is.EqualTo(StopReason.Refusal));
    }

    [Test]
    public void Untranslated_events_are_fail_soft_none()
    {
        CodexStreamJsonParser parser = NewParser();

        Assert.Multiple(() =>
        {
            Assert.That(parser.Parse("""{"type":"turn.started"}""").Updates, Is.Empty);
            Assert.That(parser.Parse("""{"type":"item.completed","item":{"id":"r","type":"reasoning","text":"hmm"}}""").Updates, Is.Empty);
            Assert.That(parser.Parse("""{"type":"item.updated","item":{"id":"item_0","type":"mcp_tool_call"}}""").Updates, Is.Empty);
            Assert.That(parser.Parse("""{"unrelated":"shape"}""").Updates, Is.Empty);
        });
    }

    [Test]
    public void A_tool_call_without_an_id_is_skipped_rather_than_keyed_on_empty()
    {
        ParsedLine parsed = NewParser().Parse(
            """{"type":"item.started","item":{"type":"mcp_tool_call","server":"rhino","tool":"add_box"}}""");

        Assert.That(parsed.Updates, Is.Empty);
    }

    [Test]
    public void A_multi_block_prompt_joins_into_the_plain_text_codex_reads_from_stdin()
    {
        string turn = NewParser().FormatTurn([
            new TextContentBlock { Text = "draw a box" },
            new TextContentBlock { Text = "10mm" },
        ]);

        Assert.That(turn, Is.EqualTo("draw a box\n10mm"));
    }
}
