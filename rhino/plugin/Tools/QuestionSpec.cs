using System.ComponentModel;

namespace Rhino.AI.Tools;

// One question as the agent asks for it. The ask_user input schema is generated from this record's
// constructor, so these descriptions are what the agent actually reads.
internal sealed record QuestionSpec(
    [Description("The question to show the user")] string Question,
    [Description("At least one non-blank choice is required. Do not include Other or I don't know; the panel adds those automatically.")] string[] Options,
    [Description("true = the user may pick several of these options (checkboxes); false = one choice (radio). Default false.")] bool MultiSelect = false);
