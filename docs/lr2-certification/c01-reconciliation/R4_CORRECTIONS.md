# R4 — Individually verified minimal corrections

Reference and methodology remain those in R3. Earlier failed TRXs, historical H1 tests and original inventories are preserved. Each subsection is published with only its corresponding correction and documentation; the index records its commit after remote verification.

## R4-C — Explicit course gauge constraints

Tested parent: `e8ca443dfa3d6876d6a64fd855bb19565a8d276b`. Changed only the four explicit mappings in `BmsCourseSession.ResolveCourseGaugeProfileFamily`: `gauge_5k/7k/9k/24k` now select FiveKeys/SevenKeys/Pms/Keyboard. `gauge_lr2`, default layout selection, formulas and persisted-state machinery are unchanged. This follows original `GrooveGauge.create`'s explicit course overrides, not a return to legacy defaults.

Release build succeeded. `C01R-R4-course-after` executed **88 cases, all passed**, no omissions: all single/auto three-stage inheritance cases, explicit course family selection, C05 course/order/policy/mixed-replay regressions and original Java gauge tests. Existing valid tests were sufficient; no expected value was edited. All 36 original explicit-family failures in this subset pass. The ten different LR2 sub-threshold-seed cases remain historical and were not included as acceptance tests for this correction.

Potential impact: future course sessions honoring explicit non-LR2 constraints now use that requested profile. Already serialized explicit mod settings remain their recorded values. This does not certify every legacy family formula or change the certified default LR2 behavior. Binary hashes, successful and original failed TRXs and a copy of the candidate production DLL are retained externally.
