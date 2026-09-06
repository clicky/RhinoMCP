using System.Text.Json;

using NUnit.Framework;

using RhinoAI.Tools;

namespace RhinoAI.Server.Tests;

[TestFixture]
public class ToolResultFormatterTests
{
    private static readonly string[] LegalTypes = ["text", "image", "resource"];

    private static JsonElement Envelope(CallToolResult result) =>
        JsonDocument.Parse(result.Content[0].Text!).RootElement;

    private static void AssertEveryBlockIsValid(CallToolResult result)
    {
        Assert.That(result.Content, Is.Not.Empty);

        foreach (ContentBlock block in result.Content)
        {
            Assert.That(LegalTypes, Does.Contain(block.Type), $"'{block.Type}' is not an MCP content type");

            if (block.Type == "text")
                Assert.That(block.Text, Is.Not.Null, "a text block without text fails union validation");
            else
                Assert.That(block.Data, Is.Not.Null);
        }
    }

    [Test]
    public void Every_content_block_carries_a_legal_type_so_the_client_can_validate_the_union()
    {
        AssertEveryBlockIsValid(ToolResultFormatter.Format(ToolResult.Success(ContentBlock.CreateText("plain"))));
        AssertEveryBlockIsValid(ToolResultFormatter.Format(ToolResult.Success(ContentBlock.CreateJson(new { a = 1 }))));
        AssertEveryBlockIsValid(ToolResultFormatter.Format(ToolResult.Success(new { a = 1 }, "a warning")));
        AssertEveryBlockIsValid(ToolResultFormatter.Format(ToolResult.Success(
            ContentBlock.CreateText("meta"),
            ContentBlock.CreateImage([1, 2, 3], "image/jpeg"))));
        AssertEveryBlockIsValid(ToolResultFormatter.Format(
            ToolResult.Failure(ToolError.Failed, ContentBlock.CreateJson(new { b = 2 }), "broke", "retry")));
        AssertEveryBlockIsValid(ToolResultFormatter.Format(ToolResult.Failure(ToolError.RH_Doc_Headless)));
    }

    [Test]
    public void CreateJson_is_a_text_block_not_a_mime_type()
    {
        ContentBlock block = ContentBlock.CreateJson(new { count = 3 });

        Assert.Multiple(() =>
        {
            Assert.That(block.Type, Is.EqualTo("text"));
            Assert.That(block.Text, Is.EqualTo("{\"count\":3}"));
        });
    }

