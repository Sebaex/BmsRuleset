# C01-TM executions

All commands use filters, Release/net10.0 and SDK 10.0.401; method probes use the existing runsettings. Every failed/successful run has a fresh directory. External evidence paths are relative to `nuevo-trabajo`, without personal paths committed. Short lot results are not broad-suite or cross-platform results. Binary/source hashes remain with each run. No benchmark or broad suite has yet been launched.

## TM-F01 [TM-F]

Release/net10.0, SDK 10.0.401, source parent 2ec37021bb1cacf809476ce5b37a562b1839ce5e. Filter: `FullyQualifiedName~BmsDifficultyPerformanceRegressionTest|FullyQualifiedName~BmsStarRatingProcessorTest|FullyQualifiedName~BmsExecutedJavaJudgementTest.OriginalJavaWindowsMatchSupportedLayouts`. 23/23 pass, 0 fail, 0 other. TRX SHA-256 `6D28EA882177781A7EB2135510F06126845DB671FB3416900708F59AE9E3EAAF`; logs/filter/binary hashes retained under `evidencias-paridad/TM-F01`. IDs: F059, F060, F061, F062.

C02 original Java Rank2 GREAT is 40 ms; U1-B isolated this single input as cause of rating change

Sensitivity: Old GREAT 45 produces .09 and the old rating: both current exact assertions reject it; mixed-note preprocessing changes still alter the rating snapshot

## TM-G02 [TM-G]

Release/net10.0, SDK 10.0.401, source parent 6d5b6da05e2b2dcc8cd0c5303803f5001cfcaffb. Filter: `FullyQualifiedName~TestSceneBmsCourseResults`. 11/11 pass, 0 fail, 0 other. TRX SHA-256 `ACB4E5051FC3D0B062EA12B56A8704E118D0C2F56D78A3C45360CDC8812CEA67`; logs/filter/binary hashes retained under `evidencias-paridad/TM-G02`. IDs: F178, F179, F180.

Framework LocalisableString.ToString uses invariant formatting, unlike the old current-culture interpolation; full fixture now reaches all later assertions

Sensitivity: Wrong stage aggregate/text still violates independent expected score; explicit comma update-host case would fail old helper and executes full integration

## TM-E02 [TM-E]

Release/net10.0, SDK 10.0.401, source parent e2b2a6248948d65dc53a6b7e0f82db0f669cdd0b. Filter: `FullyQualifiedName~BmsTimingMapQueryTest|FullyQualifiedName~BmsExecutedJavaJudgementTest.OriginalJavaWindowsMatchSupportedLayouts`. 11/11 pass, 0 fail, 0 other. TRX SHA-256 `A1F78629B56600DD75846EF309F511BFD7743F6371123A87CBA8B24EF91E7D21`; logs/filter/binary hashes retained under `evidencias-paridad/TM-E02`. IDs: F001, F002, F008, F009.

U1-A exactly recovered old hashes with old bounds; LR2 BAD 200/fast empty POOR 1000 independently reconstruct current coverage

Sensitivity: Truncated head coverage/end margin fails independent arithmetic; wrong visibility fails separate immutable-point interpolation; original exact hashes retain bit-level sensitivity

## TM-H01 [TM-H]

Release/net10.0, SDK 10.0.401, source parent c5c578caed6a9185ace208a91c546bd24bcb6504. Filter: `FullyQualifiedName~TestSceneBmsHealthBar|FullyQualifiedName~TestHardProfilesUseDistinctFixedColours|FullyQualifiedName~TestHazardDisplayIsGoldAndHasNoClearLine|FullyQualifiedName~TestCourseProfilesUseTieredGaugeDisplay|FullyQualifiedName~TestCourseAutoGaugeStartsAtExHardClass|FullyQualifiedName~BmsLocalisationTest`. 40/41 pass, 1 fail, 0 other. TRX SHA-256 `FA53C0766995FEB9D44FF286BC5D7619E6DD1E0FBCD37C391A7D4720051B795F`; logs/filter/binary hashes retained under `evidencias-paridad/TM-H01`. IDs: F005, F006, F007, F080, F081, F092.

