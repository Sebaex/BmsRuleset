# C02 — Judgement: executed reference components and limits

## Objective and versions

Evaluate C# `8efac785b671adafcefb53e77f8666fc9f75fba1` (production unchanged from `bae80bc`), Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. Evidence commit is recorded in the index after publication.

## Method, inputs and observations

The small Java callers and CSV outputs are in `osu.Game.Rulesets.BmsRuleset.Tests/reference_fixtures/lr2-certification`. `Generate.ps1` compiles the original six play classes and six model classes, before the dependency JAR in the classpath; it invokes the original methods, not translated C# expectations. Use a fresh output directory. JDK: Temurin 21.0.13+9, Windows x64, archive SHA-256 `A4DBAA05FCA4BD89D84584E58C459BD732EFEBB1722AF80AB13C6516832004F9`.

Auxiliary types come from a locally available `beatorajanew.jar`, SHA-256 `38101C00CB6918FEBB9E0671558AD9F11D1DB842A699AF1EE80CA5BA217BB249`; its complete source version is not established. Its algorithms are not the oracle: `JudgeManager`, `JudgeProperty`, `JudgeAlgorithm`, gauges and the explicitly compiled model classes come from the fixed source commits. The collector caller checks class code-source identity. Reproduction requires this dependency binary or separately verified compatible auxiliary dependencies; no JAR/JDK is distributed in Git.

| Property | Reference execution and C# check | Classification |
|---|---|---|
| Windows | Original `JudgeProperty.LR2.getJudge`: 126 rows, RANK 0–4 and DEFEXRANK 60/200, key/scratch and head/tail. Exact signed microseconds compared with C# millisecond windows across every supported layout. | CERTIFICADO — EJECUCIÓN DIFERENCIAL, window values only |
| N3 comparator | Original `JudgeAlgorithm.compare`, normal 1000 ms / long head 1050 ms, press 1150 and 1150.001 ms, all four algorithms: eight outputs. | CERTIFICADO — EJECUCIÓN DIFERENCIAL, comparator only |
| N3 full selection | The caller separately evaluates the inline late-head predicate; C# checks the resulting selected candidate and Good/Bad/None. Existing complementary tests include 1155 ms. The predicate is explicitly transcribed, not an invocation of `JudgeManager.update`. | REVISADO — ESTÁTICAMENTE plus VALIDADO — PRUEBAS C#; full Java selection not executed |
| Multi-BAD collector | Original private `MultiBadCollector.add/filter`, nine rows: early/late BAD, better Good primary, equal times, long primary/additional head and leading-long-prefix order. C# runtime checks event order, Bad extras and consumed/remaining heads. | CERTIFICADO — EJECUCIÓN DIFERENCIAL for collector membership; subsequent Java application not executed |
| Subsequent press, combo, gauge, endpoints | Existing `BmsMultiBadParityTest`, `BmsLr2PoorPenaltyTest`, complementary and LR2 judgement tests exercise consumption, second press, score and long endpoints. | VALIDADO — PRUEBAS C# |
| Empty POOR / PMS / 5K / 7K | Existing LR2 poor-penalty and judgement regressions cover the early empty press and subsequent Poor, combo preservation and Bad consumption. No stateful original Java gameplay was run. | VALIDADO — PRUEBAS C# and REVISADO — ESTÁTICAMENTE |

`CollectorOracle` allocates the enclosing manager without its graphical constructor, invokes its real inner constructor and original filter by reflection. That filter does not read the uninitialized enclosing player. This is sufficient to execute collection/filtering, not a simulation of the full player, automatic misses, score, holds or later frames. The primary index is a harness input, not a Java-selected output.

## Tests and evidence

Filtered Release command (same local osu source/build setup as C01):

```text
dotnet test <Release test DLL> --filter "FullyQualifiedName~BmsExecutedJavaJudgementTest|FullyQualifiedName~BmsLr2ComplementaryJudgementTest|FullyQualifiedName~BmsMultiBadParityTest|FullyQualifiedName~BmsLr2PoorPenaltyTest|FullyQualifiedName~BmsLr2JudgementParityTest|FullyQualifiedName~BmsJudgementSelectorTest" --logger "trx;LogFileName=tests.trx" -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None
```

First run `CERT-C02`: 110 passed, three new tests failed because MSBuild normalizes the resource directory hyphen to an underscore. The loader was corrected; production and expected algorithm results were unchanged. Preserved successor `CERT-C02-02`: **113 passed, zero failed/skipped**. Three new NUnit methods traverse 126 window rows, eight comparator rows and nine collector rows. Original failed TRX/logs remain in external evidence alongside successor binary hashes. CSV/source hashes are in `PROVENANCE.json`.

Relevant production: `BmsJudgementSelector.SelectPress`, `BmsColumnJudgementRuntime.HandleNotePress`, profile provider and Multi-BAD application. No production change was necessary for the tested vectors. The 216 ordinary and three instrumentation C01 failures remain; this filtered success does not supersede them.

## Conclusion

Window values, comparator results and collector membership agree in the executed vectors. Complete Java/C# judgement state-machine equivalence, including combo and second press, is **NO CERTIFICADO**. Internal C# assertions and a transcribed inline guard are not promoted to a full differential engine certification.
