# U1-C — F099: Duration selection and collective BAD

## Verdict

**The original expectation is obsolete (H1), but this investigation also demonstrates a separate R1 defect in collective BAD with heterogeneous rates.** Duration's nearest eligible primary selection is correct for this vector. It is not correct to conclude that the entire press operation is correct: an additional head can be consumed as BAD using another head's table. Do not close this group as harmless historical red. Confidence: high for the reproduced normal-note vector; general heterogeneous LN behavior is not certified.

## Original vector and independent expectations

`Normal/Gameplay/Judgement/BmsColumnNoteIndexTest.cs:32–45`, `DurationSearchSkipsCloserHeadsWithNoHitWindow`: layout Bme7K, Rank 3, column 1, press 1030 ms, pending normal heads:

| Head (ms) | Explicit/effective rate | Offset input−head (ms) | Current GOOD radius | BAD radius | Own table | Consumed result |
|---:|---:|---:|---:|---:|---|---|
| 900 | 1 | 130 | 120 | 200 | BAD (`Ok`) | BAD |
| 1000 | .05 | 30 | 8 | 200 | BAD | BAD, primary |
| 1060 | .05 | −30 | 8 | 200 | BAD | BAD |
| 1150 | 1 | −120 | 120 | 200 | GOOD (`Good`) | **BAD, additional** |

Bounds are inclusive, in both directions. `BmsHitObject.EffectiveJudgementRate` (`Beatmaps/Objects/BmsHitObject.cs:20–26`) uses an explicit rate directly, rather than multiplying Rank 3 by it. Provider `lr2Profile`/`scaleLr2Window` gives GOOD 8 at rate .05 and 120 at rate 1; BAD stays 200. The narrow heads are outside GOOD but **inside BAD**, so “no hit window” is false under LR2.

Before upstream `407eebebe26529d99e51c0ea4901bc6a59c43092`, `sevenKeys(rate)` scaled GOOD ±150 and asymmetric BAD [−280,+220]. At rate .05 those bounds were GOOD ±7.5 and BAD [−14,+11]. Both ±30 ms narrow heads really were outside their hit windows. At rate 1 the 1150-ms head was GOOD, 120 ms away versus 130 for 900. This explains the unchanged historical expectation and provides the required heterogeneous inside/outside-window counterexample. LR2 deliberately fixes BAD, so varying only the rate cannot reproduce that particular outside-BAD condition today.

`BmsJudgementSelector.PressSearch.Consider` (`Gameplay/Judgement/BmsJudgementSelector.cs:77–148`) traverses chronologically, excludes judged heads from hit selection, evaluates each pending head's own table, and retains the earlier head on equal Duration distance. Selected sequence: 900 → 1000; 1060 is an equal-distance future candidate, so the search terminates. The 1150 head is farther away. Long heads additionally have late-GOOD invalidation; these normal heads do not. A judged head may supply empty POOR but cannot replace this valid hit. No selector defect is demonstrated by F099.

## Additional defect demonstrated

Minimal reproduction: keep only `(1000,.05)` and `(1150,1)`, press 1030 with Duration. Selected primary is 1000/BAD. **Both heads are consumed as BAD**, although the second head's own result is GOOD.

`BmsColumnJudgementRuntime.HandleNotePress:209–220` calls `applyMultiBad` before applying the primary. In `applyMultiBad:258–274`, one `table` is obtained from `primary.JudgementRate` and reused for every additional note. The 1150-ms head's −120 offset is outside that table's GOOD radius 8, so it enters the BAD set. This bypasses the per-note table invariant used by the selector and the parsed effective-rate data. Real decoded objects can have heterogeneous rates: `BmsParser.collectJudgementRateEvents/applyJudgementRates:1014–1057` and `Beatmaps/BmsBeatmapDecoder.cs:64` preserve them. This is not confined to a fake output recorder or a display assertion. Consequences include premature consumption, extra penalties/combo/gauge effects and no later legitimate hit on that head.

Origin: `56f516dac5a2b3d4e7194ff016f5fd4b6d38d8dc` added this collective application and primary-table reuse. Baseline `59e186c` has no `applyMultiBad`: its original F099 fails with 1150=`None`, whereas current F099 fails with 1150=`Ok`. Static comparison establishes the new route; the isolated baseline execution confirms the different observed failure. Do not confuse these two failures or treat baseline failure as proof that the new consumption already existed.

