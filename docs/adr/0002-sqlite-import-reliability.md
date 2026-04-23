# ADR 0002: Use explicit import transactions with targeted SQLite lock retries

## Status

Accepted

## Context

A review raised two related questions:
- should hunt imports use an explicit transaction for clarity?
- should the app add broad optimistic concurrency (`RowVersion`) handling for SQLite lock contention?

This application is a local desktop app using SQLite. The main real contention risk is short-lived file locking around writes, not multi-user concurrent editing across the entire model.

## Decision

For solo and team hunt imports:
- wrap the duplicate-check + insert + save flow in an explicit transaction for clarity
- add targeted retry handling for SQLite `BUSY` / `LOCKED` write failures
- keep the retry scope narrow and limited to critical write paths

Do not introduce global `RowVersion` or model-wide optimistic concurrency as a blanket response.

## Consequences

Positive:
- import intent is explicit in code
- transient SQLite write locks are retried in the paths where they actually matter
- behavior is backed by real file-database tests instead of theory

Negative:
- a small amount of infrastructure code is added around import paths
- retries must remain bounded to avoid masking persistent failures

## Evidence

Implementation:
- `TibiaHuntMaster.Infrastructure/Services/Hunts/SqliteWriteRetry.cs`
- `TibiaHuntMaster.Infrastructure/Services/Hunts/HuntSessionService.cs`
- `TibiaHuntMaster.Infrastructure/Services/Hunts/TeamHuntService.cs`

Regression coverage:
- `TibiaHuntMaster.Tests/Hunts/HuntImportConcurrencyTests.cs`
