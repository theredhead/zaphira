# Contributing to Zaphira

This repository follows the engineering rules in `Zaphira_Architecture_MVP.md`.

## Test Conventions

- Prefer tests for observable behavior, contracts, and invariants.
- Name test classes after the type or behavior under test.
- Name test methods with descriptive behavior names.
- Add regression tests for bug fixes.
- Avoid tests that only mirror private implementation details.

## Code Review Checklist

- Does the change follow normal C#, .NET, ASP.NET Core, Avalonia, MVVM, and
  CommunityToolkit conventions?
- Are domain, application, and contract models initialized into valid non-null
  states?
- Is absence represented with a real value, empty collection, result/option type,
  or other explicit non-null model?
- Are nullable warnings treated as design feedback?
- Is visibility as narrow as practical?
- Are method, type, variable, command, and test names descriptive?
- Are async operations using async/await and propagating cancellation where
  appropriate?
- Are expected failures represented deliberately at application boundaries?
- Are user-facing failures clear and are diagnostics useful without logging
  sensitive content by default?