## Java comparison and limits

Read-only Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`:

* `core/src/bms/player/beatoraja/play/JudgeAlgorithm.java:28–31`: Duration compares absolute distance strictly, pending next heads only; equal distance preserves the earlier selection.
* `JudgeProperty.java:54–63`: LR2 base PG/GR/GD/BAD/empty-POOR windows; BAD remains fixed under rank scaling.
* `JudgeManager.java:1010–1022`: collector excludes heads inside GOOD before applying BAD. It has **one lane table**, not a table per note. The C# heterogeneous-rate extension must evaluate membership coherently with its own per-note tables; Java is not a direct executable oracle for that extension.

Existing [C02](../../C02_JUDGEMENT.md) certifies nine original Java collector-membership vectors, not heterogeneous tables or full Java gameplay. Those valid results remain unchanged. No new Java execution was needed to infer unsupported per-note Java behavior.

## Executions and reproducibility

All runs are short, filtered, Release/net10.0. Production source is the starting HEAD; the current probe build adds only a temporary test fixture, removed after preserving its exact source. No original assertion changed.

* `evidencias-paridad/U1-C-01/build.log`: initial diagnostic compile failed (missing required `IsLongNote` argument); preserved, no test result inferred.
* `evidencias-paridad/U1-C-02/tests.trx`: **12 total, 11 pass, 1 fail**. Filter `FullyQualifiedName~U1DurationProbe|FullyQualifiedName~DurationSearchSkipsCloserHeadsWithNoHitWindow|FullyQualifiedName~BmsMultiBadParityTest`. Two trace probes pass and print the full vector/minimal pair; nine existing MultiBAD cases pass; original F099 still fails. TRX SHA-256 `503FC65950EE1878D1670C7BEE8C0434B488E4D22E954E42EBC44A643A3D04CA`.
* Baseline `reconciliacion-c01-20261105/u1-baseline-duration-20261107/tests.trx`: existing F099 only, **0/1 pass**, expected GOOD / actual None. TRX SHA-256 `B3E69E928B052DD68DB088E4A18442D22831E0DBD681CAB2BBE5F793D250980F`.
* Exact temporary probe: `investigacion-u1-20261107/U1DurationProbe.cs`, SHA-256 `1FF4A75581BCFF782DA95F354B285264D2A531B187DD07467F3D581C46454A84`. It builds the notes above, calls public `SelectPress`, then real `BmsJudgementEngine.GetColumn(1).HandleNotePress`, and prints each table, result and consumed state. Recorder `GetMainResult` observes actual runtime application. The primary assertions (1000/BAD) derive from fixed LR2 bounds and strict distance; erroneous additional results are observations, not new expectations blessed as correct.
* Current diagnostic test DLL SHA-256 `89D09F5B5FE627A02DD05BF8E3EDD15FC32AC534EA5EF1F881F95B2702A9A30C`; production DLL `52D45EA0FB973B1CC3A766EEF6975FFACED8865E42A8C6916AFA0A413E1FB3E1`. Per-run `hashes.json`, `filter.txt`, build/test logs and baseline hashes are retained alongside each TRX.

Reproduce the original on either corresponding Release test DLL:

```powershell
dotnet test <test.dll> --filter 'FullyQualifiedName~DurationSearchSkipsCloserHeadsWithNoHitWindow' --logger 'trx;LogFileName=tests.trx' --results-directory <fresh-directory> -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None
```

## Proposed later correction, not implemented

In `applyMultiBad`, retain the primary's own BAD classification, ordering, leading-LN exclusion and judged-head rules, but evaluate each additional head against **its own** effective table. Fixed LR2 BAD bounds permit retaining the bounded scan. Verify the minimal pair leaves 1150 pending, later press can hit it, uniform-rate Java vectors remain identical, heterogeneous directions/rank boundaries and LN heads obey their own eligibility. Review combo, gauge, replay and additional-result order. No changes to Duration comparison or original goldens are justified by this evidence. Severity: gameplay-relevant; recommend a focused corrective review before merge. No production fix is authorized or applied here.
