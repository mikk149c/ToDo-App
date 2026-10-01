# ToDo-App

A ToDo app with a WinUI 3 frontend (`ToDo-App.UI`) and a class library (`ToDo-App`).

## Requirements

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
