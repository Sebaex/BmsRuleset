# U1-B — F059: mixed-chart star rating

**H1, obsolete golden demonstrated.** The change is an indirect, intentional consequence of adopting LR2 judgement windows, not a new star-rating formula or an assertion that this C# difficulty model exists in Java. No difficulty defect is demonstrated by this vector. Confidence: high.

## Reproduction and causal isolation

`Normal/Difficulty/BmsDifficultyPerformanceRegressionTest.cs:24–29,135–149` creates 20,000 objects in eight columns at `(i / 3) * 37.25` ms (integer division), every seventeenth a 210-ms LN; Rank 2, clock rate 1. It converts column/head/tail into `BmsNoteTiming` and calls `BmsStarRatingProcessor.ComputeStarRating(notes, 8, 2)`. Golden `10.748954856597857` was introduced by `a0322522f9feb95fadff0e719f5c9e8cd5e6f8ae`.

`Gameplay/Difficulty/BmsStarRatingProcessor.cs:210–265,619–628` preprocesses floored timings and computes several density/pattern/hold components with `HitLeniencyX` affecting their weights. `BmsHitLeniency.cs:11–27` gets the non-scratch GREAT window from the judgement provider:

```text
x0 = 0.3 * sqrt(GREAT_ms / 500)
HitLeniencyX = min(x0, 0.6 * (x0 - 0.09) + 0.09)
```

Before upstream `407eebebe26529d99e51c0ea4901bc6a59c43092`, 7K Rank 2 GREAT was `60 * .75 = 45` ms. LR2 Rank 2 GREAT is 40 ms (`BmsJudgementProfileProvider.lr2Profile`). The processor and leniency helper have **no diff between baseline `59e186c` and starting `2ab2ad3`**; the upstream profile change predates that baseline.

| Same 20,000 timings and processor | GREAT | HitLeniencyX | Star rating |
|---|---:|---:|---:|
| Current LR2 | 40 | 0.0848528137423857 | 10.98694842655334 |
| Historical GREAT-only diagnostic override | 45 | 0.09 | **10.748954856597857** |

The diagnostic uses existing thread-local `UseWindowMods` in a nonparallel test, replaces only the GREAT row by ±45, recomputes the same notes, asserts the untouched historical golden within its original `1e-12`, then disposes the scope. No production field, formula, note, dependency or expected value was changed. Exact recovery of the golden after controlling this single input demonstrates the cause; it is stronger than observing that the baseline also fails. This does not establish desired balance for every difficulty chart or a general Java/C# rating equivalence.

## Evidence

All paths below are relative to the external evidence root defined in the index; Release/.NET SDK 10.0.401. Production source remains starting `2ab2ad3`; published investigation commits contain documentation only.

* `evidencias-paridad/U1-AB-01/tests.trx`: filter `FullyQualifiedName~U1LifetimeProbe|FullyQualifiedName~U1DifficultyProbe|FullyQualifiedName~MixedChartKeepsOriginalStarRating|FullyQualifiedName~RealChartLifetimeRefreshKeepsStatelessSnapshotResults`; six cases: **3 pass, 3 fail**. Difficulty diagnostic passes; unchanged original F059 fails with current value `10.98694842655334`. Other cases belong to U1-A and are not difficulty regressions. TRX SHA-256 `04F8202BB272A1132C7EC483F4EAA7C23EFCABCF38FE69E7BBB0171B7263EE2F`.
* `investigacion-u1-20261107/U1DifficultyProbe.cs`, exact temporary diagnostic source SHA-256 `8CEE296FF03CE972460D30F689A5B0841E812034E40749C666FD097026C441A9`; archived outside Git, removed from the source tree after execution.
* Diagnostic test DLL `214EEA0D85950988132D1AAA7092383FC3E068D8E5DD808EE580A925FB27176A`; production DLL `F657A06DE3C9A4FB0D760F2843DC0A8E7F31982045A44C65AB3A00E314C02DB2`. Full build/test logs and hashes retained with the TRX.
* Existing isolated baseline binary: `investigacion-u1-20261107/B-baseline/tests.trx`, F059 only, **0/1 pass**, same current rating. TRX SHA-256 `5D4F45E76D69A3389044F3C68C642FB66D5B36A13825D1E0E465CEC336372172`. Original C01/R5 failures remain untouched.

Original-case reproduction, under five minutes:

```powershell
dotnet test <corresponding Release test.dll> --filter 'FullyQualifiedName~MixedChartKeepsOriginalStarRating' --logger 'trx;LogFileName=tests.trx' --results-directory <fresh-directory> -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None
```

## Action

Document now; no production correction. A later authorized fixture-maintenance phase can replace this old-policy golden with an independently justified LR2 expectation and retain the input/leniency invariant. This phase does not replace it, suppress the failure, or rerun the full suite. No new merge blocker arises from F059 itself; existing non-green CI still needs the separate project decision recorded by R5.
