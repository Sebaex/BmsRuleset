# R1 — Original failure inventory and historical comparison

## Scope and method

Evaluated checkout: `795f54403a5db3b1db8ffa6aa985ace38182df92`, matching the fork's authorized branch and remote, with empty staging and no preexisting untracked files. No production, fixture, expected value, mod or dependency was changed. Existing C01 TRXs were parsed rather than rerunning the broad suite.

The ordinary TRX SHA-256 is `2351E0BE7009A29875BBD4EB1A8234385FA38F307D0A15FCA9782043BF3EEBCE`; instrumentation TRX SHA-256 is `8FA8C03414AA255D3ABA39C1840326AE11302F75757906B5938E1E17AD461226`. These match the historical `C06_EVIDENCE.json` manifest. Full messages, expected/actual values and stacks remain in those originals and an external extraction. The committed CSV uses short sanitized messages and repository-relative source paths.

Each of `C01_FAILURES.csv`'s 216 class/case identities was matched exactly against the ordinary TRX failures, then the three instrumentation identities were added. `R1_FAILURE_TRIAGE.csv` has **219 unique suite/class/case rows**, linked to **42 fixture families**, with a stable F identifier, source file, baseline-file comparison and provisional category. The two fingerprint cases have ordinary and instrumentation variants; they are distinct observations, not four independently proven bugs.

All 42 relevant test source files existed and have identical Git blobs at historical `59e186c527bb8e2ab3e8ea82c6668e21b4bf147a` and current HEAD. This establishes unchanged test code, **not** that every failure occurred historically. Production changes and LR2 policy must be examined separately. No H2 or R1 origin claim follows from this file comparison alone.

## Provisional grouping and work to follow

The table below groups observations by fixture as an initial index. Every group is U1 pending policy/functional analysis except G42: its seven exceptions explicitly report the absent test-output `BMS` directory (E1, setup/resource condition). E1 here does not imply that zero-time gameplay is correct; the missing assets prevent reaching that behavior.

- G04/G07–G09/G14/G18/G31/G35/G41: test whether old window constants and derived HUD/difficulty expectations conflict with executed LR2 tables. Mixed rendering/timing cases require separate checking before H1.
- G05/G06/G10/G11/G21/G23/G24/G37/G40: split profile selection, death thresholds, palette and GAS attribution from actual loss of inherited state. In particular, the 42 course failures are not classified together as historical.
- G12/G13/G15–G17/G19/G25–G30/G33/G34/G36/G38: split PMS grace, head/tail windows, Empty Poor, collective BAD, HCN damage, reapply and rewind. Use existing differential evidence only for the properties it actually covers.
- G03: inspect both ordinary fingerprint failures and INST-02/03 before blaming scroll/lifetime. Original TRX shows the independent reference and optimized path agree before the hard-coded fingerprint assertion fails.
- G01/G20/G32/G39/G42 and remaining visual-only checks: investigate resource setup, localization, result statistics and input visibility on their actual contracts; defer cosmetic work unless it misrepresents gameplay state.

The appended fixture table supplies affected counts and source-state checks. Detailed case names and summaries are in the CSV; later R2/R3 conclusions use those stable IDs. No mass historical classification is made at this stage.

## Omitted-test counter reconciliation

The original ordinary TRX summary says `total=3914`, `executed=3911`, `passed=3695`, `failed=216`, but `notExecuted=0`. Its individual results instead contain exactly **three `NotExecuted` entries**:

1. `CompareLocalReplays`: local replay corpus environment variable not supplied.
2. `TestMpegFallsToMissingWhenSupplementalUnavailable`: supplemental FFmpeg backend is embedded in this build.
3. `TestDeclinesCleanlyWhenNativeBackendNotEmbedded`: the same embedded-backend condition makes the alternative-path check inapplicable.

Thus C01's three omitted cases are supported by the actual result entries and `total-executed`; C06's JSON field of zero copied only the inconsistent summary counter. **The correct observed omitted count is three**, not zero. Instrumentation has 288 executed results and no omissions. Historical documents/JSON are preserved with this explicit erratum.

## Outcome and limits