User explicitly approves current palette; translation synchronization lacks an exception and remains a visible production-resource limitation

Sensitivity: Wrong exact color or actual HUD binding fails; missing/wrong fallback text fails; original resource completeness still fails rather than being weakened

## TM-H-TR01 [TM-H]

Release/net10.0, SDK 10.0.401, source parent 15d77ea4b6fd39989b115c3b520713dce81a40e4. Filter: `FullyQualifiedName~BmsLocalisationTest|FullyQualifiedName~BmsGaugeHistoryConfiguredChainTest`. 23/23 pass, 0 fail, 0 other. TRX SHA-256 `D65EC4008CD47BF09698A6BB1538368E459A006A557BC612152C2534764C9F7D`; logs/filter/binary hashes retained under `evidencias-paridad/TM-H-TR01`. IDs: F133.

2026-11-08 user explicitly authorized contextual GAS translations; resource completeness fixed rather than exempted

Sensitivity: Removing any translation or retaining orphan key still fails original F133; exact bilingual cases catch unresolved/wrong text and language restoration

## TM-A03 [TM-A]

Release/net10.0, SDK 10.0.401, source parent 482ba56430c2f12ccd659d6796818cd550648179. Filter: `(FullyQualifiedName~BmsRulesetTest|FullyQualifiedName~BmsHitWindowsTest|FullyQualifiedName~BmsModNoGoodNoGreatTest|FullyQualifiedName~BmsHitErrorMeterTest|FullyQualifiedName~BmsColumnNoteIndexTest|FullyQualifiedName~BmsExecutedJavaJudgementTest)&FullyQualifiedName!~DurationSearchSkipsCloserHeadsWithNoHitWindow`. 91/91 pass, 0 fail, 0 other. TRX SHA-256 `90BD3A1B9C9F760F49BCA54FD8CF5B00B36D0F30AFC921F348083B2A807D6FF4`; logs/filter/binary hashes retained under `evidencias-paridad/TM-A03`. IDs: F010, F011, F063, F064, F065, F066, F067, F068, F069, F070, F071, F072, F073, F074, F075, F076, F077, F078, F100, F101, F102, F103, F104, F105, F106, F107, F108, F109, F127, F128, F129.

Original Java LR2 windows and ALWAYS/early-only policies replace incompatible historical bounds; C# UI/mod contracts preserve original consumer checks

Sensitivity: CSV-derived inside/on/outside controls catch wrong bounds/signs/inclusivity; exact counts and non-suppressed stacked misses catch policy/index regressions

## TM-A-UI01 [TM-A]

Release/net10.0, SDK 10.0.401, source parent 11cb743a23a7654dd43dc9623756e728aabf44f8. Filter: `FullyQualifiedName~TestSceneBmsColumnJudgement|FullyQualifiedName~TestSceneBmsJudgementAlgorithm|FullyQualifiedName~TestColourBarMatchesJudgements|FullyQualifiedName~TestPoorLinesFollowVisibleWindowEnds`. 32/32 pass, 0 fail, 0 other. TRX SHA-256 `26C7348E1FC494BB62202E822B0F734E630AEB9E5ADC3A78DC480EBD427EBD3A`; logs/filter/binary hashes retained under `evidencias-paridad/TM-A-UI01`. IDs: F174, F175, F176, F177, F181, F182, F183, F184, F204, F205, F206, F207, F208, F209, F210, F211, F212.

Java fixed BAD 200 and early empty POOR 1000 alter logical/spatial bounds; test inputs preserve genuine overlapping candidate selection

Sensitivity: Wrong primary now fails event order even if both notes are BAD; old deadlines, missing endpoints and wrong meter width/edge still fail exact integration checks

## TM-C01 [TM-C]

