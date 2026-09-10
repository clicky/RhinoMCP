namespace Rhino.AI.Tools;

internal static class AskUserValidation
{
    // Validate the whole batch before publishing any cards, so retrying cannot duplicate questions.
    internal static IToolResult? Validate(QuestionSpec[]? questions, Coercions coercions,
        out List<PendingQuestion> posed)
    {
        posed = [];
        const string retry = "No questions were shown. Correct the questions array and retry ask_user now; "
            + "do not stop or wait for answers. Each question needs non-blank text and at least one real option "
            + "besides Other or I don't know (the panel adds those).";
        if (questions is null || questions.Length == 0)
            return ToolResult.Failure(ToolError.BadArgument, "questions must be a non-empty array.", retry);

        List<PendingQuestion> validated = [];
        for (int i = 0; i < questions.Length; i++)
        {
            QuestionSpec? spec = questions[i];
            if (spec is null || string.IsNullOrWhiteSpace(spec.Question))
                return ToolResult.Failure(ToolError.BadArgument, $"questions[{i}].question must be non-blank.", retry);

            PendingQuestion pending = new(spec.Question, spec.Options ?? [],
                spec.MultiSelect ? AskUserMode.Multi : AskUserMode.Single);
            if (pending.Options.Count == 0)
                return ToolResult.Failure(ToolError.BadArgument,
                    $"questions[{i}].options must contain at least one non-blank, non-panel-synthesized option.", retry);

            if (pending.Options.Count != spec.Options!.Length)
                coercions.Note($"questions[{i}].options: removed blank or panel-synthesized choices; "
                    + $"{pending.Options.Count} real option(s) remain.");
            validated.Add(pending);
        }

        posed = validated;
        return null;
    }
}
