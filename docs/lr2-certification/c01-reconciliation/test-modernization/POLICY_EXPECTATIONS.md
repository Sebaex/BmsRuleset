# Expectation ledger and source hierarchy

Pinned primary sources: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, jbms-parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. Framework reference `37c3e328c8cc7a018234ec78982ea12d3712096a`, osu `9a10d935b05a3b896f4bd811d620f2023331db93`. References are read-only. Prior C02–C06 callers, CSVs and provenance are unchanged. Java contradicting C# is not grounds for changing a test to C# unless an explicit project decision governs that extension.

## F — difficulty (F059–F062)

| Test file / method / assertion | Before | Now | Independent authority |
|---|---|---|---|
| `Normal/Difficulty/BmsStarRatingProcessorTest.TestStarRatingProcessorHitLeniencyUsesBmsGreatWindow`, three layout parameters | Rank 2 GREAT 45/37.5/35 ms | 40 ms for all three layouts | Executed `windows.csv`: `rank,2,NOTE,1,-40000,40000`; original `JudgeProperty.LR2`, `BMSPlayerRule` default LR2 |
| `Normal/Difficulty/BmsDifficultyPerformanceRegressionTest.MixedChartKeepsOriginalStarRating`, unchanged 20,000-note snapshot | 10.748954856597857 | 10.98694842655334, same 1e-12 tolerance | U1-B controlled GREAT-only counterfactual exactly recovers the old golden; unchanged C# difficulty algorithm is an accepted C# feature, not a Java rating oracle |
| Same mixed test, additional input invariant | no separate leniency assertion | `.3*sqrt(40/500)` with documented min-transform | Accepted C# leniency formula, U1-B causal isolation; literal 40 is independently executed Java GREAT, not read back from production |

All layout parameters, 20,000 timings, eight columns, long notes, ordering and numerical tolerances are retained. Later batches append their own per-assertion source ledger before publication.