Provisional unique classification: **E1 = 7; U1 = 212; all other categories = 0**. This deliberately records uncertainty before R2/R3. The original Windows environment and binary provenance are unchanged from C01; no new runtime result, Linux classification or baseline execution is claimed in R1. Full source-history metadata is retained externally. R2 will use focused historical executions where helpful, and R3 will prioritize the three instrumentation observations and actual state invariants.

## Fixture-family index

| Family | Fixture | Cases | Test file versus historical baseline | Provisional category |
|---|---|---:|---|---|
| G01 | `BmsWorkingBeatmapCacheTest` | 1 | unchanged | U1 |
| G02 | `TestSceneBmsHealthBar` | 3 | unchanged | U1 |
| G03 | `BmsTimingMapQueryTest` | 4 | unchanged | U1 |
| G04 | `BmsRulesetTest` | 2 | unchanged | U1 |
| G05 | `BmsCourseHealthInheritanceTest` | 42 | unchanged | U1 |
| G06 | `BmsCourseSessionTest` | 5 | unchanged | U1 |
| G07 | `BmsDifficultyPerformanceRegressionTest` | 1 | unchanged | U1 |
| G08 | `BmsStarRatingProcessorTest` | 3 | unchanged | U1 |
| G09 | `BmsHitErrorMeterTest` | 16 | unchanged | U1 |
| G10 | `BmsGaugeProfileTest` | 6 | unchanged | U1 |
| G11 | `BmsHealthProcessorTest` | 12 | unchanged | U1 |
| G12 | `BmsBeatorajaFrameHistoryTest` | 2 | unchanged | U1 |
| G13 | `BmsColumnNoteIndexTest` | 3 | unchanged | U1 |
| G14 | `BmsHitWindowsTest` | 8 | unchanged | U1 |
| G15 | `BmsLongNoteJudgementTest` | 3 | unchanged | U1 |
| G16 | `BmsLongNotePresentationTest` | 6 | unchanged | U1 |
| G17 | `BmsLongNoteRuleTest` | 8 | unchanged | U1 |
| G18 | `BmsModNoGoodNoGreatTest` | 3 | unchanged | U1 |
| G19 | `BmsOperationSequenceTest` | 3 | unchanged | U1 |
| G20 | `BmsLocalisationTest` | 1 | unchanged | U1 |
| G21 | `BmsModGaugeAutoShiftTest` | 6 | unchanged | U1 |
| G22 | `BmsReplaySimulationTest` | 1 | unchanged | U1 |
| G23 | `BmsGaugeHistoryGraphTest` | 2 | unchanged | U1 |
| G24 | `TestSceneBmsAutoGaugeScoreAttribution` | 1 | unchanged | U1 |
| G25 | `TestSceneBmsBeatorajaHcnFrameOrder` | 4 | unchanged | U1 |
| G26 | `TestSceneBmsBeatorajaHcnKeyboardInput` | 2 | unchanged | U1 |
| G27 | `TestSceneBmsBeatorajaJudgements` | 2 | unchanged | U1 |
| G28 | `TestSceneBmsBeatorajaLongNotes` | 18 | unchanged | U1 |
| G29 | `TestSceneBmsBeatorajaPmsConsumers` | 2 | unchanged | U1 |
| G30 | `TestSceneBmsBeatorajaScratchInput` | 2 | unchanged | U1 |
| G31 | `TestSceneBmsColumnJudgement` | 4 | unchanged | U1 |
| G32 | `TestSceneBmsCourseResults` | 3 | unchanged | U1 |
| G33 | `TestSceneBmsJudgementAlgorithm` | 4 | unchanged | U1 |
| G34 | `TestSceneBmsJudgementLifecycleExecution` | 1 | unchanged | U1 |
| G35 | `TestSceneBmsLongNoteJudgement` | 3 | unchanged | U1 |
| G36 | `TestSceneBmsLongNoteLifecycle` | 8 | unchanged | U1 |
| G37 | `TestSceneBmsMods` | 1 | unchanged | U1 |
| G38 | `TestSceneBmsReplayRewindTiming` | 2 | unchanged | U1 |
| G39 | `TestSceneBmsReplayVisualizer` | 4 | unchanged | U1 |
| G40 | `TestSceneBmsResultScreenStatistics` | 1 | unchanged | U1 |
| G41 | `TestSceneBmsSongProgress` | 9 | unchanged | U1 |
| G42 | `TestSceneBmsZeroTimeNotes` | 7 | unchanged | E1 |
