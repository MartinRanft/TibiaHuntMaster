# ADR 0001: Keep character-name matching targeted instead of adding global SQLite `NOCASE`

## Status

Accepted

## Context

A review suggested adding a global SQLite `NOCASE` collation across string fields to avoid platform-specific case-matching issues.

The concrete bug was narrower:
- `HistoryViewModel` had a self-canceling reload flow
- character lookup behavior differed between direct SQL equality and in-memory comparisons
- the failing tests were about targeted character-name resolution, not a proven system-wide collation defect

Global collation changes would affect more than character names:
- duplicate detection
- ordering behavior
- filtering semantics across unrelated entities
- future migrations on installed user databases

## Decision

Do not introduce a model-wide or database-wide `NOCASE` collation.

Keep matching targeted:
- try exact provider query first
- fall back to explicit in-memory `StringComparison.OrdinalIgnoreCase` only where character-name resolution must be robust across platforms
- keep Unicode-sensitive behavior explicit and covered by tests

## Consequences

Positive:
- avoids a broad schema change without proof of system-wide need
- keeps the fix local to the actual lookup paths
- works consistently across Windows, Linux, and macOS even if SQLite/provider behavior differs

Negative:
- some targeted lookups require a fallback query path that loads candidate rows into memory
- if future real-world evidence shows broader collation problems, that should be addressed field-by-field with migration evidence and tests

## Evidence

Relevant code paths:
- `TibiaHuntMaster.App/ViewModels/Dashboard/HistoryViewModel.cs`
- `TibiaHuntMaster.Infrastructure/Services/Hunts/HuntSessionService.cs`
- `TibiaHuntMaster.Infrastructure/Services/Hunts/TeamHuntService.cs`

Relevant tests:
- `TibiaHuntMaster.Tests/ViewModels/HistoryViewModelTests.cs`
- `TibiaHuntMaster.Tests/Hunts/HuntAnalyticsTests.cs`
