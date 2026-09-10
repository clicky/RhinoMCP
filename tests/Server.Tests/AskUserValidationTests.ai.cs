using NUnit.Framework;
using Rhino.AI.Tools;

namespace Rhino.AI.Server.Tests;

[TestFixture]
public class AskUserValidationTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("Other")]
    [TestCase(" I don't know ")]
    public void Unusable_options_fail_with_retry_feedback(string? option)
    {
        IToolResult? result = AskUserValidation.Validate(
            [new("Choose?", [option!])], new(), out var posed);
        Assert.That(posed, Is.Empty);
        Assert.That(result!.Code, Is.EqualTo(ToolError.BadArgument));
        Assert.That(result.Message, Does.Contain("questions[0].options"));
        Assert.That(result.Guidance, Does.Contain("retry ask_user now"));
        Assert.That(ToolResultFormatter.Format(result).IsError, Is.True);
    }

    [Test]
    public void Invalid_batch_does_not_partially_publish()
    {
        IToolResult? result = AskUserValidation.Validate(
            [new("Valid?", ["Yes"]), new("Invalid?", [])], new(), out var posed);
        Assert.That(result!.Message, Does.Contain("questions[1].options"));
        Assert.That(posed, Is.Empty);
    }

    [Test]
    public void Missing_questions_text_and_options_fail()
    {
        QuestionSpec[]?[] batches = [null, [], [null!], [new(" ", ["Yes"])], [new("Choose?", null!)]];
        foreach (var batch in batches)
        {
            var result = AskUserValidation.Validate(batch, new(), out var posed);
            Assert.That(result!.IsFailure, Is.True);
            Assert.That(posed, Is.Empty);
        }
    }

    [Test]
    public void Cleanup_is_reported_and_real_choices_and_mode_are_preserved()
    {
        Coercions coercions = new();
        var result = AskUserValidation.Validate(
            [new("Choose?", ["", null!, "Other", "I don't know", "A", "B"], true)],
            coercions, out var posed);
        Assert.That(result, Is.Null);
        Assert.That(posed.Single().Options, Is.EqualTo(new[] { "A", "B" }));
        Assert.That(posed.Single().Mode, Is.EqualTo(AskUserMode.Multi));
        Assert.That(coercions.Guidance, Does.Contain("questions[0].options"));
    }
}
