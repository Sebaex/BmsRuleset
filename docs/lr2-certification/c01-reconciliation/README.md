# C01 reconciliation

Investigate the 216 ordinary and three instrumentation failures observed at `bae80bc95a2abde15cc1519d0cf784e43cbd9ced`; do not change historical expectations merely to obtain a green suite. Initial local and remote HEAD: `795f54403a5db3b1db8ffa6aa985ace38182df92`, branch `fix/lr2-endlessdream-parity-incremental`, clean tree and index. Historical baseline: `59e186c527bb8e2ab3e8ea82c6668e21b4bf147a`.

Reference Java remains Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a` and parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. C02–C06 evidence is reused within its declared limits.

| Stage | Report | Published evidence commit |
|---|---|---|
| R1 | [Inventory](R1_FAILURE_INVENTORY.md), [219-case mapping](R1_FAILURE_TRIAGE.csv) | [`35000f9e8d5d9953fc7093acd78b63c53cd9c60a`](https://github.com/hinobara-ctrl/BmsRuleset/commit/35000f9e8d5d9953fc7093acd78b63c53cd9c60a) |
| R2 | [Historical expectations](R2_HISTORICAL_EXPECTATIONS.md) | [`eefd14fff41a58c1623f61d41e8b6580d4c21eac`](https://github.com/hinobara-ctrl/BmsRuleset/commit/eefd14fff41a58c1623f61d41e8b6580d4c21eac) |
| R3 | [Current failures and correction decisions](R3_CURRENT_FAILURES.md) | [`e8ca443dfa3d6876d6a64fd855bb19565a8d276b`](https://github.com/hinobara-ctrl/BmsRuleset/commit/e8ca443dfa3d6876d6a64fd855bb19565a8d276b) |
| R4-C | [Explicit course overrides](R4_CORRECTIONS.md#r4-c--explicit-course-gauge-constraints) | [`f6f7af30cc3e87f6aaf339ea7cd3993f2ceb16de`](https://github.com/hinobara-ctrl/BmsRuleset/commit/f6f7af30cc3e87f6aaf339ea7cd3993f2ceb16de) |
| R4-P | [Preview source fallback](R4_CORRECTIONS.md#r4-p--missing-and-empty-preview-source) | [`b98c9db857d24f52d25786a25900b4f6e1f7809b`](https://github.com/hinobara-ctrl/BmsRuleset/commit/b98c9db857d24f52d25786a25900b4f6e1f7809b) |
| R4-G | [Configured GAS graph tiers](R4_CORRECTIONS.md#r4-g--configured-lower-tiers-in-legacy-gas-graphs) | Recorded after publication in the next index update |

R1 is a provisional inventory, not a declaration that its 42 fixture families are 42 independent bugs. Later reports retain the original rows/IDs and provide a separate final classification. Historical C01/C06 results, manifests and failed TRXs are preserved.
