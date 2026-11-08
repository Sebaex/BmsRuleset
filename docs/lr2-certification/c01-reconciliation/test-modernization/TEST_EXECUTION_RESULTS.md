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
