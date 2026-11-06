# R3 — Current contracts, regressions and bounded corrections

## Scope and method

Evaluated production: `eefd14fff41a58c1623f61d41e8b6580d4c21eac`, unchanged from certification HEAD `795f544`. Java/parser remain `d4995153b0a759859ebc8f859dd868abae39db5a` / `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. Original C01 failures, R2's isolated baseline and source comparisons are reused. No production has yet changed. New small NUnit probes distinguish resource fallback and configured graph tiers without updating historical expected values.

The pre-correction probe filter (`BmsPreviewAudioResolverTest|BmsGaugeHistoryConfiguredChainTest`, using `FullyQualifiedName~` clauses) built Release and executed **seven cases: two passed, five failed**. The existing-directory preview ordering and Normal-only graph passed; empty-source/missing-directory previews (three) and Easy/Assist graph tiers (two) failed. Original failed TRX/logs and hashes are preserved in `C01R-R3-probes-before`.

## CURRENT-COURSE — Explicit course overrides ignored

**H2, high impact, CORREGIR AHORA.** Affects 32 inheritance and four explicit-family observations. Reproduction: create a course constrained by `gauge_5k`, `gauge_7k`, `gauge_9k` or `gauge_24k`, build its mods, serialize/restore them, and configure the health processor. Its selected family is always LR2 rather than the requested family; consequently gains, damage and class state handling use another profile.

Evidence: `BmsCourseSession.ResolveCourseGaugeProfileFamily:237–254` maps those four strings to `Lr2`; original `CourseData:174–190` recognizes them and `GrooveGauge:128–144` explicitly selects FIVEKEYS/SEVENKEYS/PMS/KEYBOARD. This is an explicit override, not the normal layout-derived LR2 default. The course resolver's Git blob is unchanged from historical `59e186c`, and the isolated baseline reproduces all 36 cases. Their validity is independently supported by the Java switch, not by their old age.

Minimal correction: restore the four explicit mappings in this resolver. Keep `gauge_lr2`, unconstrained/default layouts, formulas, death thresholds, seeded GAS and mod-order rules unchanged. Existing family-selection and three-stage serialized-inheritance cases already provide regressions; no duplicate test matrix is necessary. Revalidate those cases and C05's actual context/replay cases before publication.

## CURRENT-PREVIEW — Optional discovery aborts preview loading

**H2, high impact, CORREGIR AHORA.** One working-beatmap transfer and seven zero-time playback observations share the same productive stack: `BmsWorkingBeatmap.createPreviewTrack` calls `BmsPreviewAudioResolver.GetExistingDedicatedPreviewCandidates`, then `getDedicatedPreviewCandidates:38` enumerates a source directory unconditionally.

An empty source throws `ArgumentException`; a missing source throws `DirectoryNotFoundException`. `BmsFileResourceStore` already documents and implements empty source as current directory (`:33–34`), and treats missing resource lookups as absent. Optional dedicated-preview discovery should not prevent fallback to the existing event/keysound preview. This can affect an in-memory working beatmap or a moved/missing chart directory, not just a test-run path.

The historical preview run executed nine cases: constructor passed, the same eight playback/transfer cases failed. The resolver is byte-identical in the baseline/current commits. New probes directly isolate both failures, while an existing directory still resolves explicit preview, named preview and fallback in that order. Thus R1's provisional E1 label for the seven zero-time observations is superseded by **H2**: environment supplies the trigger, but the resolver violates its current missing-resource contract.

Minimal correction: normalize empty source consistently with the store, then return no dedicated candidates if the normalized directory does not exist, before enumeration. Preserve ordering and normal sample/preview volume routing. This is not a general filesystem error-swallowing mechanism. Verify both new resolver tests and the existing playback/transfer tests; do not claim that fixing startup automatically certifies their later assertions.

## CURRENT-GRAPH — GAS lower limit omitted from legacy result graphs

**H2, medium functional/presentation impact, CORREGIR AHORA.** The old hit-event reconstruction route in `BmsGaugeHistoryGraph.gaugeTypesFor:567–575` always returns the four-entry base chain for regular GAS. Actual `BmsModGaugeAutoShift.ApplyToHealthProcessor:85–98` adds Easy or Easy/Assist according to `GasMode`. Default Easy therefore produces five real gameplay layers but only four graph series. A configured final/available gauge can be absent from the result display.

The new regression compares graph names with actual processor tiers for each configured lower limit: Normal passes; Easy and Assist fail before correction. The old baseline graph case also fails with four series, and the graph selector and base chain are unchanged from `59e186c`. This establishes a preexisting current mismatch. The old C01 graph/result-screen cases additionally assume all six tiers irrespective of the setting: that **secondary historical assertion can remain red even after the real missing-tier bug is fixed**. Do not change their assertions to hide this distinction.

Minimal correction: the regular graph selector should append only the lower tiers requested by the GAS setting. Preserve its existing class-chain branch and the recorded-history route. Do not redesign legacy reconstruction, add all Java GAS policies or promise exact HCN reconstruction from old hit events. Validate Normal/Easy/Assist names and relevant recorded/course graph cases.

## FIXTURE-CONFIG — Valid checks set up an incompatible chain

Five `BmsHealthProcessorTest.TestAutoGaugeClearThresholdBoundary(...,-1)` cases install the four-entry `AUTO_GAUGE_CHAIN`, then restore six snapshots including Easy/Assist and expect Easy to qualify. `RestoreGaugeStates` restores configured tiers; it does not manufacture extra ones. The test never installed its claimed fallback layers. **E1, CORREGIR AHORA in this one fixture setup**: explicitly configure Easy/Assist in this boundary test while keeping every existing assertion. This preserves the purpose of testing adjacent clear-threshold doubles, not a production policy change.

Two other old course/GAS context tests instantiate an unseeded new GAS mod after manually configuring a course context. The supported current course pipeline serializes its surviving seeds onto GAS; C05 checks both mod orders and reconstruction. Their setup does not represent that current pipeline. **E1, SOLO DOCUMENTAR** pending focused fixture maintenance. Do not revert the seed/identity protections to recover the old setup.

## Additional demonstrated H1 observations

The total H1 set increases from R2's 109 to **148 observations, nine policy causes**, with no historical test edit:

- **H-DAMAGE (7):** four GAS attribution cases, one health cascade, one visual attribution and one failure-point graph assume a short chart's BAD only kills Hazard. Executing the original C03 gauge caller for N=1/default TOTAL gives Hard 40% and ExHard 0% after BAD from 100%, so active Hard is correct. Another old graph expects survival through several misses on a two-note chart, ignoring LR2's note-count damage multiplier.
- **H-COLLECTIVE (2):** dense replay timing cases require every judgement to preserve +150 ms offset. In the same column, notes 1400/1700 with press 1550 are both BAD; the original collector retains the additional index 1. One press can consume both notes, so the initial all-offsets-equal assertion is invalid before the test reaches its rewind comparisons. That does not certify unreached rewind assertions.
- **H-LATEHEAD (1):** a long head pressed 180 ms late is rejected before consumption by original `JudgeManager:433–439` (late long head outside GOOD); the later passive Poor is correct. Do not turn it back into a consuming BAD to satisfy the old visual case.
- The replay HCN test requiring a living 0.0001 survival value contradicts 2% death; four visual algorithm cases expect a 250 ms late BAD outside the fixed 200 ms limit; four replay visualizer cases expect a white unjudged press even though LR2 ALWAYS/Empty Poor produces another coloured empty press.
- Seventeen visual long-note failures use the old PMS grace, tail windows or -3% BAD. Three poor-line assertions select the Empty Poor box as both outer ends under NoBad; LR2's one-sided Empty Poor ends at zero, while the positive playable window still extends beyond zero. Current `GetPoorDisplayOffsets` correctly uses the union's outer bounds.

Original Java calls for the short-chart damage and the two-note collector use the C06-compiled fixed sources and the same pinned auxiliary JAR. Results (in invariant-locale output): Hard 100→40, ExHard 100→0; collector extra index 1 for `(1400,1700),1550`. The initial locale-dependent output and invariant successor are both retained; the latter SHA-256 is `CA5A9076A39B73D8C595C0AD31D0443D75D40E1E4CF488541D68177DAF33E0F1`. These are component executions, not a full Java replay.

## Instrumentation verdict

| Observation | Result | Recommendation |
|---|---|---|
| INST-01 / F003 | H1 zero-margin assertion, reached before reapply. Baseline passes; current selected rule correctly settles immediately. Accumulator-loss claim not established. | SOLO DOCUMENTAR; preserve old test. Later maintenance should test reapply against current contract. |
| INST-02 / F001 (caution) | Optimized and independently selected stateless map points give equal 192 lifetime pairs; hard-coded fingerprint differs. Same actual hash in historical/current binaries. | U1, SOLO DOCUMENTAR; no demonstrated optimized lookup regression. |
| INST-03 / F002 (scrollplus) | Same sequence and same historical/current hash for this different chart. Shared failing assertion, not proof of identical underlying golden provenance. | U1, SOLO DOCUMENTAR. |

Minimal existing fingerprint sequence: decode the chart with fixed branch random selection, select 64 evenly distributed notes, refresh lifetime pairs at speeds 8/12/6, hash 192 `(start,end)` double pairs. The independent point search is injected in the existing instrumental test and exact equality passes before its golden assertion. The obsolete full arrays that produced those goldens are not supplied, so a 64-character hash cannot identify the first changed element or establish which lifetime formula was originally intended. No fingerprint is replaced blindly. Ordinary F008/F009 share the same unresolved golden issue.

## Lower-priority presentation and remaining uncertainty

**Eight P1 cases, CORREGIR DESPUÉS / design or fixture review:** six palette assertions (ExHard gold/Hazard gray versus old purple/gold), one unhosted settings-control visibility assertion, and one translation invariant. English has 395 keys, Chinese 393: missing `gas_lower_limit`, `gas_lower_limit_description`, `mod_gauge_auto_shift`, with obsolete extra `mod_auto_gauge`. The same mismatch exists in the baseline. Preserve it as a real localization maintenance issue, not a gameplay regression or proof that all UI is correct.

**Ten U1 observations, not automatically merge-safe:** four fingerprint observations (above); one mixed-chart difficulty golden with no independent expected derivation; one Duration candidate case with per-note narrow rates; three aggregate course-result statistic cases; one visual GAS downgrade observation. Their current messages/source are recorded in the inventory. A fixture timeout or fixed golden alone does not establish a production cause. The course-statistic and candidate cases deserve targeted future diagnosis if merge review requires those consumers; broad architectural changes or speculative expectation edits are not authorized by the available evidence. They remain CORREGIR DESPUÉS only after a valid counterexample, otherwise SOLO DOCUMENTAR.

## Decision before R4

Unique primary classification: **H1 148, H2 46, R1 0, V1 0, E1 7, P1 8, U1 10 = 219**. The 46 H2 observations represent **three** current functional causes, not 46 bugs; all three are demonstrated preexisting. No functional defect has been attributed to a parity commit without evidence. The seven new probes are additional tests and are not added to the original 219-case denominator.

Proceed with three small independent production corrections and one independent fixture-setup correction, each with its own validation/commit/push. Keep the original H1 files/expectations, known mod exclusions, reference checkouts and all earlier evidence intact. R5 will report actual pass/fail counts, unresolved cases and any secondary old assertions exposed after a corrected cause.
