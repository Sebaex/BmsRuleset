# R4 — Individually verified minimal corrections

Reference and methodology remain those in R3. Earlier failed TRXs, historical H1 tests and original inventories are preserved. Each subsection is published with only its corresponding correction and documentation; the index records its commit after remote verification.

## R4-C — Explicit course gauge constraints

Tested parent: `e8ca443dfa3d6876d6a64fd855bb19565a8d276b`. Changed only the four explicit mappings in `BmsCourseSession.ResolveCourseGaugeProfileFamily`: `gauge_5k/7k/9k/24k` now select FiveKeys/SevenKeys/Pms/Keyboard. `gauge_lr2`, default layout selection, formulas and persisted-state machinery are unchanged. This follows original `GrooveGauge.create`'s explicit course overrides, not a return to legacy defaults.

Release build succeeded. `C01R-R4-course-after` executed **88 cases, all passed**, no omissions: all single/auto three-stage inheritance cases, explicit course family selection, C05 course/order/policy/mixed-replay regressions and original Java gauge tests. Existing valid tests were sufficient; no expected value was edited. All 36 original explicit-family failures in this subset pass. The ten different LR2 sub-threshold-seed cases remain historical and were not included as acceptance tests for this correction.

Potential impact: future course sessions honoring explicit non-LR2 constraints now use that requested profile. Already serialized explicit mod settings remain their recorded values. This does not certify every legacy family formula or change the certified default LR2 behavior. Binary hashes, successful and original failed TRXs and a copy of the candidate production DLL are retained externally.

## R4-P — Missing and empty preview source

Tested parent: `f6f7af30cc3e87f6aaf339ea7cd3993f2ceb16de`. `BmsPreviewAudioResolver.GetExistingDedicatedPreviewCandidates` now normalizes an empty source as the current directory, matching its resource store, and returns an empty dedicated-preview list for a nonexistent directory. Five added lines leave the existing explicit/named/fallback ordering and event/keysound preview available. No audio volume policy or missing-file decoder behavior was changed.

`C01R-R4-preview-after`: Release build succeeded; **72 filtered cases passed**, zero failures/omissions, including all four new resolver cases and the existing preview, track transfer and zero-time playback fixtures. The eight original startup exceptions no longer occur and their later playback assertions pass. Original pre-fix C01/baseline/probe results remain intact; candidate DLL and binary hashes are retained with this successor.

Scope limit: the guard handles an absent directory at discovery time; it does not introduce broad exception swallowing or guarantee that files cannot disappear during later audio decoding. That wider lifecycle was not rewritten.

## R4-G — Configured lower tiers in legacy GAS graphs

Tested parent: `b98c9db857d24f52d25786a25900b4f6e1f7809b`. The regular-GAS branch of `BmsGaugeHistoryGraph.gaugeTypesFor` now reads `GasMode`: Normal uses the four base tiers, Easy appends Easy, Assist appends Easy/Assist. The default matches the mod's existing Easy fallback. The class-gauge and already-recorded-history routes are unchanged.

`C01R-R4-graph-after`: Release build succeeded; **28 executed, 26 passed, two failed**, zero omissions. All three new configured-chain cases pass (Easy/Assist failed before). Existing policy/mixed-replay and other graph checks pass. The two preserved historical failures are the unconditional six-series assertion (now correctly five for default Easy, formerly four) and the old short-chart ExHard failure-time expectation (0.875 versus independently supported 0.125). Neither expected value was changed. The default-Easy omission is fixed even though the old graph assertion remains red for its unrelated demand that Assist also be present.

This is a correction to which configured tiers are shown, not a certification of every health value reconstructed from old hit events or of all course/profile policies. Candidate DLL, original failed probe and successor results are retained externally.
