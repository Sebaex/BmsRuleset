# U1-A — F001/F002/F008/F009: lifetime fingerprints

**H1 for all four observations: historical judgement-window expectations demonstrated.** The old arrays were not retained, but both exact stored hashes can now be recovered by reconstructing the old policy. This supplies causal evidence beyond the earlier baseline/current hash agreement. No new lifetime/lookup defect is demonstrated by these charts. Confidence: high within these sampled sequences; no blanket rendering certification.

## Functions and controlled variables

`Normal/BeatmapTest/Decoding/BmsTimingMapQueryTest.cs:38–118` decodes each chart with fixed branch choice 1, selects 64 evenly distributed non-mine notes, refreshes them at scroll speeds 8/12/6 and hashes 192 little-endian `(double Start,double End)` pairs. F001/F002 are the instrumented stateless-point comparison; F008/F009 are the ordinary fingerprints.

`UI/Gameplay/BmsHitObjectLifetimeEntry.cs:107–140,212–245,368–381` determines the interval from scroll visibility, minimum future lifetime 100 ms, head fast BAD/empty-POOR bounds, head/tail slow BAD, past lifetime 0, passive margin 100 ms and visual offset. Scroll speed, timing map (BPM/STOP/SCROLL), playback/range scale, note type/times/column/rate and a supplied current time can affect it. Here visual offset is 0, no current-time clamp, default playback/range scale and no active window mods. Invisible-note/mine special lifetimes are not exercised by this selected sequence.

For these notes, current `LifetimeEnd = note.GetEndTime() + (200 + 100)`. The probe asserts this **independent arithmetic invariant for every sampled pair**. Future lifetime is the maximum of the visibility interval, the 100-ms minimum and fast input coverage (currently empty POOR 1000 ms). This prevents culling an upcoming note while it can receive empty POOR.

Representative observed values (ms; decimal points normalized from local comma-formatted logs):

| Chart / speed / sample | Object head → end | Current lifetime | Historical end |
|---|---|---|---:|
| caution / 8 / 0 | 8362.135171751717 → same | −3122.8648282482827 → 8662.135171751717 | 8742.135171751717 |
| caution / 8 / 32, LN | 62677.89205916342 → 62712.37502252345 | 0 → 63012.37502252345 | 63092.37502252345 |
| scrollplus / 8 / 0 | 1862.0689655172416 → same | 113.63146551724162 → 2162.068965517242 | 2242.068965517242 |
| scrollplus / 8 / 32 | 55241.977235400016 → same | 54241.977235400016 → 55541.977235400016 | 55621.977235400016 |
| scrollplus / 12 / 0 | 1862.0689655172416 → same | 592.5377155172416 → 2162.068965517242 | 2242.068965517242 |

The scrollplus sample-32 start equals head−1000, demonstrating the input-window floor rather than an arbitrary snapshot difference. The first sample's different starts at different speeds demonstrate visibility dependence. Current end arithmetic is correct for the current tables.

## Recovery of the historical goldens

`2e93d31c76e312a76a9960e8902889253475ce32` set the two current stored goldens. Its profile provider still used the old 7K policy: BAD slow 280 normal / 290 scratch, rate-scaled in integral microseconds, and fast empty POOR 500 ms. Rank 3 rate is 1; scrollplus `DEFEXRANK 134` uses the historical integer calculation `truncate(truncate(134)*75/100)/100 = 1`, **not** 1.005. The modern EXRANK identity marker slightly above 1 does not change the fixed current BAD bound.

The final probe reuses only unchanged scroll visibility (`findEarliestVisibleWindowStart`) and reconstructs:

```text
oldStart = head - max(500, head - earliestVisibleTime)
oldEnd = end + (truncate(oldBadSlow * 1000 * oldRate)/1000 + 100)
oldBadSlow = scratch ? 290 : 280
```

It preserves floating-point operation grouping from the old implementation. Both reconstructed fingerprints match the untouched original constants exactly:

| Chart | Current | Reconstructed old / stored golden |
|---|---|---|
| caution | `96F06BC3CA3385E245C4A4A4A945E36845B9DAE2640336A894DF7BEE1BD2DF39` | `FB282145C2C0AC8E90BBC4C4FFC9F2C9CB2E818D81A0E1EBCBE8342043D0FFFD` |
| scrollplus | `5929033CF48AAF4CAA224FF09A27CA029E1322AE70E43E72DE9BF39737074BB7` | `13D57E0EB00D932CC736B71494A633B0BB68707A8380B0616B1CADB3F10C570C` |

