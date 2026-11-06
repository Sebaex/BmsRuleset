# C06 — Final certification verdict

## Objective, versions and decision

Evaluate which published LR2 corrections have independent evidence without rewriting working production. C# tested parent HEAD: `d3e6cd707baa7588906c9c669b5dc7d012ec266f`, plus C06's test-only extension from eight to twelve original comparator vectors; production is byte-for-byte unchanged in Git from initial `bae80bc95a2abde15cc1519d0cf784e43cbd9ced`. Historical base `59e186c527bb8e2ab3e8ea82c6668e21b4bf147a` is retained, but broad failures have not all been reproduced against that historical base.

Original Java sources: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, jbms-parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. Reference repositories remained clean and unchanged. Their explicitly compiled classes are the oracle; the auxiliary JAR's complete source version is unknown and its exact SHA is pinned in C02. No original Java graphical player was executed.

**APTO PARA REVISIÓN DE PR, with explicit reservations: broad tests and CI fail, and integral engine parity is not certified.** This permits technical review of the evidence; it is not approval to merge. No PR was opened. Narrow properties below merit **PARIDAD LR2 CERTIFICADA EN EL ALCANCE DECLARADO**, not a 1:1 label for the whole ruleset.

## Certification matrix

| Subsystem | Classification | Method / tests | Limits |
|---|---|---|---|
| Gauge | CERTIFICADO — EJECUCIÓN DIFERENCIAL; CERTIFICADO — DEMOSTRACIÓN MATEMÁTICA | Original `GrooveGauge`/`BMSPlayerRule`; 23 sequences, nine types, six judgements, death/guts/recovery/default TOTAL; C03 | Finite short sequences; float/double histories not universally identical |
| Scoring | CERTIFICADO — EJECUCIÓN DIFERENCIAL; CERTIFICADO — DEMOSTRACIÓN MATEMÁTICA | Original `ScoreData.getExscore`, six count vectors and aggregate identity | Formula only; all event attribution is not differential |
| RANK/DEFEXRANK | CERTIFICADO — EJECUCIÓN DIFERENCIAL | 126 original window rows, exact bounds, C02 | Table values; not every input-offset path or mod combination |
| N3 | Comparator: CERTIFICADO — EJECUCIÓN DIFERENCIAL. Full selection: VALIDADO — PRUEBAS C# / REVISADO — ESTÁTICAMENTE | Twelve original comparisons: 1150, 1150.001 and 1155 ms for four algorithms; inline late predicate separately identified | Original full `JudgeManager.update` not executed |
| Multi-BAD | Collector: CERTIFICADO — EJECUCIÓN DIFERENCIAL. Application: VALIDADO — PRUEBAS C# | Nine original collector vectors plus actual C# consumption, events, second press, combo/endpoints | Primary is a harness input; Java application and subsequent frames not executed |
| Empty POOR | VALIDADO — PRUEBAS C#; REVISADO — ESTÁTICAMENTE | Existing poor-penalty and LR2 judgement cases | No original stateful gameplay execution |
| PMS | VALIDADO — PRUEBAS C#; REVISADO — ESTÁTICAMENTE | Shared LR2 windows and poor-penalty regressions | Full PMS consumers not certified; C01 has failures |
| LN/CN | VALIDADO — PRUEBAS C#; full machine NO CERTIFICADO | Execution, independent tails, scratch/release, misses and engine-end cases; C04 | Complete key/scratch/repress matrix not differential; legacy failures remain |
| HCN | DIFERENCIA CONFIRMADA in temporal update rules; VALIDADO — PRUEBAS C# | Existing signed accumulator/frame-partition demonstration and body execution tests | Not executed in a live Java player; reapply instrumentation remains failing |
| GAS | CERTIFICADO — DEMOSTRACIÓN MATEMÁTICA for bounded BESTCLEAR selection; VALIDADO — PRUEBAS C# | Five policy cases, six order cases and composed state tests | Identical ranges/states/fallback required; other Java policies NO CERTIFICADO |
| Course | VALIDADO — PRUEBAS C# for focused seeds/attribution; full lifecycle NO CERTIFICADO | 21 course parity cases and five selected session cases | 42 C01 inheritance failures remain; original Java course not executed |
| Replay | VALIDADO — PRUEBAS C# | Mixed BAD/CN/recovery/GAS vector, five intermediate checkpoints, actual score/health processors | Shared core, different drivers; no live graphical capture or original Java replay |
| Parser LNMODE | CERTIFICADO — EJECUCIÓN DIFERENCIAL | Original parser: 15 header/player-mode vectors, actual C# decode/convert/replay count | One minimal valid long-note chart, not the whole parser |

