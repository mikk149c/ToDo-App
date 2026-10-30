# ToDo-App

A ToDo app with a WinUI 3 frontend (`ToDo-App.UI`), a web frontend (`ToDo-App.Web`) and a class library (`ToDo-App`) that both frontends use.

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

## Web app

`ToDo-App.Web` is the same board in a browser. It's a Blazor Web App (ASP.NET Core) that uses the `ToDo-App` library on the server. It only needs the .NET 10 SDK, so it also builds and runs on macOS and Linux:

```
dotnet run --project ToDo-App.Web
```

Then open http://localhost:5133.

- **One board per server.** The board is saved as `App_Data/board.json` in the project folder. To keep it somewhere else, set `Board:DataDirectory` in `appsettings.json` or the environment variable `Board__DataDirectory`. The file has the same format as the Windows app's `board.json`, so you can copy a board between the apps, or use Export and Import.
- **Every open tab shows the same board** and updates when another tab changes it.
- **Attachments** added in the browser are uploaded to `App_Data/attachments`. Attachments added in the Windows app point to a file on that PC, so the web app shows them but can't open them.
- **Theme, accent colour and language** are remembered in the browser, not on the server.
- **Translations** are the `.resx` files in `ToDo-App.UI/Properties`. The web app links them, so a string is added or translated once for both apps.
- It's meant for a single user on your own machine or network. There's no sign-in, so don't put it on the internet as it is.

### Run the server in Docker

With Docker Desktop running:

```
docker compose up --build
```

The web app is then on http://localhost:8080. The board is kept in the `todo-data` volume, so `docker compose down` and rebuilding keep it. In VS Code the same commands are under *Terminal → Run Task → server: docker compose up / down*.

## Desktop app on the server's board

The desktop app has a **Local / Remote** toggle in the toolbar. It always starts on **Local**: the `board.json` next to the `.exe`, as before. Switch it to **Remote** to work on the server's board instead, the same board the web app shows. Changes made in the browser, or in another desktop app, show up straight away. If the server can't be reached, the app says so and goes back to Local. The local board isn't touched by anything you do in Remote.

The server's address is http://localhost:8080 by default (the Docker setup above). Set the environment variable `TODO_SERVER_URL` to use another one.

**How it works.** `BoardController` (`ToDo-App/Controller`) is an abstract parent with one child per place the board can live. The toggle replaces the page's controller with the other kind:

- `BoardController` has the events, the reads, and a protected `Apply…` method for each change, which changes the board and raises the events. Each change itself is an abstract method that every child implements. It also makes the Ids for new columns, notes and tags before a change goes anywhere, so every copy of the board uses the same Ids.
- `LocalBoardController` applies each change straight away. It's the default, and works as the controller always has.
- `RemoteBoardController` (`ToDo-App.Client`) only sends the change to the server's API (`POST /commands/{change}`). The server applies it, numbers it, and sends it over SignalR to every connected app, including the one that sent it, which then applies it with the same `Apply…` method. So in Remote mode, the desktop only updates when the server says something changed, also for its own changes.
- The server uses a `ServerBoardController` (`ToDo-App.Web`): a `LocalBoardController` that also sends every change on, from the web pages and the API alike.
- A change that arrives over the network is only a name and its arguments as JSON. The two classes that receive them match the name in a `switch`: `ServerBoardController.Execute` (from the API, made like a change from a web page) and `RemoteBoardController.ApplyChange` (from SignalR, only applied, as the server has already made it).
- A new kind of change is an abstract method and an `Apply…` method in `BoardController`, a one-line override in each child, and a `case` in both switches. A test checks that no change is missing from either `switch`.

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
- **What's excluded:** `ToDo-App.UI`, which the tests don't reference and which needs WinUI test infrastructure, `ToDo-App.Web`, which is mostly Razor UI (its services are tested in `WebAppTests.cs`, but not counted), and code marked `[GeneratedCode]`. Any other exclusion needs a written reason in its PR.
- **Tooling:** coverlet's `XPlat Code Coverage` collector writes a Cobertura report. `Check-Coverage.ps1` summarises it and enforces the threshold. No external coverage service is used.

Tests for known bugs are marked `[Explicit]` and `[Category("KnownBug")]`, so they don't run by default. Run them with `--filter "Category=KnownBug"`. They're expected to fail until the bug is fixed.

## Documentation

The project's architecture is described in these UML diagrams:

- [Component diagram](docs/ComponentDiagram.md): the projects and how they depend on each other
- [Core class diagram](docs/CoreClassDiagram.md): the board, columns, notes, tags and controllers
- [Remote mode class diagram](docs/RemoteClassDiagram.md): the client, server and change broadcasting
- [Sequence diagram](docs/SequenceDiagramMoveNote.md): what happens when a change is made in Remote mode by moving a note

Additionally, our plan, workflow and design choices are described in this document:

-[Project Plan](docs/Projektplan,%20forløb%20og%20andre%20overvejelser%20til%20software%20arkitekturt.docx)