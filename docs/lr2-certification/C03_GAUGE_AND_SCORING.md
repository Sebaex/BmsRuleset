# C03 — Gauge and EX-score

## Objective and versions

C# evaluated: `feb2727da475f9352a83829fa64648b2deb4cf7e`; production unchanged from initial `bae80bc`. Java: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. Toolchain and auxiliary dependency restrictions are those in C02. Evidence commit is recorded in the index.

## Independent execution

`GaugeOracle.java` invokes original `BMSPlayerRule.validate` and `GrooveGauge.update/setValue/isQualified` with real `BMSModel`, `TimeLine` and notes. It verifies the model note count and code source of the evaluated classes. `ScoreOracle.java` invokes original `ScoreData.getExscore`; the original ScoreData source is explicitly compiled. Expectations are captured Java outputs, not C# results. `Generate.ps1` reproduces all CSVs in a fresh directory and compares text with normalized line endings. C02's initial raw-byte hashes remain an immutable generation snapshot; normal Git line-ending conversion can change raw file hashes without changing those vectors.

23 short gauge sequences include all nine LR2 gauge types (Assist/Easy/Normal/Hard/ExHard/Hazard/Class/ExClass/ExHardClass), the six judgements, subsequent recovery, 31.99% versus 32% guts, exact 2%, 1.99%, Hazard 10.3% minus Empty Poor, short-chart/TOTAL damage and seven default-TOTAL chart sizes. The CSV records the initial state and every update, qualified flag and effective TOTAL. Normal clear at exactly 80% is checked before subsequent damage.

All observed health, death and qualified results match C#. Numeric health permits `2e-7` in normalized 0–1 units: the longest vectors have seven updates, each bounded by float rounding in percentage units; the bound covers their accumulated representation error. **Zero/dead and qualified comparisons use no tolerance.** This is not a proof against every possible float/double boundary crossing in untested long histories.

Six EX-score count vectors include zero, early/late PGREAT, early/late GREAT and mixed counts; outputs agree exactly. C# adds nonzero other-judgement statistics to ensure they do not contribute. This certifies the aggregate formula, not attribution of every possible gameplay event.

## Mathematical correspondence

Relevant implementations: `BmsGaugeCalculator`, `BmsGaugeProfileFactory`, `BmsGaugeProfile.ClampHealth`, `BmsHealthProcessor`, `BmsScoreProcessor`, `BmsExScore`; Java `GaugeProperty`, `GrooveGauge`, `BMSPlayerRule`, `ScoreData`.

- Default TOTAL: `160 + .16 * (N + clamp(N-400, 0, 200))`. C# branch and Java expression are algebraically identical for positive integer N. Tests independently cover 400/401 and 600/601 boundaries.
- TOTAL recovery: Java percentage gain `coefficient * TOTAL/N`; normalized C# gain divides by 100. Damage uses the same maximum of the TOTAL-derived and piecewise note-count factors; existing LR2 parity tests cover the branches, and executed sequences cover composed updates.
- Guts: negative deltas below 32% multiply by 0.6 for LR2 Hard/Class/ExClass. The strict predicate and equality case are separately tested.
- A dead survival gauge cannot recover: Java `setValue` updates only while the old value is positive; C# `ApplyDelta` returns zero when old health is nonpositive. Both clamp surviving values and test the death threshold after applying the delta.
- EX-score: Java `(epg+lpg)*2+egr+lgr` equals C# `2*Perfect+Great` after aggregating early/late counts. BAD/POOR/Empty POOR contribute zero. This identity applies to valid count ranges without integer overflow.

These are **CERTIFICADO — DEMOSTRACIÓN MATEMÁTICA** for the expressions and conditions, not a claim that every downstream consumer supplies identical events or precision.

## Tests and results

Release build and filtered execution:

```text
dotnet test <Release test DLL> --filter "FullyQualifiedName~BmsExecutedJavaGaugeTest|FullyQualifiedName~BmsLr2GaugeParityTest|FullyQualifiedName~BmsGaugeCalculatorTest|FullyQualifiedName~BmsExScoreTest|FullyQualifiedName~BmsScoreProcessorTest" --logger "trx;LogFileName=tests.trx" -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None
```

`CERT-C03`: 146 passed, zero failed/skipped (gauge oracle included). Successor `CERT-C03-02`: **147 passed, zero failed/skipped**, adding original Java EX-score execution. Both runs/logs/TRXs/binary hashes are retained. Source/fixture canonical SHA-256 values are in `C03_PROVENANCE.json`. No production change was made.

## Conclusion and limits

**CERTIFICADO — EJECUCIÓN DIFERENCIAL** for the 23 gauge sequences and six aggregate EX-score vectors. F02/Hazard dies at the positive sub-threshold vector and remains dead after recovery. Default TOTAL and EX-score identities additionally have independent algebraic support. Existing C# tests validate event consumers; complete score attribution, course/UI consumers and every floating-point history are not thereby certified. The C01 failures remain explicit and unmodified.
