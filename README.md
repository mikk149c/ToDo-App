# ToDo-App

A ToDo app with a WinUI 3 frontend (`ToDo-App.UI`) and a class library (`ToDo-App`).

## Download

Download the newest build here: [ToDo-App-win-x64.zip](https://github.com/mikk149c/ToDo-App/releases/latest/download/ToDo-App-win-x64.zip)

Unzip it and run `ToDo-App.UI.exe`. Nothing else needs to be installed. The build is replaced every time new code lands on `main`.

Your board is saved as `board.json` next to the `.exe`, so copy that file over when you move to a newer build.

## Requirements

These are only needed to build the app from source.

- Windows 10 (1809) or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- VS Code with the **C# Dev Kit** extension (VS Code suggests it when you open the folder)

The Windows App SDK is bundled with the app, so no extra runtime install is needed.

## Build and run

In VS Code:

- **Debug:** press `F5`
- **Build:** `Ctrl+Shift+B`
- **Run without debugger:** *Terminal → Run Task → run*

From a terminal:

```
dotnet build ToDo-App.slnx -p:Platform=x64
dotnet run --project ToDo-App.UI -p:Platform=x64
```

Always pass `-p:Platform=x64`, because WinUI 3 does not build as "Any CPU". On an ARM PC, use `ARM64` instead.

## Tests and coverage

Run the tests:

```
dotnet test ToDo-App.Tests/ToDo-App.Tests.csproj
```

Run them with coverage, the same way CI does, and print the report:

```
dotnet test ToDo-App.Tests/ToDo-App.Tests.csproj -c Release --collect "XPlat Code Coverage" --results-directory coverage
powershell -File .github/scripts/Check-Coverage.ps1 -ReportPath coverage
```

Delete the `coverage` folder between runs, because the script expects exactly one report.

Every pull request to `main` runs the **Tests and coverage** check (`.github/workflows/pr-checks.yml`). It fails when a test fails or when line coverage of the `ToDo-App` class library is below the threshold set by `COVERAGE_THRESHOLD` in that workflow. The coverage table is shown in the run's summary on the PR.

- **What's measured:** line coverage of `ToDo-App` only (`coverage.runsettings` in the test project). Branch coverage is reported but not enforced.
- **What's excluded:** `ToDo-App.UI`, which the tests don't reference and which needs WinUI test infrastructure, and code marked `[GeneratedCode]`. Any other exclusion needs a written reason in its PR.
- **Tooling:** coverlet's `XPlat Code Coverage` collector writes a Cobertura report. `Check-Coverage.ps1` summarises it and enforces the threshold. No external coverage service is used.

Tests for known bugs are marked `[Explicit]` and `[Category("KnownBug")]`, so they don't run by default. Run them with `--filter "Category=KnownBug"`. They're expected to fail until the bug is fixed.