The reports C02–C05 identify relevant methods, inputs, outputs, limitations and provenance. The Java callers, captured CSVs, generator and seven new NUnit test methods are committed. Their expected values were not copied from C# output.

## Executed tests and environment

Windows 11 build 26200 x64, .NET SDK 10.0.401 / runtime 10.0.12, Release, osu! source `9a10d935b05a3b896f4bd811d620f2023331db93` matching `OsuVersion=2026.1005.0`. Portable Temurin 21.0.13+9 was used for original-source execution. No dependency version, project workflow or production code was changed.

| Preserved run | Total | Passed | Failed | Skipped | Meaning |
|---|---:|---:|---:|---:|---|
| C01 ordinary | 3914 | 3695 | 216 | 3 | Broad CI-derived filter at initial HEAD |
| C01 instrumentation | 288 | 285 | 3 | 0 | Separate runsettings; not mixed into ordinary tests |
| CERT-C02 | 113 | 110 | 3 | 0 | New resource-loading failure; preserved |
| CERT-C02-02 | 113 | 113 | 0 | 0 | Resource name fixed; algorithm expectations unchanged |
| CERT-C03 | 146 | 146 | 0 | 0 | Gauge oracle included |
| CERT-C03-02 | 147 | 147 | 0 | 0 | Original EX-score execution added |
| CERT-C04 | 50 | 50 | 0 | 0 | Long-note/parser focus |
| CERT-C04-02 | 124 | 124 | 0 | 0 | Adds engine end/order cases |
| CERT-C05 | 38 | 38 | 0 | 0 | Mixed replay and focused course/GAS |
| CERT-C05-02 | 40 | 40 | 0 | 0 | Nullable test annotation verified with gauge cases |
| CERT-C06 | **420** | **420** | **0** | **0** | Union of the final C02/C03/C04/C05 filters, deduplicated |
| CERT-C06-02 | **420** | **420** | **0** | **0** | Same union after adding four Java comparisons at the explicitly requested 1155 ms |

Counts are executions, not unique cases to sum across repeats. The final focused run is not a substitute for C01's broad failed suite. The broad suite was not rerun automatically after test/document-only changes. Both initial and final Release builds succeed. The initial sandbox restore failure and failed resource test run were preserved; no failed output was replaced by a successful rerun. TRX and final binary hashes are recorded in `C06_EVIDENCE.json`; raw logs/TRXs and earlier inventories remain in separate local evidence directories. The final original-source generator reproduced all six CSVs, including the extended N3 comparison; canonical final caller/fixture hashes are in `C06_PROVENANCE.json`. C02's eight-vector evidence remains at its published commit and its report describes that original execution.

Final filtered command uses `dotnet test <Release test DLL> --filter <deduplicated union of C02–C05 filters> --logger "trx;LogFileName=tests.trx" -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None`. Each exact filter is retained with its run. The solution restore/build commands are in C01; the final solution build repeats the Release command with `--no-restore`, `UseLocalOsu=true` and the same external artifact/reference configuration. No benchmark or full unfiltered test suite was run.

Preexisting NU1902/NU1903 dependency advisories remain. The existing nullable-value warning in `BmsHealthDisplay.cs` was not changed. One introduced test annotation warning was removed by enabling the nullable context; no production expectations were relaxed. The historical group of 41 failures retains its earlier classification; C01's larger set does not establish origin at the historical baseline or obsolescence of every expectation.

### GitHub Actions

