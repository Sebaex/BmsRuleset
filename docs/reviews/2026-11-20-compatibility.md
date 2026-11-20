# osu!lazer 2026.1115.0 compatibility review

The original ruleset targets lazer 2026.1005.0. This candidate adapts the decoder, sample-info, footer, cover and skin-editor interfaces to 2026.1115.0. Its base is Sebaex `dev` at `a1ab55d16eee20aa37efb47ba96910e8f319e474`.

## Change and inspection

The existing `9f4c5e1` patch was cherry-picked independently. The review required no additional production edits to that patch. The memory experiment's six commits are absent from this branch.

- `Directory.Build.props` selects 2026.1115.0.
- `BmsBeatmapDecoder.CreateTemplateObject()` follows the new public override contract.
- `BmsSampleInfo.IsHitsound` is false, matching the new interface. The PCM playback route continues to obtain master gain from `AudioManager.AggregateVolume`, independently of the effects/sample channel.
- Selection and result screens use `FooterContent`. The upstream loader now owns quick-restart footer behavior. Obsolete overrides and `GlobalScrollAdjustsVolume` components are removed.
- Leaderboard covers use `CoverBackground.Model`; affected skin-editor and footer tests follow the new APIs.

The inspection covered constructors, callbacks, result/selection disposal, beatmap-selection cancellation and loader/footer ownership. No new worker, update loop or cache is introduced by the patch.

## Validation

Release compiled with .NET SDK 10.0.401 against the pinned 2026.1115.0 source (`169c020`, outside this repository). Build artifacts were redirected away from the read-only reference source. Existing warnings remain.

| Filtered run | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Selection, preview/background audio, PCM/BASS, BGA, decoder, skin and results, excluding the three separately controlled viewport cases | 682 | 2 | 2 |
| Identical filter on original Sebaex code, with its matching 2026.1005.0 dependency | 682 | 2 | 2 |
| Audio routing/helper fixture | 11 | 0 | 0 |
| Viewport and historical-comparison controls, original | 3 | 3 | 0 |
| Same controls, candidate | 3 | 3 | 0 |

Both aggregate runs failed `TestLampColoursMatchBeatoraja` with the same clear-colour assertion and `TestHistoricalBestComparison(1280,720,False)` with the same historical-score timeout. Historical comparison passed in both smaller control runs. All three viewport failures have the same `rank composition fills the narrowed column` assertion on the original and candidate. These failures remain unresolved; matching observations identify existing failures, not a fully green suite.

To reproduce with `--artifacts-path`, set `BmsNativeBrotliToolPath` to the redirected native-tool DLL and copy `osu.Game.Rulesets.BmsRuleset.Tests/bms_test_songs` into the artifact root expected by file-based fixtures. Ordinary CI output paths do not need this override.

Counters and TRX hashes are in [the validation record](2026-11-20-compatibility-validation.json). Runs overlap. The user reports successful general ingame testing; this does not identify every exact isolated candidate binary.

## Review status

Suitable for a focused Draft PR. Cross-platform CI and maintainer review remain pending. No new benchmark, audible-latency measurement or prolonged memory session was performed. The broad lifecycle review found separate HUD retention and supplemental-video defects; their fixes are independent PR candidates, not hidden in this compatibility change.
