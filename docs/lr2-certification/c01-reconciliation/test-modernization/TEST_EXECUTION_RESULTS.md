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