Release/net10.0, SDK 10.0.401, source parent 72272058e907acc4d9844293d0ebba46f3cb52c7. Filter: `FullyQualifiedName~BmsCourseHealthInheritanceTest|FullyQualifiedName~BmsHealthProcessorTest|FullyQualifiedName~BmsGaugeProfileTest|FullyQualifiedName~TestCourseGaugeProfileFamilySelection|FullyQualifiedName~BmsExecutedJavaGaugeTest|FullyQualifiedName~BmsGasCourseParityTest`. 221/221 pass, 0 fail, 0 other. TRX SHA-256 `5AD19FAD1AAB48EFFC82CA22D4DA06AD4546F61D07C8EF0F7259AB2CEE01E6B1`; logs/filter/binary hashes retained under `evidencias-paridad/TM-C01`. IDs: F020, F021, F022, F023, F024, F025, F026, F027, F028, F029, F079, F082, F083, F084, F085, F091, F093, F094, F095, F096.

Original Java LR2 death and arithmetic replace impossible living sub-2% states; added valid boundary inputs preserve original lifecycle purpose

Sensitivity: Refill/rounding, wrong death/failed flags, revived tiers or lost reset state fail exact inheritance; wrong scale/family/guts fails independent constants and protected vectors

## TM-D03 [TM-D]

Release/net10.0, SDK 10.0.401, source parent 97f729226b8d1439ff11bd09882cda233201ec7a. Filter: `FullyQualifiedName~BmsModGaugeAutoShiftTest|FullyQualifiedName~BmsCourseSessionTest|FullyQualifiedName~BmsGaugeHistoryGraphTest|FullyQualifiedName~BmsGaugeHistoryConfiguredChainTest|FullyQualifiedName~BmsGasCourseParityTest|FullyQualifiedName~BmsGasOrderParityTest|FullyQualifiedName~BmsGasPolicyReferenceTest`. 137/138 pass, 1 fail, 0 other. TRX SHA-256 `1D32DB70FFEE7BB501BC11D8D4AA7A7095EC7117756A68836F6C35E970ED4A0B`; logs/filter/binary hashes retained under `evidencias-paridad/TM-D03`. IDs: F054, F134, F135, F136, F137, F138, F139, F141, F142.

Accepted real Course/GAS configuration and executed Java short-chart arithmetic replace obsolete assumptions; newly exposed setting loss is left failing

Sensitivity: Lost/refilled seeds, wrong hidden/public control, omitted tier, stale attribution or wrong failure event fail exact checks; Assist serialization remains a separate visible production limitation

## TM-GAS-FIX01 [TM-GAS-FIX]

Release/net10.0 SDK 10.0.401. D1 filter repeated against the authorized production correction: 139/139 pass, zero failed/omitted; includes one new reset test. TRX SHA-256: 13B1972E9D605942F88C7F0F490AE5B150C0352C691DE30ACC4B394876A7E024. TM-D01/D02/D03 remain preserved. See GAS_SERIALISATION_FIX.md for scope and limits.

## TM-D-UI01 [TM-D]

Release/net10.0, SDK 10.0.401, source parent 029a4addd4b950765bd90c5f16858ab93f7cf179. Filter: `FullyQualifiedName~TestAutoGaugeFinalScoreAttributionUsesResolvedGauge|FullyQualifiedName~TestSceneBmsMods.TestAutoGauge|FullyQualifiedName~TestSceneBmsResultScreenStatistics.TestAutoGaugeHistory`. 3/3 pass, 0 fail, 0 other. TRX SHA-256 `25BC010AFD86617C7CEBE621D92E5196C3FA0ED318FBE8912EF6B908761A6E99`; logs/filter/binary hashes retained under `evidencias-paridad/TM-D-UI01`. IDs: F143, F196, F203.

Executed LR2 damage and accepted configured GAS clear/display policy replace historical assumptions

Sensitivity: Wrong attribution, omitted/extra series, wrong threshold inclusivity or stale HUD/history fails independent integration assertions
