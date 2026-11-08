# U1-E — F196: visual GAS transition

**H1: the fixture retains a sequential Normal-tier expectation from the earlier display policy.** The observed transition is correct under the current clear-threshold policy and configured Easy lower limit. Secondary fixture issue: its fixed autoplay cutoff does not guarantee Normal is qualified when Hard dies. No new GAS state/HUD disagreement is demonstrated. Confidence: high for this vector and the tested boundary, not integral GAS certification.

## Actual vector and decision

`Visualize/TestSceneBmsMods.cs:109–140` loads `new BmsModGaugeAutoShift()` with autoplay ending at 13250 ms, then expects any recorded Normal tier. `GasMode` defaults to Easy (`Gameplay/Mods/Gauge/BmsModGaugeAutoShift.cs:52,58–103`), so the effective deduplicated chain is Hazard/ExHard/Hard/Normal/Easy, not a chain ending at Normal. `BmsTestBeatmaps.CreateBeatmap` uses Bme7K, Rank 2, TOTAL 160 and a mixture of notes, LN scenarios and mines. The diagnostic drives the **original loaded player/replay**, not a replacement simulation.

Key history snapshots (normalized health):

| Event time (ms) | Visible/history tier | Hazard | ExHard | Hard | Normal | Easy |
|---:|---|---:|---:|---:|---:|---:|
| 3300.0000000000005 | Hazard | 1 | 1 | 1 | .2253968253968254 | .2304761904761905 |
| 13450.000000000002 | Hard | 0, failed | 0, failed | .49561540222167966 | .851507936507937 | .952 |
| 13575.000000000002 | **Easy** | 0, failed | 0, failed | 0, failed | **.7915079365079369** | **.9039999999999999** |

Normal was above .8 while survival Hard was still active. The next miss subtracts .06 from Normal and .048 from Easy (LR2 profile factory `:98–109`), producing the last row. At Hard's failure Normal is **below** its .8 clear threshold, while Easy is above .8. Remaining misses only lower these total gauges; the replay does not refill Normal. No later Normal record is required or expected. The trace does not record ExHard as a separate displayed step; do not invent an intermediate frame from the chain order.

`BmsHealthProcessor.resolveActiveState/resolveDisplayGaugeIndex` (`Gameplay/Scoring/BmsHealthProcessor.cs:464–520`) deliberately keeps surviving survival gauges visible, then chooses the hardest nonfailed total gauge meeting its clear threshold; if none meets it, it falls back to the last surviving total gauge. A nonfailed Normal below threshold need not be displayed before Easy. `recordGaugeHistory:522–535` records that same display index. This is distinct from failure/result qualification; a displayed lower tier is not evidence that Normal's HP state was destroyed.

## Independent boundary check and HUD observation

A three-case temporary test seeds Hazard/ExHard/Hard failed, Easy .904 and Normal equal to .7915079365079369, .8 or .8000001 using the existing public `SetGaugeTypes` with explicit LR2 profiles. Expected selection follows the independently stated hardest-qualified rule: Easy below .8, Normal at/above .8. **All three pass**, and `DisplayProfile.Value == GaugeProfile.Display` in each case. No new production hooks or numerical expectations derived only from C# output.

Loaded-player diagnosis finds **one actual `BmsHealthDisplay`**, not an empty/vacuous traversal. At each observed tier (Hazard, Hard, Easy), its private bound `displayProfile.Value` equals the processor's public `DisplayProfile.Value`; current `GaugeType` matches the recorded transition. The read-only reflection probe prints `displays=1 profileMatches=[True]` at all three points, and never reports a mismatch. Production `UI/HudComponents/BmsHealthDisplay.cs:149–157,168–175,252–266` binds health/profile and chooses fixed colours from the same processor. This verifies tier/profile binding; it is **not a pixel comparison or proof of every skin's appearance**. Numeric fill smoothing is a separate presentation behavior, not an incorrect tier.

## Historical attribution and scope