[Run 42131431022](https://github.com/hinobara-ctrl/BmsRuleset/actions/runs/42131431022), manually dispatched against initial `bae80bc`, completed **failure** on Windows and Ubuntu. Both jobs failed their ordinary regression and judgement-instrumentation steps. No final-head green CI is claimed. REST confirmed the workflow was active; a PR-only connector and a cached REST response explained misleading discovery results, but the historical absence of push-triggered branch runs has no established cause. No workflow was edited to manufacture a pass.

## Answers required for approval

1. **Does it build?** Yes, initial and final Release compilation succeeded.
2. **Does the suite pass?** No: C01 has 216 ordinary and three instrumentation failures. The final 420-case focus passes.
3. **F02/Hazard certified?** Yes for the executed positive-sub-threshold death/recovery vector and the stated survival rules. Not a universal floating-point-state proof.
4. **N3 certified?** Comparator outputs are differential; complete stateful Java selection is not certified. C# boundary/candidate regressions pass.
5. **Multi-BAD matches?** Original collector membership matches nine vectors; C# application/consumption/second-press cases pass. Complete Java event application was not run.
6. **LNMODE certified?** Yes in the 15 executed header/player-mode/count vectors.
7. **Gauge/scoring evidence independent?** Yes: original source execution and algebraic correspondence, with explicit auxiliary-dependency provenance and finite input scope.
8. **Gameplay/replay same states?** The direct core driver and reconstruction agree at the five mixed-CN checkpoints and focused existing cases. All live input/replay situations are not certified.
9. **Which GAS part?** BESTCLEAR maximum-qualified selection for identical configured ranges/states/fallback, plus tested C# chains/seeds/attribution. No all-policy claim.
10. **HCN timing relevant?** Long active-body updates can mathematically change tick counts and gauge; a real hitch is plausible. No live Java/replay reproduction proves its incidence here. C# intentionally catches up and is not changed to imitate frame loss.
11. **Remaining differences?** HCN temporal rule, broad C01 failures, unknown full-engine parity and excluded mod issues below.
12. **Can a PR be opened?** It is apt for review with red CI and these reservations clearly disclosed; this is not merge approval. No PR was opened automatically.
13. **LR2 1:1?** No integral claim. Only the window/comparator/collector functions and finite gauge/EX-score/LNMODE vectors have the differential scope listed above.

## Excluded known modifiers

- **MOD-01:** DP Mirror can interchange 2P scratch and ordinary keys.
- **MOD-02:** Note Random may index beyond available destinations with some mines/long-note/object counts.
- **MOD-03:** Lane Random, Rotation Random and Note Random may mix DP sides.

Their four production files are unchanged. These pending findings are separate from certification results; no repair, expectation change or dependency workaround was introduced for them.

## Publication and integrity

All stages were published sequentially to `origin` (`hinobara-ctrl/BmsRuleset`), branch `fix/lr2-endlessdream-parity-incremental`, each remote SHA verified before proceeding. No branch change, dev update, force push, squash, rebase, merge or PR. Production P1/P2R/P3/P4, excluded mods and existing test expectations were untouched. Changes contain certification docs, Java callers/CSV/provenance, a resource inclusion and seven new NUnit methods; the only follow-up test edit enables nullable annotations.

| Stage | Evidence commit | Result / tests |
|---|---|---|
| C01 | [`8efac785b671adafcefb53e77f8666fc9f75fba1`](https://github.com/hinobara-ctrl/BmsRuleset/commit/8efac785b671adafcefb53e77f8666fc9f75fba1) | Build succeeded; 3914 ordinary + 288 instrumentation, failures recorded |
| C02 | [`feb2727da475f9352a83829fa64648b2deb4cf7e`](https://github.com/hinobara-ctrl/BmsRuleset/commit/feb2727da475f9352a83829fa64648b2deb4cf7e) | 113 passed after preserved resource-loader failure |
| C03 | [`b6fa4b29234c799973486f026cfecb0495298d5d`](https://github.com/hinobara-ctrl/BmsRuleset/commit/b6fa4b29234c799973486f026cfecb0495298d5d) | 147 passed; gauge/EX-score original execution |
| C04 | [`7a5e5613c0349c6c093580ad8a1ad8ea7b81af3c`](https://github.com/hinobara-ctrl/BmsRuleset/commit/7a5e5613c0349c6c093580ad8a1ad8ea7b81af3c) | 124 passed; parser differential, long-note limits |
| C05 | [`d3e6cd707baa7588906c9c669b5dc7d012ec266f`](https://github.com/hinobara-ctrl/BmsRuleset/commit/d3e6cd707baa7588906c9c669b5dc7d012ec266f) | 38 focused, then 40 including gauge annotation validation |
| C06 | [`a332f0cac3cf57bb0d3165d26f025d8cd994dea9`](https://github.com/hinobara-ctrl/BmsRuleset/commit/a332f0cac3cf57bb0d3165d26f025d8cd994dea9) | Final solution build; 420 passed, followed by 420 after four additional Java comparator vectors |

C06's published evidence SHA has been added to the index and this table after remote verification. This final metadata-only commit cannot contain its own SHA; final local/remote HEAD and clean working tree/index are recorded in the external publication receipt and delivery message. There were no production corrective commits in this certification. An externally delivered Release DLL has the same source scope as the evaluated code; no diagnostic binary, private path, JDK or JAR is committed.