    [Test]
    public void Success_with_attachments_only_has_no_envelope()
    {
        CallToolResult result = ToolResultFormatter.Format(
            ToolResult.Success(ContentBlock.CreateText("payload")));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsError, Is.False);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Text, Is.EqualTo("payload"));
        });
    }

    [Test]
    public void Success_with_guidance_leads_with_a_guidance_envelope()
    {
        CallToolResult result = ToolResultFormatter.Format(
            ToolResult.Success(ContentBlock.CreateText("payload"), "limit -5 was clamped to 1"));

        JsonElement envelope = Envelope(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsError, Is.False);
            Assert.That(result.Content, Has.Count.EqualTo(2));
            Assert.That(envelope.GetProperty("guidance").GetString(), Is.EqualTo("limit -5 was clamped to 1"));
            Assert.That(envelope.TryGetProperty("error", out _), Is.False);
            Assert.That(result.Content[1].Text, Is.EqualTo("payload"));
        });
    }

    [Test]
    public void An_ignored_argument_note_becomes_guidance_on_an_otherwise_silent_success()
    {
        CallToolResult result = ToolResultFormatter.Format(
            ToolResult.Success(ContentBlock.CreateText("Camera updated.")),
            "Ignored 'view' because it is not an argument 'set_camera' accepts. Its arguments are: location, target");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsError, Is.False);
            Assert.That(Envelope(result).GetProperty("guidance").GetString(), Does.StartWith("Ignored 'view'"));
            Assert.That(result.Content[1].Text, Is.EqualTo("Camera updated."));
        });
    }

    [Test]
    public void An_ignored_argument_note_joins_the_tools_own_guidance()
    {
        CallToolResult result = ToolResultFormatter.Format(
            ToolResult.Success(ContentBlock.CreateText("done"), "limit -5 was clamped to 1"),
            "Ignored 'view' because it is not an argument this tool accepts");

        Assert.That(
            Envelope(result).GetProperty("guidance").GetString(),
            Is.EqualTo("limit -5 was clamped to 1; Ignored 'view' because it is not an argument this tool accepts"));
    }

    [Test]
    public void Failure_carries_error_message_and_guidance()
    {
        CallToolResult result = ToolResultFormatter.Format(
            ToolResult.Failure(ToolError.RH_Layer_NotFound, "Layer not found: Walls", "List the layers first"));

        JsonElement envelope = Envelope(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsError, Is.True);
            Assert.That(envelope.GetProperty("error").GetString(), Is.EqualTo("Layer was not found"));
            Assert.That(envelope.GetProperty("message").GetString(), Is.EqualTo("Layer not found: Walls"));
            Assert.That(envelope.GetProperty("guidance").GetString(), Is.EqualTo("List the layers first"));
        });
    }

    [Test]
    public void Failure_from_a_code_alone_omits_message_and_guidance()
    {
        CallToolResult result = ToolResultFormatter.Format(ToolResult.Failure(ToolError.RH_Doc_Headless));

        JsonElement envelope = Envelope(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsError, Is.True);
            Assert.That(envelope.GetProperty("error").GetString(), Is.EqualTo("This document is headless and has no viewport"));
            Assert.That(envelope.TryGetProperty("message", out _), Is.False);
            Assert.That(envelope.TryGetProperty("guidance", out _), Is.False);
        });
    }

    [Test]
    public void Failure_envelope_leads_the_attachments()
    {
        CallToolResult result = ToolResultFormatter.Format(ToolResult.Failure(
            ToolError.GH_Solution_Failed,
            ContentBlock.CreateJson(new { failed = 2 }),
            "The solution reported component errors"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Content, Has.Count.EqualTo(2));
            Assert.That(Envelope(result).GetProperty("message").GetString(), Is.EqualTo("The solution reported component errors"));
            Assert.That(result.Content[1].Text, Is.EqualTo("{\"failed\":2}"));
        });
    }

    [Test]
    public void Failure_from_an_exception_reports_its_message_and_attaches_the_inner_one()
    {
        Exception thrown = new("outer", new InvalidOperationException("inner"));

        CallToolResult result = ToolResultFormatter.Format(ToolResult.Failure(thrown, "Try again"));

        JsonElement envelope = Envelope(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsError, Is.True);
            Assert.That(envelope.GetProperty("error").GetString(), Is.EqualTo("An Exception was thrown"));
            Assert.That(envelope.GetProperty("message").GetString(), Is.EqualTo("outer"));
            Assert.That(envelope.GetProperty("guidance").GetString(), Is.EqualTo("Try again"));
            Assert.That(result.Content[1].Text, Is.EqualTo("inner"));
        });
    }

    [Test]
    public void Empty_success_still_yields_one_block()
    {
        CallToolResult result = ToolResultFormatter.Format(ToolResult.Success());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsError, Is.False);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Text, Is.Empty);
        });
    }

    [Test]
    public void Success_from_a_collection_serializes_it_whole_not_one_block_per_element()
    {
        CallToolResult result = ToolResultFormatter.Format(ToolResult.Success(new[] { 1, 2, 3 }));

        Assert.Multiple(() =>
        {
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Text, Is.EqualTo("[1,2,3]"));
        });
    }

    [Test]
    public void Code_survives_on_the_result_for_callers_to_branch_on()
    {
        IToolResult failure = ToolResult.Failure(ToolError.GH_Document_NotFound, guidance: "Start Grasshopper");
        IToolResult success = ToolResult.Success();

        Assert.Multiple(() =>
        {
            Assert.That(failure.Code, Is.EqualTo(ToolError.GH_Document_NotFound));
            Assert.That(failure.IsFailure, Is.True);
            Assert.That(success.Code, Is.Null);
            Assert.That(success.IsFailure, Is.False);
        });
    }
}
