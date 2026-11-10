# R2 — Demonstrated obsolete expectations

## Scope, sources and evidence

Checkout evaluated: R1 commit `35000f9e8d5d9953fc7093acd78b63c53cd9c60a`, production unchanged from `795f544`. Original reference: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. C02's original-source window execution and C03's original gauge execution are reused; they are not rerun from scratch or promoted to full-engine certification.

An isolated `git archive` of historical `59e186c527bb8e2ab3e8ea82c6668e21b4bf147a` was compiled Release against the same pinned osu! source, with separate artifacts and identical SDK/cache configuration. The authorized checkout was not reset or switched. Focused baseline filter: `BmsHitWindowsTest`, `BmsGaugeProfileTest`, `BmsCourseHealthInheritanceTest` and `TestCourseGaugeProfileFamilySelection`: **86 total, 37 passed, 49 failed**, no omissions. The failures comprise eight windows, five gauge-profile, 32 course inheritance and four course-family cases. This dates only the examined failures, not all C01 cases.

A separate five-case baseline instrumentation/snapshot run produced four identical fingerprint mismatches and one host startup access-denied error. That failed attempt remains preserved. The isolated INST-01 retry outside that restriction **passed (1/1)**. Hence the access error was environmental for that attempt; it is not the explanation of C01's actual pending-release assertion. Full original and successor TRXs/logs are retained separately.

No historical H1 fixture, expectation, trace, fingerprint or production implementation was changed in R2. There are **109 H1 observations explained by six policy causes** below. IDs identify rows of the unchanged R1 CSV; classification addresses the demonstrated failing assertion, not all later assertions a failing test never reached.

## H-WINDOW — LR2 windows and their consumers: 65 cases

Old expectations include key PGREAT 5/10/15/20/25 ms, GREAT 45 ms at normal rank, asymmetric BAD limits 165/210 ms, 500 ms Empty Poor extent and distinct long-tail PGREAT/GREAT/GOOD radii. Original `JudgeProperty.java:60–73, JudgeWindowRule.LR2` instead provides LR2 key/scratch head bases 21/60/120/200 ms, one-sided Empty Poor 0–1000 ms in dtime, a separate long-tail table and integer rank interpolation. At normal rank, key head windows are 18/40/100/200 ms; tail PG/GR/GD all have 100 ms radius. C02's exact original Java rows verify these values and the other ranks/EXRANK vectors.

Current `BmsJudgementProfileProvider.lr2Profile`, `BmsHitWindows`, tail rules and their consumers follow those tested tables. For example, a tail released 100 ms early is now Perfect rather than the old Great; an offset 220/230 ms has no BAD window; a head timeout uses 200 rather than old PMS 183 ms.

Affected assertions: `BmsHitWindowsTest` (8), `BmsHitErrorMeterTest` (16), star leniency (3), ruleset displayed windows (2), NoGood/NoGreat bounds (3), two long-tail boundary tests, two presentation tail-result cases, five long-note-rule boundary cases, three visual tail-window cases, four old column-trace deadlines, two HCN keyboard tail results, one historical Empty Poor selection and eight long-note-lifecycle cases. The last six lifecycle traces compare the current gameplay and reconstructed replay successfully **before** comparing against the immutable P2 EX-score golden (`BmsLongNoteLifecycle.capture:329–338`). Their failing old progression is not evidence that replay and gameplay differ. The other two use the old 165 ms body-visibility boundary.

Six song-progress colour tests also belong here: they obtain current windows but convert offsets with hard-coded `(offset+500)/1000` and expect a 500-unit meter width (`TestSceneBmsSongProgress:434–448`). Actual `BmsHitErrorMeter.CreateDomain` correctly derives ±1000 from the current Empty Poor table. Dynamic result selection does not make the obsolete coordinate conversion valid. Three other poor-line tests are left for R3; they have a different assertion and are not automatically covered by this conclusion.

IDs: F010–F011, F060–F078, F100, F102–F109, F111–F114, F119–F123, F127–F129, F148–F149, F174–F177, F185–F194, F195, F204–F209. Exact noncontiguous membership is retained in the final classification table.

## H-MARGIN — PMS release grace replaced by LR2 zero margin: 11 cases

Original `JudgeProperty.LR2` has both release margins zero; `JudgeManager:158,198–201` selects that property through the fixed LR2 player rule. The historical PMS property has 200000 microseconds, but it is not the active LR2 property. Current `BmsHoldRuntime.ReleaseMargin:34` is zero.