Upstream `407eebebe26529d99e51c0ea4901bc6a59c43092` changed those profile bounds to LR2 before baseline `59e186c`. Between that baseline and investigation starting HEAD `2ab2ad3`, there is no diff in the lifetime entry, scroll controller, timing map or these two chart resources. Earlier isolated baseline executions and current ordinary/instrumented runs produce identical current hashes. Neither unchanged source alone nor matching hashes alone was used as proof of correctness: the arithmetic and exact old-policy reconstruction establish the explanation.

## Evidence and falsified partial explanations

External paths are relative to the index's evidence root. Release, SDK 10.0.401; no production edit or golden replacement.

* `evidencias-paridad/U1-AB-01/tests.trx` (six cases, 3 pass / 3 fail, SHA-256 `04F8202BB272A1132C7EC483F4EAA7C23EFCABCF38FE69E7BBB0171B7263EE2F`): current end invariants pass; an **incomplete** end-only counterfactual recovers caution but not scrollplus. Its initial 1.005 assumption is superseded by source inspection, not silently treated as evidence for H1.
* `evidencias-paridad/U1-A-02/tests.trx` (four cases, 1 pass / 3 fail, SHA-256 `1C4B0EDA2C333C0779A68EB152EAA29C0EA4A94A37789442BD3629424421C4FE`): integer EXRANK alone still fails scrollplus's old-hash assertion; old fast coverage must be reconstructed too. Original two ordinary golden failures remain. Probe archived as `investigacion-u1-20261107/U1LifetimeProbe-A02.cs`.
* `evidencias-paridad/U1-A-03/tests.trx`: filter `FullyQualifiedName~U1LifetimeProbe`, **2/2 pass**, including exact old-hash assertions for both charts and all current end invariants. SHA-256 `28C86A48AA89DC241C5D38370765E3B88538C3A43954C759D9B748AB8A761093`.
* Exact final temporary fixture `investigacion-u1-20261107/U1LifetimeProbe-A03.cs`, SHA-256 `A245151D9FEA4C762869403E89607249E86B2987EB0AC7767E475DC252E47B28`. It invokes the existing private chart/refresh/hash helpers, independently reconstructs old endpoints, and reflects the existing visibility search; it does not intercept or alter production. It is an external archived diagnostic, not permanent test code.
* `investigacion-u1-20261107/A-instrumentation/tests.trx`: unchanged instrumented originals, filter `FullyQualifiedName~RealChartLifetimeRefreshKeepsIndependentStatelessResults`, existing `judgement-instrumentation.runsettings` (`COMPlus_JitNoInline=1`), **0/2 pass**, both fail only at old hash after exact stateless-reference equality. Caution 384 queries; scrollplus 247134. SHA-256 `A279DF78B394A0EBEE692208D29298C3F43425426539F513E6000DBFD62D4D1C`.
* A03/instrumentation test DLL SHA-256 `062C8F484704C8F874FBA68130B30525AA682B071FAA25BA8223D75FF7234E94`; production DLL `F657A06DE3C9A4FB0D760F2843DC0A8E7F31982045A44C65AB3A00E314C02DB2`. This binary contains the archived A/B diagnostic overlays over unchanged production (docs-only HEAD `acc1f15`). Logs, earlier failed attempts and per-run hashes remain intact. Prior baseline instrumental evidence remains in `reconciliacion-c01-20261105/baseline-instrumentation/tests.trx`, as indexed by R2/R5.

No full suite, new reference edits or new performance benchmark. Reproduce ordinary cases with `dotnet test <Release test.dll> --filter 'FullyQualifiedName~RealChartLifetimeRefreshKeepsStatelessSnapshotResults'`; use the filter/settings above for instrumented cases and a fresh results directory for every run.

## Action and limits

No production correction is warranted for these four observations. Later fixture maintenance may preserve independent LR2 endpoint/visibility invariants and update obsolete policy snapshots after review; **none are changed here**. The old full arrays remain absent as historical artifacts, but their exact fingerprints are reproduced under the documented original bounds, so their cause is no longer undetermined. Finite sampled charts and a stateless map oracle do not prove correctness of every scroll pattern, lifetime replay state or mine/invisible lifecycle.
