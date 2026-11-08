# F099 — absolute exclusion

`BmsColumnNoteIndexTest.DurationSearchSkipsCloserHeadsWithNoHitWindow` remains unchanged, including all four notes, rates, input, expected GOOD and single-result assertion. No Ignore/Explicit/Skip/KnownFailure, production change, collector investigation or new heterogeneous-rate scenario is introduced by C01-TM.

The original Java collector operates with one lane table; the C# per-note heterogeneous-window extension has no direct equivalent original Java execution. C01-U1's secondary finding is retained in its immutable report as historical evidence, but **C01-TM does not classify it as a confirmed Java/LR2 parity defect**. Resolving the extension's contract and any later correction requires independent authorization. The original failure stays visible in focused/broad results. Its original method region and source file were archived before edits; its method body is checked against that snapshot before publication.
