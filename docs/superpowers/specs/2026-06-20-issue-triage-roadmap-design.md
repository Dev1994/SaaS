# Issue Triage & Roadmap — Design

**Date:** 2026-06-20
**Status:** Approved (design)
**Scope:** Sequencing plan for the 18 open GitHub issues. This is a roadmap spec, not a per-issue implementation design — each issue gets its own branch, PR, and (where non-trivial) its own spec/plan cycle.

## Goal

Drive the open issue backlog to closure in a deliberate order: security risk first by severity, with a test foundation established before behaviour-changing security work so regressions are caught automatically.

## Constraints & Decisions

- **Priority driver:** Security first, ordered by severity (high → medium → low), then quality/bugs.
- **Delivery granularity:** One PR per issue. Branch-per-issue, matching the existing `fix/8-forwarded-headers-trusted-proxies` convention.
- **Test placement:** The test suite (#21) is pulled forward to run *before* the medium/low security changes, providing a regression net for CORS, header, and trace-sanitisation changes.
- **#8** (spoofable forwarded headers, high) is already in progress on the current branch and is treated as in-flight, not re-planned here.

## Milestones

Each milestone is a batch of independent PRs. Milestones are sequential; PRs within a milestone may proceed in parallel unless a file-conflict note says otherwise.

### M0 — In-flight
| Issue | Sev | Description |
|-------|-----|-------------|
| #8 | high | Spoofable forwarded headers → rate-limit bypass / trace poisoning. Finish and merge current branch. |

### M1 — Dependency CVEs
Pure package version bumps. Lowest logic risk, fastest to ship, removes known-vulnerable dependencies before any code work.

| Issue | Sev | Description |
|-------|-----|-------------|
| #24 | high | Upgrade MessagePack ≥ 2.5.192 (GHSA-hv8m-jj95-wg3x). |
| #25 | medium | Upgrade OpenTelemetry packages (moderate CVEs). |

### M2 — Test foundation
| Issue | Sev | Description |
|-------|-----|-------------|
| #21 | enhancement | Automated tests: unit coverage for `PhraseService`, integration coverage for endpoints (status codes, payload shape, rate limiting, category/term lookup, not-found paths). Establishes the regression net for M3/M4. |

### M3 — Medium security
| Issue | Sev | Description | Notes |
|-------|-----|-------------|-------|
| #9  | medium | Gate OpenAPI document + Scalar UI behind non-production environment check. | Independent. |
| #10 | medium | Replace any-origin CORS with a named policy + explicit allowlist. | Independent. Touches `CorsExtensions.cs`. |
| #11 | medium | Fix HSTS preload: `max-age` too short for preload eligibility. | Touches `SecurityExtensions.cs`. |
| #12 | medium | Add missing security response headers (`X-Content-Type-Options: nosniff`, `frame-ancestors`/`X-Frame-Options`). | Touches `SecurityExtensions.cs`. |

**File-conflict note:** #11 and #12 both modify `SecurityExtensions.cs`. Sequence them adjacent (one after the other), not in parallel, to avoid merge conflicts.

### M4 — Low security
| Issue | Sev | Description |
|-------|-----|-------------|
| #13 | low | Bound/sanitise user input written to trace tags (log/trace injection, cardinality blow-up). |
| #14 | low | Stop recording PII in traces (client IP + full User-Agent) — drop or hash. |
| #15 | low | Replace `AllowedHosts` wildcard with real expected hosts. |

### M5 — Quality & bugs
| Issue | Sev | Description |
|-------|-----|-------------|
| #22 | bug | Fix mojibake / broken unicode in source strings (`phrases.json` encoding). |
| #16 | enhancement | Client disconnects logged as HTTP 500 — map to client-closed (e.g. 499) instead of error. |
| #17 | enhancement | Replace `lock(Random)` with `Random.Shared`. |
| #18 | enhancement | Remove obsolete docker-compose `version` key. |
| #19 | enhancement | Use `ILogger` instead of `Console.WriteLine` for OTel config output. |
| #20 | enhancement | Unused `GetForDutch()` — wire up the missing endpoint or delete the dead code. |

## Per-PR Workflow

For each issue:
1. Branch from `master`: `fix/<n>-<slug>` (or `feat/` for enhancements).
2. Non-trivial issues (#21, #9, #10, #13, #14, #16): run brainstorming → writing-plans before coding. Trivial one-liners (#17, #18, #19, #24, #25): straight to TDD/fix.
3. Verify (tests green for M2+; manual + security check for security PRs).
4. PR referencing the issue. One issue per PR.

## Risk Notes

- **M3/M4 without M2 would be unsafe** — that is the reason #21 is pulled forward. Do not start M3 until the test suite covers the endpoints those changes touch.
- **#9 (prod doc exposure)** changes behaviour by environment; verify the live deployment (`saas.volkwyn.nl`) sets `ASPNETCORE_ENVIRONMENT=Production` so the gate actually engages.
- **#10 (CORS allowlist)** can break existing consumers; confirm the legitimate origins before narrowing.

## Out of Scope

- Net-new features (search, favourites, submit-a-phrase, auth/API keys) — not in the issue backlog; separate future cycle.
- Any refactor not tied to a listed issue.
