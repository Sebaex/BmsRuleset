# Supplemental BGA upload and worker lifetime review

This candidate starts at Sebaex `dev` `a1ab55d16eee20aa37efb47ba96910e8f319e474` and fixes defects confirmed during the lifecycle inspection. It uses the original 2026.1005.0 dependency and is independent of the lazer and HUD changes.

## Findings and changes

1. **Premature pixel-buffer return.** `Texture.SetData()` queues an upload for later consumption by the renderer. The drawable previously disposed that upload immediately, returning the pixel array to its pool while OpenGL/Veldrid could still read it. The drawable now transfers the upload to the renderer; a failed submission disposes the unaccepted upload. Texture creation happens before taking the pixels out of the frame, so a creation failure still returns them through frame disposal.
2. **Pending uploads when drawing is delayed.** Retaining pixels correctly must not grow the renderer's queue on every update. While the texture reports an incomplete upload, the drawable coalesces decoded frames into one newest waiting frame instead of submitting another upload. The latest frame survives EOF and is submitted when drawing resumes. Disposal returns any waiting frame. The framework's completion flag reflects its queue; a renderer may also be consuming an already-dequeued upload concurrently.
3. **Polling after terminal decode.** A drained in-memory decoder previously remained alive in an eight-millisecond polling loop until the drawable was disposed. The decoder exposes its finished state and the worker exits after drainage or a reported decode error. Exiting disposes the native decoder; queued displayable frames remain available until consumed or disposed with the source. A temporary no-frame result without a terminal state still retries.

The changes preserve video routing and the existing clock/timestamp selection. The decoder's time-ahead queue and preloader policy are unchanged; this is not a strict total-video byte budget.

## Validation

Release compiled with .NET SDK 10.0.401 against the pinned 2026.1005.0 source (`9a10d935`). References remain read-only.

| Filtered run | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Valid original-code reproduction: deferred consumption, failed submission, EOF | 1 | 2 | 0 |
| Final candidate: normal BGA, PCM/BASS and existing HUD cases | 221 | 0 | 2 |

The original returned the upload buffer before deferred consumption and never completed the EOF worker. Failed-submission cleanup already passed and remains covered. New tests delay renderer consumption across 100 updates, check bounded waiting frames and exactly-once returns, submit the final waiting image without requiring another decoded frame, and check disposal while an upload remains queued.

The upload tests exercise the real drawable/frame/Texture path with scoped patches to the dummy native backend. They are non-parallel, remove patches after each test and require no GPU. A real tiny MPEG fixture exercises drainage and worker completion. [Counters and TRX hashes](2026-11-20-video-validation.json).

## Remaining validation

Cross-platform CI, actual GPU consumption under OpenGL/Veldrid, resize/error paths on real hardware and prolonged ingame validation of this new binary remain pending. Correct ownership can keep a frame buffer alive longer than the old unsafe path; the pending-upload gate bounds that portion of the producer's queue. No throughput, frame-time, memory or audio benchmark was run. This is a focused Draft PR for independent review.
