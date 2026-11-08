# Expectation ledger and source hierarchy

Pinned primary sources: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, jbms-parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. Framework reference `37c3e328c8cc7a018234ec78982ea12d3712096a`, osu `9a10d935b05a3b896f4bd811d620f2023331db93`. References are read-only. Prior C02–C06 callers, CSVs and provenance are unchanged. Java contradicting C# is not grounds for changing a test to C# unless an explicit project decision governs that extension.

## F — difficulty (F059–F062)

| Test file / method / assertion | Before | Now | Independent authority |
|---|---|---|---|
| `Normal/Difficulty/BmsStarRatingProcessorTest.TestStarRatingProcessorHitLeniencyUsesBmsGreatWindow`, three layout parameters | Rank 2 GREAT 45/37.5/35 ms | 40 ms for all three layouts | Executed `windows.csv`: `rank,2,NOTE,1,-40000,40000`; original `JudgeProperty.LR2`, `BMSPlayerRule` default LR2 |
| `Normal/Difficulty/BmsDifficultyPerformanceRegressionTest.MixedChartKeepsOriginalStarRating`, unchanged 20,000-note snapshot | 10.748954856597857 | 10.98694842655334, same 1e-12 tolerance | U1-B controlled GREAT-only counterfactual exactly recovers the old golden; unchanged C# difficulty algorithm is an accepted C# feature, not a Java rating oracle |
| Same mixed test, additional input invariant | no separate leniency assertion | `.3*sqrt(40/500)` with documented min-transform | Accepted C# leniency formula, U1-B causal isolation; literal 40 is independently executed Java GREAT, not read back from production |

All layout parameters, 20,000 timings, eight columns, long notes, ordering and numerical tolerances are retained. Later batches append their own per-assertion source ledger before publication.

## G — Course results (F178–F180)

`Visualize/TestSceneBmsCourseResults.overviewMatches`: only the expected percentage's formatting path changes, from update-thread current-culture interpolation to `BmsStrings.ResultPercentage(expectedAccuracy).ToString()`. Authority: pinned framework `LocalisableFormattableString.ToString` uses invariant culture; U1-D independently identified this exact false conjunct. The expected accuracy still comes from the fixture's weighted scores, not the actual overview. Accuracy, combo, PGREAT, FAST/SLOW, stage state, layout, selection, restoration, back navigation, charts and disposal assertions remain unchanged.

New `TestCourseSummaryWithCommaDecimalHostLocale` repeats the full original 1280×720 integration scenario after setting the **test host's** locale and waiting for the update thread's decimal separator to be comma. It separately requires the invariant representation `77.00%`; teardown restores the previous host setting. This follows the framework ThreadCultureTest's event-driven host contract, using public `Value`/`TriggerChange`, not its internal `SetValue` API or NUnit test-thread culture. No production culture or rendered-string policy changes.

## E — lifetime snapshots (F001/F002/F008/F009)

`BmsTimingMapQueryTest` constants now use the documented LR2 hashes: caution old `FB282145C2C0AC8E90BBC4C4FFC9F2C9CB2E818D81A0E1EBCBE8342043D0FFFD` → `96F06BC3CA3385E245C4A4A4A945E36845B9DAE2640336A894DF7BEE1BD2DF39`; scrollplus old `13D57E0EB00D932CC736B71494A633B0BB68707A8380B0616B1CADB3F10C570C` → `5929033CF48AAF4CAA224FF09A27CA029E1322AE70E43E72DE9BF39737074BB7`. U1-A exactly recovered both old hashes using the old fast coverage 500 ms and rate-scaled BAD 280/290 ms. Current Java LR2 windows.csv independently gives BAD 200 and fast empty POOR 1000, including LN-tail BAD.

Both original methods additionally invoke `assertLr2LifetimeBounds`: end must equal note end + (200 + accepted 100-ms passive margin); start must cover head−1000 and agree with a separate spatial visibility search. That helper selects immutable timeline points by an independent upper-bound binary search, interpolates scroll coordinates, checks head/tail distance against the accepted C# viewport/speed rule, and scans visibility without invoking production lifetime-entry methods or map queries. The 1-ms start tolerance belongs only to the **new independent visibility comparison**, because the two bracketing searches differ and accepted refinement precision is 1 ms. Original hash/equality comparisons remain exact. This is an accepted C# presentation contract, not a Java lifetime oracle.
