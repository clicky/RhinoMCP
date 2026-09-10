using NUnit.Framework;
using Rhino.AI.WebPanel;

namespace Rhino.AI.Server.Tests;

[TestFixture]
public class ConversationFeedTests
{
    [Test]
    public void A_silent_completion_is_reported_ok_not_running()
    {
        List<PanelEvent> emitted = [];
        Conversation convo = new(Guid.NewGuid(), "claude", "doc.3dm");
        ConversationFeed feed = new(convo, emitted.Add);

        convo.BeginTurn("go");
        convo.Record(TurnEventKind.ToolUse, "list_objects", id: "c1");
        feed.Pump();
        Assert.That(StatusOf(emitted, "c1"), Is.EqualTo("running"));

        convo.CompleteToolCall("c1", string.Empty);
        feed.Pump();
        Assert.That(StatusOf(emitted, "c1"), Is.EqualTo("ok"));
    }

    [Test]
    public void A_failure_with_no_output_is_still_a_failure()
    {
        List<PanelEvent> emitted = [];
        Conversation convo = new(Guid.NewGuid(), "claude", "doc.3dm");
        ConversationFeed feed = new(convo, emitted.Add);

        convo.BeginTurn("go");
        convo.Record(TurnEventKind.ToolUse, "run_python", id: "c1");
        feed.Pump();
        convo.CompleteToolCall("c1", string.Empty, failed: true);
        feed.Pump();

        Assert.That(StatusOf(emitted, "c1"), Is.EqualTo("failed"));
        Assert.That(TitleOf(emitted, "c1"), Does.Contain("failed"));
    }

    [Test]
    public void An_open_call_is_unknown_once_the_turn_ends()
    {
        List<PanelEvent> emitted = [];
        Conversation convo = new(Guid.NewGuid(), "claude", "doc.3dm");
        ConversationFeed feed = new(convo, emitted.Add);

        convo.BeginTurn("go");
        convo.Record(TurnEventKind.ToolUse, "run_command", id: "c1");
        feed.Pump();
        Assert.That(feed.IsCallRunning("c1"), Is.True);

        convo.CompleteTurn();
        feed.Pump();
        feed.Pump();

        Assert.That(StatusOf(emitted, "c1"), Is.EqualTo("unknown"));
        Assert.That(PatchesFor(emitted, "c1"), Has.Count.EqualTo(1));
        Assert.That(PatchesFor(emitted, "c1")[0].Chips, Is.Empty);
        Assert.That(feed.IsCallRunning("c1"), Is.False);
    }

    // Conversation drops events once its terminal event has landed, so unknown is the final word.
    [Test]
    public void A_result_landing_after_the_turn_ended_does_not_revive_the_call()
    {
        List<PanelEvent> emitted = [];
        Conversation convo = new(Guid.NewGuid(), "claude", "doc.3dm");
        ConversationFeed feed = new(convo, emitted.Add);

        convo.BeginTurn("go");
        convo.Record(TurnEventKind.ToolUse, "list_objects", id: "c1");
        feed.Pump();
        convo.CompleteTurn();
        feed.Pump();
        convo.CompleteToolCall("c1", "{\"count\":3}");
        feed.Pump();

        Assert.That(StatusOf(emitted, "c1"), Is.EqualTo("unknown"));
    }

    [Test]
    public void A_transcript_saved_before_Done_existed_still_reports_its_tools_finished()
    {
        DateTimeOffset at = DateTimeOffset.UnixEpoch;
        TurnEventDto tool = new(TurnEventKind.ToolUse, "list_objects", at, "{}", "{\"Ok\":true,\"count\":3}", "c1");
        ConversationDto dto = new("2f8b1c40-5d3e-4a91-9c62-7f0a8e14d5b3", "claude", "doc.3dm", at, [],
            [new TurnDto("go", at, at, [tool])]);

        List<PanelEvent> emitted = [];
        ConversationFeed feed = new(Conversation.Restore(dto), emitted.Add);
        feed.Replay(readOnly: true);

        Assert.That(StatusOf(emitted, "c1"), Is.EqualTo("ok"));
    }

    private static string? StatusOf(IReadOnlyList<PanelEvent> emitted, string callId)
    {
        string? status = null;
        foreach (PanelEvent ev in emitted)
            switch (ev)
            {
                case TurnToolEvent call when call.Call.Id == callId:
                    status = call.Call.Status;
                    break;
                case TurnToolPatchEvent patch when patch.CallId == callId && patch.Patch.Status is string landed:
                    status = landed;
                    break;
            }
        return status;
    }

    private static string? TitleOf(IReadOnlyList<PanelEvent> emitted, string callId)
    {
        string? title = null;
        foreach (PanelEvent ev in emitted)
            switch (ev)
            {
                case TurnToolEvent call when call.Call.Id == callId:
                    title = call.Call.Title;
                    break;
                case TurnToolPatchEvent patch when patch.CallId == callId && patch.Patch.Title is string landed:
                    title = landed;
                    break;
            }
        return title;
    }

    private static List<PanelToolPatch> PatchesFor(IReadOnlyList<PanelEvent> emitted, string callId)
    {
        List<PanelToolPatch> patches = [];
        foreach (PanelEvent ev in emitted)
            if (ev is TurnToolPatchEvent patch && patch.CallId == callId)
                patches.Add(patch.Patch);
        return patches;
    }
}