Upstream `cd1beb4fdb1b610bf7eecfcd62c71a4f95314c86`, an ancestor of baseline `59e186c`, introduced `displayGaugeIndex`, the threshold-based resolver, and history recording of that display index. Previously `GaugeType`/history used the first surviving active gauge directly; the old test's sequential Normal assumption fitted that earlier policy. The current fixture keeps its Normal expectation. Baseline's original case also times out at exactly `gauge downgraded through Normal`.

The resolver/history branches are unchanged between baseline and starting HEAD `2ab2ad3`. Later clamp/profile/context changes exist and are not ignored, but the actual snapshot is far below the threshold (about .00849) and the boundary control confirms the selected policy. No new functional defect is attributable to those changes from this case. R4-G fixed configured tiers in `BmsGaugeHistoryGraph`; it did not change this live resolver or make a particular tier mandatory. These independent concerns are not conflated. Existing C03/C05 gauge proofs remain limited to their recorded vectors; this C# display-policy check needs no new Java execution.

## Evidence

External evidence-root-relative paths; .NET SDK 10.0.401, Release/net10.0. No production modification or changed original expectation; temporary fixtures are archived and removed.

* `evidencias-paridad/U1-E-01/tests.trx`: original visual case with transition-only logging, **0/1 pass**, timeout about 12 seconds; SHA-256 `B4134EC9913406BF2586DE49C7CFA7C96F24B27692F58CDF973B7E4F44828ED3`. Exact diagnostic source `investigacion-u1-20261107/TestSceneBmsMods.diagnostic.cs`, SHA-256 `56A893E4617513EA5EC7AD74A984E82A2931C7E128C4394DDE209F1836F14CDC`.
* `investigacion-u1-20261107/E-baseline/tests.trx`: untouched isolated baseline test binary, **0/1 pass**, same timeout, about 12 seconds; SHA-256 `E0EB5D2FA5405245F44DA5334E463669B06522284E9A0078D14DE5994D1FD9F6`.
* `evidencias-paridad/U1-E-02/tests.trx`: filter `FullyQualifiedName~U1GasSnapshotProbe|FullyQualifiedName~TestSceneBmsMods.TestAutoGauge`, **3 pass / 1 fail** in about 13 seconds. Threshold probes pass; original timeout remains and all three observed loaded-HUD bindings match. TRX SHA-256 `A90CAE0280FB1DF580662B29AF5F63ADE89FC7E1BD44CA81663D882D819C9C89`.
* E02 source overlay `investigacion-u1-20261107/TestSceneBmsMods.diagnostic-E02.cs`, SHA-256 `8D5744DEF6892F7363184714A55E2C09FAF2C0BB1851F9A5316CBDA6F337483D`; temporary `U1GasSnapshotProbe.cs`, SHA-256 `854286273264F9AA66244E96E46C76DC2BDF3CD1709EE62AC1C526E9D3468E6D`. Patch `E02-diagnostic.patch`, complete logs, filters and binary hashes retained externally.
* E02 test DLL SHA-256 `B5978C8CD4A128A6A1924BD67CE3DCED751BA74C6B25FA88F118678174FD54DE`; production DLL `7AE27E5A2A567F32273D0971BF08BFE99526F36BA4A5B5F114E82A57B1194EE6`. Production source is unchanged at `2ab2ad3`; E02 source includes only diagnostic overlays plus documentation through `91695aa`.
* Original visual fixture restored exactly, SHA-256 `AA438CC41FB5E36777392875DE9D6FE7380023808391395AD3F14C55B53A2AA5`.

Reproduce the original with `dotnet test <corresponding Release test.dll> --filter 'FullyQualifiedName~TestSceneBmsMods.TestAutoGauge' --logger 'trx;LogFileName=tests.trx' --results-directory <fresh-directory> -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None`. Headless host setup needs permissions unavailable inside this sandbox; executions outside it are explicit in the retained evidence.

## Action

No production GAS correction for this finding. Later fixture maintenance should choose an explicit lower limit, assert the intended selection rule, or construct inputs that keep Normal at/above threshold when all survival gauges fail. Preserve boundary and HUD-binding assertions. Do not require a fictitious Normal transition, change the profile, or rerun the full suite to make this old wait pass. Fixture maintenance can be deferred; this conclusion does not reopen excluded mod defects.
