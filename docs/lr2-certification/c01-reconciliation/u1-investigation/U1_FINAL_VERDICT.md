# C01-U1 — Final independent verdict

2026-11-07. Investigation completed for exactly the ten original U1 observations. **Seven primary H1, three primary E1, none remaining U1. One additional gameplay R1 is demonstrated under mixed F099 and remains uncorrected.** This mixed result must not be summarized as ten harmless failures or zero regressions.

## Case decisions

| ID | Initial | Final primary / secondary | Demonstrated cause | Evidence | Action |
|---|---|---|---|---|---|
| F001 | U1 | H1 | Caution golden exactly recovered with old lifetime windows | [A](U1_A_LIFETIME.md) | Later fixture maintenance |
| F002 | U1 | H1 | Scrollplus golden exactly recovered with old fast/slow coverage | [A](U1_A_LIFETIME.md) | Later fixture maintenance |
| F008 | U1 | H1 | Same caution golden, ordinary test | [A](U1_A_LIFETIME.md) | Later fixture maintenance |
| F009 | U1 | H1 | Same scrollplus golden, ordinary test | [A](U1_A_LIFETIME.md) | Later fixture maintenance |
| F059 | U1 | H1 | GREAT 45→40 ms changes leniency; 45 exactly recovers old rating | [B](U1_B_DIFFICULTY.md) | No production rating change |
| F099 | U1 | H1 / **R1** | Old narrow-head exclusion is obsolete; new collective BAD wrongly uses primary table for other heads | [C](U1_C_DURATION.md) | **Separate corrective review before merge** |
| F178 | U1 | E1 | 1024×600 helper compares comma-decimal expectation with invariant `ToString()` | [D](U1_D_COURSE_RESULTS.md) | Fix fixture culture contract later |
| F179 | U1 | E1 | Same condition, 1280×720 | [D](U1_D_COURSE_RESULTS.md) | Same |
| F180 | U1 | E1 | Same condition, 1600×900 | [D](U1_D_COURSE_RESULTS.md) | Same |
| F196 | U1 | H1 / E1 | Old sequential Normal expectation conflicts with threshold display; Normal is below .8 when Hard fails | [E](U1_E_GAS_VISUAL.md) | Explicit/configured fixture vector later |

The classification CSV is a successor limited to these ten rows. Original R1/R5 CSVs, the 148 earlier H1 observations, original reports, failed TRXs, goldens, mod exclusions and expectations remain intact. No unrelated case is reclassified. H1, preexisting and correct are not inferred from one another: each group report supplies a separate functional/policy argument.

## Answers for review

1. **F099:** Duration's selected primary (1000 ms at input 1030) follows LR2 fixed BAD and strict nearest-distance rules. A separate **current functional defect** occurs after selection: with heads `(1000,.05)` and `(1150,1)`, the second has own GOOD ±120 yet is consumed as BAD using the first's GOOD ±8. `56f516dac5a2b3d4e7194ff016f5fd4b6d38d8dc` introduced that route. Baseline original F099 fails with the second head None; current fails with Ok. Recommend evaluating additional heads with their own effective tables while retaining ordering, primary classification and LN exclusions. No engine correction was made.
2. **Lifetimes:** exact old fingerprints now reconstructed, not merely dismissed as bad hashes. Current representative endpoints meet LR2 bounds; stateless/optimized searches agree. No new sampled lifetime defect demonstrated. Original full historical arrays are still absent; the successful reconstruction supplies their policy explanation without pretending those artifacts existed.
3. **Difficulty:** formula unchanged in the examined baseline/current range. Upstream LR2 GREAT shrank 45→40, changing `HitLeniencyX` .09→.0848528137423857 and rating 10.748954856597857→10.98694842655334. Controlled historical GREAT-only override recovers the golden. Intentional policy consequence, no new rating formula.
4. **Course:** all three timeouts share one demonstrated fixture cause. Numeric aggregate, combo, PGREAT, FAST/SLOW and controls are present/correct at the blocked check; only `77,00%` versus `77.00%` fails. No initialization/scheduler/backend repair justified. Subsequent selection/restoration steps were not reached and remain outside this proof.
5. **GAS:** at Hard failure Normal=.7915079365079369<.8, Easy=.904≥.8. Easy is the correct current display/history choice. Boundary controls select Normal at/above .8. The loaded HUD has one health display whose bound profile agrees at all observed transitions. No pixel/skin-wide certification or requirement for every chain tier to appear.
6. **Corrective priority:** heterogeneous collective BAD is gameplay-relevant and should receive a narrow engine/replay/gauge regression review before merge. Proposed tests and production location are in C. No broad transaction system, new testing infrastructure or profile changes are required.
7. **Deferrable maintenance:** old goldens, F099's historical assertion, Course culture comparison and GAS fixture setup can enter a dedicated fixture-maintenance phase. Do not silently suppress them or claim CI green. No new H2/V1 cause is established within these ten observations.

