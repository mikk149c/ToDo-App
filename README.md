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
