# R5 — Final selective reconciliation

## Result and scope

Evaluated corrected-source HEAD: `97effb69fb57a21fdc2dd2f8318ca35b4d5009d3`, authorized branch `fix/lr2-endlessdream-parity-incremental`, origin `hinobara-ctrl/BmsRuleset`. Initial reconciliation HEAD was `795f54403a5db3b1db8ffa6aa985ace38182df92`; original C01 tested `bae80bc95a2abde15cc1519d0cf784e43cbd9ced`. Historical comparison used isolated `59e186c527bb8e2ab3e8ea82c6668e21b4bf147a`. Original Java/parser versions remain the fixed commits recorded in R1–R3.

**Three demonstrated preexisting production causes were corrected, plus one valid fixture's preparation. No new failed case identity appeared in the final broad run. The suite remains red; merge is not approved by this report.** All 420 protected certification cases and seven new regressions pass.

## Unique classification of the original 219 observations

`R5_FAILURE_CLASSIFICATION.csv` contains every R1 ID exactly once with its primary category, cause, disposition and actual final outcome. Secondary issues are described without adding the same case to another category.

| Category | Cases | Independent causes / limits | Actual status |
|---|---:|---|---|
| H1 — demonstrated obsolete historical expectations | **148** | **9 policy causes**, R2/R3 | All 148 old assertions remain unchanged and fail |
| H2 — preexisting, functionally relevant | **46** | **3 production causes** | Causes corrected; 44 cases pass; two graph/UI cases retain secondary obsolete six-tier assertions |
| R1 — confirmed current regression with attribution | **0** | None demonstrated | No new attribution claimed |
| V1 — functional defect, origin unknown | **0** | None established to this standard | U1 is not silently converted into V1 or H1 |
| E1 — fixture/environment configuration | **7** | **2 setup causes** | Five boundary cases pass after setup-only correction; two unseeded-context cases remain |
| P1 — presentation/localization | **8** | 3 presentation families; not all are proven design defects | Deferred; all eight remain failing |
| U1 — undetermined | **10** | No established production cause; 5 investigation families | Retained, not declared harmless |
| **Total** | **219** | Categories do not overlap | **49 pass, 170 fail** |

The nine H1 causes are LR2 window/consumer bounds, zero release margin, default LR2 family/BAD damage, 2% death, 32% guts, ALWAYS/Empty Poor, short-chart damage scaling, collective BAD consumption, and late long-head rejection. R3 adds the demonstrated families to R2's initial 109 observations. Some tests fail before their later invariant checks; those unreached checks are not thereby certified.

The two mixed graph cases illustrate why a falling test count is not the objective: the real graph omitted Easy, but the old test also demanded Assist for every configuration. The actual chain now has five correct default-Easy tiers; the original six-tier assertion remains red. These cases retain primary H2 provenance and an explicit secondary historical issue rather than being counted twice.

## What changed and why

1. **Course overrides:** four mappings in `BmsCourseSession.ResolveCourseGaugeProfileFamily` now honor explicit 5K/7K/PMS/Keyboard constraints, as original Java `GrooveGauge.create` does. Default and explicitly requested LR2 are preserved. Existing three-stage serialized health/inheritance checks verify the correction.
2. **Preview fallback:** `BmsPreviewAudioResolver` normalizes empty source consistently with its resource store and avoids enumerating a nonexistent directory. Optional preview discovery no longer aborts the available event/keysound fallback. Existing ordering and volume routing remain intact.
3. **Legacy GAS graph tiers:** `BmsGaugeHistoryGraph` includes Easy/Assist only when the configured lower limit requests them. Its class branch and recorded-history route are preserved. This does not certify every legacy reconstructed health value.
4. **Boundary fixture:** `TestAutoGaugeClearThresholdBoundary` now installs the Easy/Assist tiers whose snapshots it restores and whose clear behavior it asserts. Every assertion and adjacent-double boundary is unchanged. No H1 test method was modified.

R3 introduced seven controlled regression cases in two small NUnit files: four preview conditions and three configured graph limits. Their pre-fix run had five failures and two valid controls; the same seven now pass. No test, category or CI filter was removed, disabled or weakened. No benchmark, reference-source edit, dependency change, excluded modifier change or unrelated refactor was made. The exact reviewed delta is three production files, those two new test files and one setup call in an existing fixture, plus the requested reports/tables.

## Executions and comparison with C01

Local environment is Windows 11 build 26200 x64, .NET SDK 10.0.401/runtime 10.0.12, Release, matching pinned osu! source. All test commands use filters. R5 performs one broad ordinary run and one separate instrumentation run with the existing runsettings; C02–C06 Java generation was not repeated wholesale.

