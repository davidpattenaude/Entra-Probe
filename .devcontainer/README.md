# Devcontainer

This repository includes a devcontainer for local development and CI-parity-oriented editing.

## What It Provides

- .NET 8 SDK in a reproducible container
- VS Code extensions for C# and GitHub Actions
- Automatic `dotnet restore` after container creation

## How to Use It

1. Open the repository in Visual Studio Code.
2. Choose `Dev Containers: Reopen in Container`.
3. Wait for the container to finish setup.
4. Build or test from the integrated terminal:

```bash
dotnet build EntraProbe.sln -c Release
dotnet test EntraProbe.sln -c Release
```

## Notes

- The devcontainer is intended for development, build, and test workflows.
- The production app supports Windows and macOS. The devcontainer is for editing, restore, build, and test workflows, not Linux runtime support.