Four presentation cases, three rule cases and three operation-order cases assume an early BAD/outside-window release remains pending for 200 ms and can be rescued by repressing. INST-01 assumes `PendingReleaseTime=3300` after traversing to 3400. These are incompatible with zero-margin settlement. **INST-01 fails before `note.ApplyDefaults` or any reapply**, so it cannot demonstrate lost accumulator or pending state during reapply. The historical baseline passes INST-01 and still returns PMS margin 200; its changed result has a direct policy explanation. This does not certify the unreached remainder of the instrumentation test.

IDs: F003, F115–F118, F124–F126, F130–F132.

## H-GAUGE — Default LR2 family and BAD damage: 20 cases

`BMSPlayerRule.java:19,43–54,112` selects LR2 by default across layouts. `BmsGaugeProfileFamilyProvider.FromLayout` does likewise; these three default-layout tests were already incompatible at the historical baseline. This is distinct from an **explicit course gauge constraint**, discussed below.

LR2 Normal BAD is -4%, and an HCN released body tick applies half BAD (-2%). Old tests expect SevenKeys -3% and half BAD -1.5%, or old default 5K/PMS clear borders. C03 executes original `GaugeProperty.LR2`/`GrooveGauge` sequences, and the relevant `BmsHealthProcessor.ApplyHellChargeTick` simply scales the selected profile's result. Therefore changing recovery, rewind or HCN scheduling to recover these old numbers would be wrong.

Affected: three default family, four regular-health/default BAD assertions, one half-BAD test, two signed/body-history assertions, four HCN frame-order health assertions and six visual ordinary/PMS/scratch health assertions. The failure is at the old damage/profile number; this does not certify every later frame-order assertion.

IDs: F082–F084, F093–F098, F110, F144–F147, F150–F151, F170–F173.

## H-DEATH — Positive sub-2% LR2 survival values are dead: 11 cases

Ten course inheritance cases construct nominally surviving LR2 seeds of 0.005, 0.001 or 1e-9 and require exact preservation as living gauges. LR2 survival profiles use death 2%; original `GrooveGauge.Gauge.setValue` clamps and then kills values below that threshold, and cannot recover a dead gauge. F02 and C03 independently verify this rule, including a positive Hazard result below 2%. Correctly normalizing an impossible surviving seed is not unintended refill or health loss.

One health-processor case requires a tiny positive floating-point remainder after five ExHardClass misses rather than shifting to a surviving tier. Its obsolete pre-2%-death expectation fails for the same reason. Other course constraints/state issues are not included.

IDs: F020–F029, F091.

## H-GUTS — LR2 strict 32% threshold: one case

`BmsGaugeProfileTest.TestCourseGaugeFamiliesUseBeatorajaClassValues` requires LR2 ExClass guts at 30%. Original `GaugeProperty.LR2` uses `<32%`, matching current factory and C03's independent 31.99/32% vectors. Preserve the other explicit-family assertions; only its LR2 threshold expectation is obsolete. ID F079.

## H-MISS — LR2 always penalizes the later miss: one case

`StackedPmsHeadsShareTheEmptyPoorMistakeMarker` expects the old PMS one-miss marker. The active LR2 property is `MissCondition.ALWAYS`, unlike historical PMS `ONE`. `BmsColumnJudgementRuntime.HasPmsMistake` intentionally returns false for the selected LR2 policy; C02's existing poor-penalty regressions cover the relevant C# behavior. Do not restore old double-penalty suppression just to satisfy that marker assertion. ID F101.

## What is deliberately not declared obsolete

- **36 explicit course-family observations** (32 inheritance, four family-selection): original `CourseData.java:174–190` recognizes `gauge_5k/7k/9k/24k/lr2`, and `GrooveGauge.java:128–144` selects their corresponding properties. Current `BmsCourseSession.ResolveCourseGaugeProfileFamily:237–254` maps every explicit value to LR2. The baseline reproduces the mismatch. R3 must evaluate it as a current functional contract issue; the default LR2 rule does not justify ignoring an explicit override.
- Palette differences, missing assets, input markers and result statistics are not called H1 merely because they are visual.
- INST-02/03's reference and optimized results agree, but both disagree with their goldens, identically in the historical baseline. This dates the mismatch without proving why those goldens differ or that the entire lifetime formula is correct.
- Unexplained selection, replay, attribution and localization cases remain outside this H1 set.

## Recommendation

Keep all H1 tests and golden files unchanged as historical evidence. A later dedicated maintenance phase can replace their old policy constants/coordinates with LR2-aware expectations while retaining valid invariants. For the current phase, document and exclude these observations from the list of demonstrated production defects; do not hide them from CI. R3 investigates the remaining cases, especially explicit course constraints and preview-loading exceptions.
