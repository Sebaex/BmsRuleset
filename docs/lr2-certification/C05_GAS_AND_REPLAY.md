# C05 — GAS, course and replay

## Objective and versions

C# evaluated: `7a5e5613c0349c6c093580ad8a1ad8ea7b81af3c`; production unchanged from `bae80bc`. Java reference: `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. Evidence commit appears in the index.

## Representative mixed replay

The new `BmsGasMixedReplayCertificationTest` uses RANK 2, 7K, TOTAL 300, LR2 profiles, GAS with Normal lower limit, a normal note at 1000 ms, CN at 2000–2500 ms and normal note at 3000 ms. Inputs press at 850 ms (BAD), release 851, press the CN lane at 2000, release 2500, press the ordinary lane at 3000, release 3001, finish 3201. The replay records the default Combo algorithm; direct engine processing also uses Combo.

Two routes execute the same production judgement core with different drivers: direct queued inputs and 1 ms frames with recording output wired to real score/health processors, versus `BmsReplaySimulation.Run` with its own event-driven schedule and output adapter. No new replay storage, graphical host or production hook was introduced. At 850/2000/2500/3000/3201 ms, the test compares every gauge's exact health/failed snapshot, active type, result sequence and EX progression. It additionally requires Hazard and ExHard to fail, active Hard after BAD, actual health recovery, four scoring events `[BAD, PGREAT, PGREAT, PGREAT]`, EX 6 and passed final state. Applying score attribution must yield Hard and preserve reconstruction.

The complete replay is simulated once, then its event history is inspected at intermediate times. Truncating it and marking each prefix complete would incorrectly exercise the aborted-chart completion path. Shared core agreement is **VALIDADO — PRUEBAS C#**, not independent Java equivalence and not a live graphical input/replay recording test.

## Existing coverage and precise policy boundary

- `BmsGasCourseParityTest`: 21 cases exercise surviving/failed inherited class seeds, five ordinary gauge mod types, both mod orders, course/non-course contexts, damage/recovery and replay reconstruction including explicit course state snapshots.
- `BmsGasOrderParityTest`: six cases check lower limits and attribution mods cannot alter the effective chain before/after score population.
- `BmsGasPolicyReferenceTest`: five scenarios across the configured lower limits check qualified selection and attribution. Its Java loop is transcribed C#, not an executed original player.
- Five focused existing `BmsCourseSessionTest` cases check final-tier record attribution, omitted failed gauges, regular-chain replacement, state carry and carried-health initialization. `BmsCourseSession.updateGaugeSettings` serializes profile family and surviving class seeds onto the mods used by reconstruction.

Original `BMSPlayer.java:782–797` BESTCLEAR chooses the last qualified gauge while scanning upward within its eligible range. In an equivalent descending C# chain, the first qualified entry is the same maximum by gauge type. The loop invariant is that the current choice is the greatest qualified index seen so far, or the configured bottom fallback if none qualifies. This establishes **CERTIFICADO — DEMOSTRACIÓN MATEMÁTICA** only for selection over identical ranges, states and bottom fallback. C03 independently executes original gauge qualification for its vectors; the entire original `BMSPlayer` GAS branch was not executed.

The earlier F13 commit `585bcaf7086e5d720de54f90c15149f9a9f8ad03` does not certify all Java policies. SELECT_TO_UNDER has a selected-type-dependent upper bound; CONTINUE, NONE and SURVIVAL_TO_GROOVE have different failure actions. Those full policies and Java course lifecycle are **NO CERTIFICADO** here. The C# chain, monotonic failures, display and final attribution are tested only in the stated configured subset.

## Execution, limits and conclusion

Filtered Release run `CERT-C05`: **38 passed, zero failed/skipped** (one new mixed case, 21 course parity, six mod-order, five policy, five selected course-session checks). Filter clauses name these fixtures/cases explicitly; complete command, TRX, logs and hashes are retained externally. A new C03 test-only nullable annotation warning was subsequently removed by enabling its nullable context; no assertion or expected value changed. C06 reruns the combined filters after that annotation change.

Relevant production files: `BmsModGaugeAutoShift`, `BmsModGauge`, `BmsCourseSession`, `BmsHealthProcessor`, `BmsReplaySimulation`. No production modification. Gameplay core/replay snapshots agree for the mixed CN vector and the existing focused course cases. The graphical host, recorded input timings, Java course lifecycle, every GAS policy and all HCN replay histories remain outside differential certification. In particular, the 42 course-inheritance C01 failures remain unresolved; focused success is not a claim that the entire course subsystem passes.
