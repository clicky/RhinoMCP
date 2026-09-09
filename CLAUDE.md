# RhinoAI

## AI-authored C# files use a `.ai.cs` suffix

Any C# file created by an AI coding agent is named `<Name>.ai.cs` rather than `<Name>.cs`.

The point is provenance. A glance at the tree, or a single `git ls-files '*.ai.cs'`, tells you which code was machine-drafted and therefore wants closer review before it is trusted. Nothing about the suffix changes how the code compiles or behaves.

### Rules

- New C# files written by an agent: `Widget.ai.cs`, not `Widget.cs`.
- Existing hand-written files keep their names. Editing `Widget.cs` does not rename it, because renaming an in-place edit would fork the type into a duplicate rather than change it.
- A file does not lose the suffix once a human edits it. The suffix records who drafted the file, not who touched it last. Rename it deliberately if the file has been rewritten enough that the original draft is gone.
- The C# class name does not have to match the filename, so `Widget.ai.cs` still declares `class Widget`.

### Excluded

Files whose name is load-bearing keep their conventional spelling, since tooling matches on it:

- `*.xaml.cs` (code-behind pairs with its `.xaml`)
- `*.Designer.cs` (pairs with its `.resx`)
- `*.g.cs`, `*.g.i.cs`, `*.generated.cs` (generated output)

### Build impact

Every project here is SDK-style and uses the default compile globs, so `*.ai.cs` is picked up automatically by the project that owns it. No csproj change is needed to add one.

The exception is the test projects that link plugin sources in by path. `tests/Server.Tests/Server.Tests.csproj` and `tests/StreamJson.Tests/StreamJson.Tests.csproj` carry explicit `<Compile Include="../../rhino/plugin/..." />` lists, so a linked source file that is named `.ai.cs` must be spelled that way in the csproj too.