| Run | Total | Passed | Failed | Omitted |
|---|---:|---:|---:|---:|
| Original C01 ordinary | 3914 | 3695 | 216 | 3 |
| Final ordinary | **3928** | **3758** | **167** | **3** |
| Original C01 instrumentation | 288 | 285 | 3 | 0 |
| Final instrumentation | **288** | **285** | **3** | **0** |
| Final certification + new regression focus | **427** | **427** | **0** | **0** |

The ordinary total increases by 14: seven certification cases added in C02–C05 and seven new reconciliation cases. All 14 pass. Of the original 216 ordinary failures, **49 now pass** (36 course, eight preview/startup, five fixture setup). The other 167 persist; all three original instrumental failures also persist. A comparison by original suite/class/case found **zero new failed identities**. This is one observed regression comparison, not a universal absence-of-bugs claim.

The protected subset was checked against the original C06 TRX as a multiset of class/case/outcome: all **420 original passing executions are present and passing**, plus seven additional passing executions. Original protected TRX SHA-256: `725F63DB7288634265E1DC15D5D039547AE0CB6F8D5DC0843E097871C4D6E61A`; final 427-case TRX: `418639D252147D1E067FCEB815139C039B3FCDA65C470CF99C7F7126D6184812`.

R4 acceptance runs are documented individually: course **88/88**, preview **72/72**, graph **26/28** with its two preserved historical assertions, boundary **23/23**. The final broad ordinary run took approximately 6m15s; instrumentation 2m10s. Release restore/build succeeded. Preexisting NU1902/NU1903 and the existing HUD nullable warning remain; no package or warning suppression was changed.

Counts come from individual TRX result entries, not deduplicated display names. Some successful legacy-skin resource cases share a display name and still count as separate executions. The original and final ordinary TRX summaries inconsistently say `notExecuted=0`, while three result entries are `NotExecuted` and `total-executed=3`. Those entries are the local replay corpus check and the two inapplicable nonembedded-FFmpeg checks listed in R1. **Three is the correct observed omitted count.** The older C06 JSON copying zero is retained with this erratum; original evidence was not overwritten.

`R5_EVIDENCE.json` records the preserved original/baseline/probe/successor TRX hashes, raw counters alongside actual outcomes and final binaries. Raw logs, candidate DLLs and failed attempts remain separately archived outside Git. The isolated baseline included a denied host-startup attempt; its accepted INST-01 retry is separately preserved, not substituted into the failed run.

## Instrumentation and remaining Course concerns

INST-01 is H1: its pending-release assertion occurs before defaults/reapply, and requires the replaced PMS 200 ms margin. It passes in the old baseline but the current LR2 margin is zero. There is no demonstrated accumulator-loss defect from this failure. INST-02/03 remain U1: original/current binaries produce the same respective hashes and optimized/stateless map searches agree, but the old golden arrays are absent. No hash was replaced or mismatch hidden.

The 36 explicit Course profile failures pass. Ten remaining inheritance cases require living sub-2% LR2 seeds and are H1, not evidence of accidental health refill/loss. Two unseeded course/GAS setup cases remain E1. Three aggregate course-result UI cases remain U1: their timeout does not isolate which metric/text condition failed. They are not declared historical merely because backend inheritance now passes.

The ten U1 observations are F001/F002/F008/F009 (two chart goldens in ordinary/instrumental variants), F059 (mixed difficulty golden), F099 (Duration with per-note rates), F178–F180 (course aggregate UI) and F196 (visual GAS downgrade). They retain explicit uncertainty and require a valid targeted expected result before correction. In particular, H1 timing/score changes do not justify changing their expectations speculatively.

## CI and publication

