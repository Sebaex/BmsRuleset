# U1-D — F178/F179/F180: Course overview timeout

**E1: one culture-dependent fixture comparison, demonstrated at all three sizes.** No incorrect aggregate or asynchronous initialization defect is demonstrated by this timeout. Confidence: high for the first blocked predicate; later interaction steps remain unexecuted and are not certified.

## Where execution stops

`Visualize/TestSceneBmsCourseResults.cs:281–353` creates a four-stage course: first completed, second aborted, last two unplayed. The screen loads, four cards/metadata and overview appear, preceding layout/text checks pass (including a literal `77.00%` check), then `aggregate left statistics match` waits ten seconds for `overviewMatches` (`:192–203`). The helper joins independent numeric and textual predicates into one Boolean.

A nine-line temporary diagnostic logs each conjunct's actual inputs on the existing update thread once per distinct value/size. It does not alter the return expression, any expected value, wait duration or scheduling. Observed at **1024×600, 1280×720 and 1600×900**:

| Check | Expected | Observed | Predicate |
|---|---|---|---|
| Overview loaded | present | present and preceding screen checks passed | true |
| Aggregate accuracy | .77 | .77 | true |
| Maximum combo | 375 | 375 | true |
| PGREAT row count | 600 | 600 | true |
| Combo text | `375` | `COMBO`, `375`, `/856` | true |
| FAST / SLOW | 1 / 2 | 1 / 2 | true |
| Percentage text | `$"{accuracy * 100:F2}%"` | helper expects **`77,00%`**; `text.Text.ToString()` is **`77.00%`** | **false** |

The numeric backend is coherent: `Gameplay/Course/BmsCourseScoreAggregation.cs:13–39,48–52` includes only stages with scores, weights accuracy by maximum statistic count (equal here, `(.91+.63)/2=.77`), sums statistics (300+300 PGREAT), takes max combo (375 vs 150), and concatenates hit events (one fast, two slow). Unplayed stages supply cards, not artificial zero scores. The score, rows and timing controls are already available; waiting longer cannot reconcile a comma and a point.

## Why these strings differ

`UI/Result/Statistic/BmsResultOverview.cs:56–76` supplies `BmsStrings.ResultPercentage(score.Accuracy)`. English/Chinese resources use `{0:0.00%}`. The test reads the underlying `LocalisableString.ToString()`, **not the resolved rendered string from the localisation manager**. In the pinned framework (`37c3e328c8cc7a018234ec78982ea12d3712096a`), `osu.Framework/Localisation/LocalisableFormattableString.cs:73` implements `ToString()` using `CultureInfo.InvariantCulture`. Rendering separately resolves `Store.EffectiveCulture` (`:43–53`).

The helper constructs its expected string with the update thread's current culture, observed `es-CL`/`es`, whose decimal separator is comma. This is an inconsistent fixture comparison, not proof that displayed accuracy is wrong or that the product should use an invariant locale globally. `Platform/GameHost.cs:1292–1303` applies its configured locale to framework threads and default cultures. A repeat with `NUnit.DefaultCulture=en-US` still observes Spanish on the update thread and fails identically: NUnit's test-thread culture is not a reliable host-locale control.

`git diff 59e186c 2ab2ad3 -- <fixture> <BmsCourseScoreAggregation.cs>` is empty. Framework semantics and these assertions are preexisting. This does not claim that an unchanged fixture proves unchanged dependencies; the actual values, pinned framework implementation and differing culture paths demonstrate the defect independently. No Java comparison is relevant to this C# localisable-string assertion.

## Evidence and limitations

External paths relative to the index's evidence root; SDK 10.0.401, Release/net10.0, production unchanged at `2ab2ad3`; diagnostic build based on docs-only HEAD `0f4a450`.

* `evidencias-paridad/U1-D-01/tests.trx`: three attempts fail during host setup due sandbox access denial; **not** three product timeouts. Preserved TRX SHA-256 `A22AB633EA49279361AD623E82DF5B1104C05F1A0E3180799F7727869AB82AF9`.
* `investigacion-u1-20261107/D-02/tests.trx`: same DLL outside sandbox, **0/3 pass**, about 33 seconds, all reach the same aggregate predicate; independent values above are in each case's stdout. TRX SHA-256 `78F1548527FFBA825F443C3958E576D102A4EA787D7E550637A5AC5882FE16D1`.
* `investigacion-u1-20261107/D-03-en-US/tests.trx`: controlled NUnit setting attempt, **0/3 pass**, about 33 seconds; the update-thread culture did not change. TRX SHA-256 `5E344DC04371CD16C867A40DBF1AC872DB394B170EACF8D5AD97A2D2B6249647`. No passing control is invented from that setting.
* Filter for all: `FullyQualifiedName~TestCourseSummaryShowsPlayedAndEmptyStages`; `dotnet test <diagnostic Release test.dll> --filter <filter> --logger 'trx;LogFileName=tests.trx' --results-directory <fresh-directory> -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None`; D03 additionally sets `NUnit.DefaultCulture=en-US`.
* Exact modified fixture archived as `investigacion-u1-20261107/TestSceneBmsCourseResults.diagnostic.cs`, SHA-256 `5548AE4A9D9FEB30FB2C86D9B4278E89E034F97AA0F7BD1CB2F7CA80E3FC6A79`; patch `D-diagnostic.patch`, full logs and per-run binary hashes retained. Test DLL SHA-256 `1ED2EA4B62C7E7A65673980BDDA62288F5296B4BCF44291481E973BE5BC3BB92`.
* Original fixture restored byte-for-byte: SHA-256 `79A859C872A93A79384DA2DA2BD9BA65CB4E8240D7A3D1BF5D98871CE0BC8B22`, matching the pre-diagnostic backup. No permanent harness, changed expectation or production modification is published.

These runs do not reach subsequent stage-selection/restoration checks because the original first wait still fails. They establish neither a later scheduler defect nor full UI success. Final-gauge backend behavior and R4-C's separate explicit-profile corrections are not reclassified by this string issue.

## Proposed later action

Repair only the fixture's culture contract: if comparing `Text.ToString()`, derive the expected percentage through the same `BmsStrings.ResultPercentage(accuracy).ToString()` path (or explicit invariant formatting). If testing rendered localisation, use the host's localisation manager and resolved text instead. Keep separate numeric/statistic assertions and add a controlled comma-decimal host-locale case so a timeout cannot hide the reason. Do not change Course aggregation, UI formatting or global user locale. Such maintenance is deferrable and is **not implemented here**; all original failures/evidence remain intact.
