# Zaphira

Zaphira is a local-first AI assistant for desktop, designed around user control
over local models, runtimes, and future provider strategies.

The initial goal is simple:

```text
Launch Zaphira -> choose an initial model -> chat.
```

## Documents

- [Architecture MVP Spec](Zaphira_Architecture_MVP.md)
- [Implementation Plan](Zaphira_Implementation_Plan.md)

## Development

Required baseline:

- .NET SDK 10.0.300 or compatible feature-band roll-forward
- macOS for first-class development and validation

Common commands:

```bash
dotnet restore
dotnet build Zaphira.sln -maxcpucount:1
dotnet test Zaphira.sln -maxcpucount:1
```

The single-node MSBuild option keeps early solution builds deterministic in the
current development environment.
