# LR2 / Endless Dream certification

This index distinguishes differential execution, mathematical demonstration, C# regression tests, static review, confirmed differences and uncertified behavior. Passing C# tests alone does not certify a Java implementation.

Reference sources: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. The initial ruleset HEAD is `bae80bc95a2abde15cc1519d0cf784e43cbd9ced`.

| Stage | Report | Evidence commit |
|---|---|---|
| C01 | [Build and tests](C01_BUILD_AND_TESTS.md) | [`8efac785b671adafcefb53e77f8666fc9f75fba1`](https://github.com/hinobara-ctrl/BmsRuleset/commit/8efac785b671adafcefb53e77f8666fc9f75fba1) |
| C02 | [Judgement](C02_JUDGEMENT.md) | [`feb2727da475f9352a83829fa64648b2deb4cf7e`](https://github.com/hinobara-ctrl/BmsRuleset/commit/feb2727da475f9352a83829fa64648b2deb4cf7e) |
| C03 | [Gauge and scoring](C03_GAUGE_AND_SCORING.md) | [`b6fa4b29234c799973486f026cfecb0495298d5d`](https://github.com/hinobara-ctrl/BmsRuleset/commit/b6fa4b29234c799973486f026cfecb0495298d5d) |
| C04 | [Long notes](C04_LONG_NOTES.md) | [`7a5e5613c0349c6c093580ad8a1ad8ea7b81af3c`](https://github.com/hinobara-ctrl/BmsRuleset/commit/7a5e5613c0349c6c093580ad8a1ad8ea7b81af3c) |
| C05 | [GAS and replay](C05_GAS_AND_REPLAY.md) | [`d3e6cd707baa7588906c9c669b5dc7d012ec266f`](https://github.com/hinobara-ctrl/BmsRuleset/commit/d3e6cd707baa7588906c9c669b5dc7d012ec266f) |

[Final verdict](C06_FINAL_VERDICT.md): **APTO PARA REVISIÓN DE PR with reservations; no integral LR2 1:1 certification.** Final focused validation: 420 passed. Broad baseline: 216 ordinary and three instrumentation failures. Executed original Java component oracles certify the finite scopes described in each report; C#-only success is labelled separately. [Evidence hashes and run counts](C06_EVIDENCE.json).

The evidence commit is recorded after publication in the next index update. The final index records the verdict commit without attempting a self-referential commit hash.
