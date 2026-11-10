# C04 — Long notes and parser

## Objective and versions

C# evaluated: `b6fa4b29234c799973486f026cfecb0495298d5d`; production unchanged from `bae80bc`. Java and parser fixed commits: `d4995153b0a759859ebc8f859dd868abae39db5a` and `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. Evidence commit appears in the index. No scheduler or long-note implementation was changed.

## Parser: executed independent reference

`ParserOracle.java` compiles original `BMSDecoder`, `Section`, `ChartDecoder`, `ChartInformation`, `DecodeLog` and `BMSModelUtils` in addition to C02/C03's explicitly compiled types. It decodes the same ASCII chart (`#BPM 120`, `#LNTYPE 1`, `#00151:0102`) with absent/0/1/2/3 LNMODE and player LN/CN/HCN. The Java player constants are 0/1/2, distinct from LongNote type constants and C# mode values; the caller maps these explicitly.

All 15 rows produce locked mode zero and scoring-note count 1 for LN, 2 for CN/HCN. `BmsExecutedJavaParserTest` decodes and converts the equivalent chart, applies the corresponding existing player mod, then performs a successful head/tail replay and compares actual scoring-event count with Java. Source-derived expectations are not used in place of these Java outputs. **CERTIFICADO — EJECUCIÓN DIFERENCIAL** for this LNMODE/header and note-count matrix, not the parser in general. Parser stderr warns about missing TOTAL/default PLAYER; it is retained separately and does not affect this property.

The final generator also recompiles original `BMSModelUtils` for C03's gauge chart counts: all previous window/comparator/collector/gauge/score CSVs remain identical. Auxiliary dependency and JDK restrictions remain those documented in C02. `C04_PROVENANCE.json` records the additional sources and vectors; earlier provenance snapshots remain intact.

## State-machine coverage and limits

| Behavior | Existing executed checks | What they establish |
|---|---|---|
| LN head, publication, holding and tail | `BmsLongNoteExecutionTest` | C# state and callback ordering; no original Java state-machine run |
| CN independent head/tail, passive miss, scratch release/reverse press | `BmsChargeNoteExecutionTest` | C# results, tail lifetime and scratch interaction |
| HCN body after miss/successful tail, held/released body, no duplicate ticks | `BmsHellChargeNoteExecutionTest`, `BmsHellChargeBodyTrackerTest` | C# body/tail independence and deterministic accumulation |
| Chart ending with an active hold | `BmsJudgementEngineTest.EndSettlesHoldsThroughTheSameEngine` | Existing LN/CN/HCN completion path, including aborted endpoint |
| Mode selection and endpoint count | `BmsLongNoteModeModsTest`, five header cases and the new Java CSV | C# player-mode behavior; differential parser/count matrix only |
| Early/late release and repress across all key/scratch modes | Prior rules/tests and static implementations | Not uniformly green in C01; no complete differential matrix obtained |

All successful C# rows are **VALIDADO — PRUEBAS C#**, not certification of every Java LN/CN/HCN transition. C01 still includes failing legacy long-note rules/judgement, visual tests and HCN reapply instrumentation. Those expectations were neither edited nor dismissed. Complete keyboard/scratch LN/CN/HCN state-machine parity is **NO CERTIFICADO**.

## HCN temporal difference (commit 1bca379)

The earlier `BmsHcnFrameReferenceTest` is a C# execution against an independently transcribed accumulator rule, **not an executed Java player**. Original `JudgeManager.update`, lines 331–354, adds signed elapsed microseconds and uses one strict `> 200000` or `< -200000` test per call. `BmsHellChargeBodyTracker` settles all crossed thresholds. Its strict boundary produces nine ticks over a 2000 ms active-body interval; the transcribed Java rule produces nine with 1 ms/16.6667 ms/100 ms partitions, four with 500 ms calls and two with 1000 ms calls. Holding/releasing changes the sign; the existing 1 ms signed-transition tests compare counts and resulting gauge at each step.

The nine-versus-four case is a **DIFERENCIA CONFIRMADA in the mathematical update rules**, not a full-player differential execution. It can change the gauge whenever the body remains active during a long update and gains/damage do not clamp away the difference. A real application hitch can create such an elapsed interval; no live original Java hitch or recorded original Java replay was executed here. Equivalence is therefore conditional on the examined bounded partitions and identical hold-state transitions, not guaranteed for every frame schedule or long history.

C# `BmsJudgementEngine.advance/NextEventTime` explicitly visits body deadlines, and `BmsReplaySimulation.AdvanceTo` visits engine/input events. This supports C# gameplay/replay determinism even with sparse replay frames. It does not reproduce Java's per-update cap. Making C# depend on rendering/update stalls would discard its documented catch-up behavior; no production change is justified by the existing abstract vector alone. Full end-to-end Java/C# HCN gauge comparison remains **NO CERTIFICADO** and the temporal difference remains visible.

## Executions and conclusion

Release filtered run `CERT-C04`: **50 passed, zero failed/skipped**, including executed parser/gauge cases, mode mods, LN/CN/HCN execution and HCN frame reference. Successor `CERT-C04-02` additionally runs the engine's completion/order cases; its result is recorded in C06. Filters, original Java stderr, TRXs, build logs and binary hashes are retained externally. Reproduce with the named fixture classes joined by `FullyQualifiedName~...` OR clauses, as recorded in each filter file; no full unfiltered suite or benchmark was run.

LNMODE and endpoint-count vectors agree with executed original Java. The wider long-note machinery has focused C# validation and static comparison, with an explicit HCN temporal non-equivalence and unresolved broad regressions. No claim of integral LN/CN/HCN parity follows.
