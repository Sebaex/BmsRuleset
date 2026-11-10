# C01 — Baseline build and regression tests

## Objective and versions

Establish what actually passes before adding certification evidence. Evaluated C# HEAD: `bae80bc95a2abde15cc1519d0cf784e43cbd9ced`. Reference Java: `d4995153b0a759859ebc8f859dd868abae39db5a`; parser: `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. No production or test source was changed for this baseline.

The active branch and remote were `fix/lr2-endlessdream-parity-incremental` at that exact HEAD. Working tree and index were clean, with no untracked files. Local environment: Windows 11 build 26200 x64, SDK 10.0.401, runtime 10.0.12. The matching osu! source was `9a10d935b05a3b896f4bd811d620f2023331db93` (`OsuVersion=2026.1005.0`). Reference checkouts were read-only.

## Method and commands

The solution and filters come from `.github/workflows/ci.yml`. Instrumentation uses `osu.Game.Rulesets.BmsRuleset.Tests/judgement-instrumentation.runsettings`, including `COMPlus_JitNoInline=1`. No benchmarks or unfiltered suite were run.

Run from the repository with `OSU_SOURCE`, `ARTIFACTS`, `RESULTS` and `NATIVE_HELPER` set to separate local paths. The native helper is the existing project's previously built executable assembly. External artifacts and the helper avoid writes to reference trees. These are the complete command forms used locally:

```powershell
dotnet restore osu.Game.Rulesets.BmsRuleset.sln -p:UseLocalOsu=true "-p:OsuSourcePath=$OSU_SOURCE" --artifacts-path "$ARTIFACTS"
dotnet build osu.Game.Rulesets.BmsRuleset.sln -c Release --no-restore -p:UseLocalOsu=true "-p:OsuSourcePath=$OSU_SOURCE" --artifacts-path "$ARTIFACTS" "-p:BmsNativeBrotliToolPath=$NATIVE_HELPER" -p:UseSharedCompilation=false -m:1 -nr:false
$dll = "$ARTIFACTS/bin/osu.Game.Rulesets.BmsRuleset.Tests/release/osu.Game.Rulesets.BmsRuleset.Tests.dll"
dotnet test $dll --filter 'FullyQualifiedName!~.Performance.&FullyQualifiedName!~Benchmark&TestCategory!=Performance&TestCategory!=Benchmark&TestCategory!=JudgementInstrumentation' --logger 'trx;LogFileName=regression.trx' --results-directory "$RESULTS/regression" --blame-hang-timeout 5m -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None
dotnet test $dll --settings osu.Game.Rulesets.BmsRuleset.Tests/judgement-instrumentation.runsettings --filter 'FullyQualifiedName!~.Performance.&FullyQualifiedName!~Benchmark&TestCategory!=Performance&TestCategory!=Benchmark&TestCategory=JudgementInstrumentation' --logger 'trx;LogFileName=instrumentation.trx' --results-directory "$RESULTS/instrumentation" --blame-hang-timeout 5m -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None
```

The workflow builds into its usual project directories. With external artifacts, `BmsEmbeddedSongDecoderTest.TestSongsRoot` instead resolves below the artifact root. The tracked `bms_test_songs` inputs were copied there without changing tests or source inputs; previously present copies were hash-checked. The tests' own configuration was retained.

## Results

- Restore and solution Release build succeeded, with zero compilation errors.
- Ordinary regression filter: **3914 total, 3695 passed, 216 failed, 3 skipped**, approximately 6m31s.
- Separate instrumentation filter: **288 total, 285 passed, 3 failed, 0 skipped**, approximately 2m26s. Failures: `HcnReapplyRetainsBodyAccumulatorAndPendingRelease` and the two `RealChartLifetimeRefreshKeepsIndependentStatelessResults` fingerprints for `99_outlaw_caution.bms` and `99_outlaw_scrollplus.bms`.
- Existing NU1902/NU1903 dependency advisories and the nullable warning in `BmsHealthDisplay.cs` remain. Dependencies and that file were unchanged.
- The sandbox could not complete the first restore and denied an SDK service-manager query. The separate unsandboxed run completed restore/build. Both attempts were retained outside the repository.

The failure inventory is `C01_FAILURES.csv`. Every listed failure existed at this phase's initial HEAD. That establishes its provenance relative to this certification phase; it does not establish whether it existed in historical baseline `59e186c`, whether it was introduced by an earlier parity commit, or whether its expectation is wrong.

The largest groups include course health inheritance (42), visual long-note tests (18), hit-error meter (16), health processor (12), song progress (9), and long-note lifecycle/rule/window tests. The historical group of 41 failures is not reclassified or silently replaced by this larger inventory. Individual root causes remain pending unless a later certification stage supplies evidence.

## GitHub Actions

The workflow was active. The branch initially had zero listed runs. An earlier connector operation only lists PR-triggered runs; an unauthenticated REST response was also observed to be cached. Neither is proof that Actions is disabled.

A manual dispatch was accepted using the existing Git credential after `gh` dispatch returned HTTP 401. No token was printed or stored, and no workflow configuration was changed. [Run 42131431022](https://github.com/hinobara-ctrl/BmsRuleset/actions/runs/42131431022) evaluates the initial HEAD on Windows and Ubuntu. Its final status is recorded in C06; no CI success is claimed here while it is running. The historical absence of push-triggered runs is not assigned an unverified cause.

## Conclusion and limits

**The ruleset builds, but the broad local regression suite does not pass.** The earlier 518-case focused success did not establish that the CI filter was green. C01 is an executed baseline, not a parity certification. Later stages may certify narrower properties while these failures remain explicit. No Linux result, end-to-end Java equivalence, or universal absence of regressions follows from this local run.
