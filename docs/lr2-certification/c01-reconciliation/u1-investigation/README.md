# C01-U1 — Independent investigation

Started 2026-11-07. Authorized branch: `fix/lr2-endlessdream-parity-incremental`; starting local/remote HEAD `2ab2ad3d6e5d77232ae5c6617a1a6480c8354dff`, clean tree/index. Origin is `hinobara-ctrl/BmsRuleset`. Historical baseline: `59e186c527bb8e2ab3e8ea82c6668e21b4bf147a`; pre-certification: `bae80bc95a2abde15cc1519d0cf784e43cbd9ced`; pre-reconciliation: `795f54403a5db3b1db8ffa6aa985ace38182df92`.

This is investigation of the ten original U1 observations, not a production repair or an attempt to make CI green. Previous R1–R5 classifications/evidence and all original assertions remain intact. Short filtered executions use .NET SDK 10.0.401, Release, the pinned osu source `9a10d935b05a3b896f4bd811d620f2023331db93`. No broad suite/CI is required merely to publish these findings.

* [U1-C: Duration and heterogeneous judgement rates](U1_C_DURATION.md)

Evidence references use paths relative to the external `nuevo-trabajo` evidence root, avoiding personal paths in Git. Temporary diagnostic sources, logs, TRX and binary hashes are preserved there; none are distributed as gameplay binaries. A diagnostic source overlay is explicitly distinguished from the unmodified source SHA. No permanent probes or production hooks are added.
