# HUD skin-editor lifetime review

On Sebaex `dev` at `a1ab55d16eee20aa37efb47ba96910e8f319e474`, the long-lived skin editor registers delegates capturing `BmsTextHud` and `BmsStageHud`. The delegates remain attached after the HUDs are disposed and keep their gameplay graph reachable.

## Change

The existing production fix `f95506b` is isolated from the research harness and compatibility patch. Each HUD owns a `Bindable<Visibility>` bound to the editor's state. The pinned framework uses weak links between bindables and unbinds drawable-owned fields during removal/disposal. The callback is owned by the HUD's bindable, so the editor no longer owns a delegate capturing the HUD. Immediate initialization preserves an editor already visible when the HUD loads.

The review needed no further production edits. It added regression coverage for initial hidden/visible editor state and collection of both disposed HUDs while the shared editor remains alive. Existing gameplay-event unsubscriptions and stage-controller unregistering were inspected and retained.

## New validation

Release compiled with .NET SDK 10.0.401 and the matching 2026.1005.0 source (`9a10d935`), without editing reference sources.

| Filtered run | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Six HUD cases against original production code | 4 | 2 | 0 |
| Candidate: HUD/stage/legacy skin, composition, BGA and PCM/BASS regression selection | 380 | 0 | 2 |

Original failures were post-disposal callbacks and collection while the editor remains alive. Both pass with the fix. The collection probe uses forced collection only to test reachability; no collection was added to production code. [Validation record and TRX hashes](2026-11-20-hud-validation.json).

## Earlier performance evidence and remaining review

The earlier [paired investigation at `3007910`](https://github.com/hinobara-ctrl/BmsRuleset/blob/3007910e01955d75ddca3c8117898cd29bc7bd78/docs/research/BMS_SESSION_MEMORY_FIX_REVIEW.md) observed earlier reachable players falling from 12 to 0 and BGA displays from 24 to 0 after controlled collection. It also observed MP4 update P95/P99 increases of 20.0%/17.3%, with median absolute increases of 0.0864/0.1324 ms under its matched inactive-window protocol.

Those measurements belong to the earlier experiment, not a new benchmark of this isolated branch. Their cause and relevance to presented frames remain unresolved. This candidate therefore remains a Draft for external review. The user's successful general ingame testing is additional manual evidence, not a replacement for a measured performance comparison. Prolonged session behavior, audible latency and GPU/native resource attribution are not newly certified.