The existing workflow was manually dispatched on corrected-source `97effb69fb57a21fdc2dd2f8318ca35b4d5009d3`: [run 42142156136](https://github.com/hinobara-ctrl/BmsRuleset/actions/runs/42142156136). At review (2026-11-06 01:25:47 UTC), Ubuntu completed **failure**, with both regression and instrumentation steps failing; Windows is still running after its regression step failed. Both Release builds succeeded. **Final-source CI is not green.** The previous completed initial-head run `42131431022` also failed on both operating systems. The final publication receipt records any later status available at delivery; the workflow and its filters are unchanged.

Closing update (2026-11-06 01:28:21 UTC): this final-source run completed **failure on both Windows and Ubuntu**. `R5_EVIDENCE.json` preserves the earlier timestamped review snapshot; the delivery receipt includes the terminal status. No green result is inferred from successful builds or focused tests.

| Stage | Published commit | Evidence |
|---|---|---|
| R1 | [`35000f9e8d5d9953fc7093acd78b63c53cd9c60a`](https://github.com/hinobara-ctrl/BmsRuleset/commit/35000f9e8d5d9953fc7093acd78b63c53cd9c60a) | 219 identities, original TRX integrity, omission erratum |
| R2 | [`eefd14fff41a58c1623f61d41e8b6580d4c21eac`](https://github.com/hinobara-ctrl/BmsRuleset/commit/eefd14fff41a58c1623f61d41e8b6580d4c21eac) | Historical executions and first six H1 policies |
| R3 | [`e8ca443dfa3d6876d6a64fd855bb19565a8d276b`](https://github.com/hinobara-ctrl/BmsRuleset/commit/e8ca443dfa3d6876d6a64fd855bb19565a8d276b) | Current causes; seven probes, five initially failing |
| R4-C | [`f6f7af30cc3e87f6aaf339ea7cd3993f2ceb16de`](https://github.com/hinobara-ctrl/BmsRuleset/commit/f6f7af30cc3e87f6aaf339ea7cd3993f2ceb16de) | Explicit Course profile overrides |
| R4-P | [`b98c9db857d24f52d25786a25900b4f6e1f7809b`](https://github.com/hinobara-ctrl/BmsRuleset/commit/b98c9db857d24f52d25786a25900b4f6e1f7809b) | Preview fallback |
| R4-G | [`aacdda05114ecf4ff46c67d4f21e04b912283ab3`](https://github.com/hinobara-ctrl/BmsRuleset/commit/aacdda05114ecf4ff46c67d4f21e04b912283ab3) | Configured graph tiers |
| R4-T | [`97effb69fb57a21fdc2dd2f8318ca35b4d5009d3`](https://github.com/hinobara-ctrl/BmsRuleset/commit/97effb69fb57a21fdc2dd2f8318ca35b4d5009d3) | Boundary fixture setup, same assertions |
| R5 | [`a7779e6a863b23452bae66a65a2254c9d90838e5`](https://github.com/hinobara-ctrl/BmsRuleset/commit/a7779e6a863b23452bae66a65a2254c9d90838e5) | Final broad runs, exact 420+7 subset and unique 219-case classification |

R5's published report SHA is now recorded in this table/index. Final local/remote HEAD and clean tree/index are verified and recorded in the external delivery receipt; its metadata-only closing commit cannot contain its own SHA. Every stage was pushed separately to the fork. No dev change, merge, PR, squash, rebase or force push. Reference checkouts, earlier reports/inventories/patches and excluded MOD-01/02/03 production files remain intact; the parent certification index only gains a link to this subsequent phase.

## Explicit decisions for independent review

1. **Historical expectations:** 148 primary cases, explained by nine policies. Two additional mixed graph cases retain secondary old assertions after their functional cause was corrected; they are not counted twice.
2. **Current functional defects identified:** three production causes. All three are demonstrated preexisting at the historical baseline and now corrected locally and in the fork.
3. **Introduced by incremental parity:** zero demonstrated functional causes in the range from the historical baseline to certification HEAD. This does not imply that all unexamined behavior is correct or date an unknown failure.
4. **Still undetermined:** ten observations in five investigation families, listed above.
5. **Course:** explicit override/state propagation cases now pass; invalid LR2 seeds and unseeded fixture setups remain separately documented; aggregate UI metric failures need more evidence.
6. **Instrumentation:** one proven obsolete assertion, two unresolved goldens, no suppression or blind update.
7. **Deferrable visuals:** the six palette, one settings visibility and one translation-key cases can enter a separate presentation/fixture maintenance task. Incorrectly missing configured graph tiers was treated as functional and corrected.
8. **Merge blockers:** CI remains non-green; ten U1 observations, especially candidate selection and aggregate Course UI, must be resolved or explicitly accepted by the project. The other historical red assertions require a planned maintenance/merge-gate decision; this report does not authorize ignoring the whole failed suite.
9. **Certification:** the exact original 420-case subset still passes. Seven new regressions pass too. No integral Java/C# or all-policy GAS certification is added.
10. **Next maintenance phase:** advisable for H1 constants/goldens/coordinates and the two stale course setups, preserving useful invariants and independent LR2 expectations. It has not been started automatically.

**Reconciliation completed with documented limits; ready for independent review, not automatic merge.**