Four explanatory causes cover these families: upstream LR2 window-policy change (A/B/C historical expectations), threshold-based GAS display policy (E), culture-inconsistent Course comparison (D), and the newly demonstrated heterogeneous collector regression (C). They are not ten independent bugs. This newly bounded counterexample updates knowledge after R5's then-zero demonstrated regression finding; it does not rewrite R5 or invalidate C02's uniform-table Java vectors.

## Executions, failures and provenance

All executions were filtered and anticipated below five minutes, with SDK 10.0.401, Release/net10.0. Each completed within that limit; no full suite, benchmark or CI dispatch. Builds succeeded except the first diagnostic compile (missing required constructor argument), which remains retained as `evidencias-paridad/U1-C-01/build.log`. Existing NuGet advisories/nullable warning were observed; dependencies and warnings were not repaired or reclassified by this phase.

| Run | Total | Pass | Fail | Meaning |
|---|---:|---:|---:|---|
| U1-C-02 | 12 | 11 | 1 | Two trace probes + nine existing MultiBAD pass; unchanged F099 fails |
| C baseline | 1 | 0 | 1 | Original F099, different actual result None |
| U1-AB-01 | 6 | 3 | 3 | Diagnostic A/B pass; unchanged ordinary goldens/rating fail |
| U1-A-02 | 4 | 1 | 3 | One incomplete scrollplus hypothesis + two original golden failures |
| U1-A-03 | 2 | 2 | 0 | Exact old-policy hash reconstruction and current end invariants |
| A instrumentation | 2 | 0 | 2 | Exact independent equality passes before old-hash failure |
| B baseline | 1 | 0 | 1 | Same current difficulty value at baseline |
| U1-D-01 | 3 | 0 | 3 | Host setup access denial, not product timeouts |
| D-02 | 3 | 0 | 3 | All three actual aggregate text-comparison timeouts |
| D-03-en-US | 3 | 0 | 3 | NUnit culture attempt did not change host thread culture |
| U1-E-01 | 1 | 0 | 1 | Original GAS wait with observed transition snapshots |
| E baseline | 1 | 0 | 1 | Same original GAS wait fails |
| U1-E-02 | 4 | 3 | 1 | Three independent threshold controls pass; original GAS wait fails, HUD agrees |

These are **43 result entries across repeated/diagnostic runs: 20 pass, 23 fail**, not 43 unique cases or 23 production bugs. No omissions. Failed attempts were not overwritten by retries. [U1_EVIDENCE.json](U1_EVIDENCE.json) hashes every retained TRX/log/per-run hash manifest; group reports identify exact temporary source overlays, binary hashes, filters, source SHAs and external evidence-root-relative paths. Temporary probes were removed and both instrumented visual fixtures restored byte-for-byte. No diagnostic DLL is delivered as a new gameplay version.

Pending: no long run is necessary to substantiate these bounded diagnoses. A future **authorized correction** needs mixed-rate BAD regressions and relevant engine/replay checks; later fixture maintenance needs controlled host-locale coverage and the Course steps currently blocked by the original assertion. Original 420/427 protected subsets and full Windows/Ubuntu CI were not rerun merely for docs. Previous non-green CI remains evidence, not a new passing certification. Cross-platform reproduction of this new mixed-rate case and later Course interactions were not performed.

## Publication and integrity

Origin verified `https://github.com/hinobara-ctrl/BmsRuleset.git`; branch unchanged `fix/lr2-endlessdream-parity-incremental`. Every independent group was committed, pushed normally and its remote SHA verified before the next completed publication:

| Group | Published/verified SHA |
|---|---|
| C | `acc1f15d1a111f76b1b8bf5fba7fcc6bfd515da2` |
| B | `f0ad4ae7a92f508dc0e8274c2541e4cdcee02fdb` |
| A | `0f4a4501edf975c05802a11e9437cad504534905` |
| D | `91695aad21181b6338009fab9390bc0bd1868034` |
| E | `27bf826af972fa0b22ed934ba72fc0d6cf48ed0e` |

At final-report drafting, **local and remote HEAD both `27bf826af972fa0b22ed934ba72fc0d6cf48ed0e`**, clean tree/index. The final summary adds only this report, CSV, evidence manifest, index and a source-line citation correction in E. Its own resulting local/remote SHA and final clean tree/index are recorded after push in the external `investigacion-u1-20261107/ENTREGA_FINAL.json` receipt and delivery message (a commit cannot embed its own hash).

`git diff 2ab2ad3 HEAD` outside this new documentation directory is empty. Production, original fixtures/goldens, historical evidence and prior documentation are unchanged; no permanent diagnostics, dependency/workflow changes, excluded MOD fixes or new branch. Read-only references were checked clean at their pinned commits: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`, osu `9a10d935b05a3b896f4bd811d620f2023331db93`, framework `37c3e328c8cc7a018234ec78982ea12d3712096a`. No web research was necessary; local source/history and prior differential evidence sufficed.

**Recommendation: independent review and a separate authorization to correct the heterogeneous collective BAD defect before merge.** No PR, merge, force push, rebase or squash. This investigation does not renew integral LR2/Endless Dream certification, and does not authorize the next repair automatically.
