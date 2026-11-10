# New findings kept visible

Successor status: TM-NEW-01 was subsequently authorized for a minimal production correction. See [the correction report](GAS_SERIALISATION_FIX.md). The diagnosis and failed executions below remain historical evidence; they are not overwritten by the passing successor run.

## TM-NEW-01 — GAS Assist lower limit lost during API serialization

Confirmed by `BmsModGaugeAutoShiftTest.TestGasLowerLimitSurvivesCloneAndSerialisation(Assist)` in TM-D03. This is a **new test identity, not another original C01 observation**. No production correction or skip is applied. Original F139's health serialization/control checks are independently repaired and pass.

Reproduction: construct GAS with `GasMode=Assist` and a real carried ClassHealth=.81 as in the original Course fixture; DeepClone; create APIMod; serialize/deserialize it; ToMod. Observed source Default=Assist, clone=Assist, API keys only `class_health`, restored lower limit=Easy. Normal/Easy controls preserve both clone and serialized value. Expected result follows the explicit C# configurable-setting preservation contract, not a Java equivalence claim.

Cause: unchanged production `Gameplay/Mods/Gauge/BmsModGaugeAutoShift.cs:52` initializes `new Bindable<BmsGasLowerLimit>() { Value=Easy }`. Enum zero is Assist, so Default remains Assist. Pinned osu `Online/API/APIMod.cs:40–45` omits settings whose bindable IsDefault; ToMod constructs a fresh GAS whose Value is Easy and has no saved gas_mode to apply. `Rulesets/Mods/Mod.cs:152–174` clones values correctly; the final reproduction proves cloning itself is not the failing stage. This production source predates C01-TM and remains unchanged; no earlier introduction date is asserted.

Evidence preserved separately: TM-D01 (134/135, nondefault setting assertion discovered before original visibility checks); TM-D02 (137/138, an empty-settings JSON attempt hits APIMod.ToMod's null Settings before the assertion); TM-D03 (137/138, real nonempty Course representation isolates the setting-loss assertion). The empty-settings path is not declared fixed or fully classified; the final fixture uses the original Course representation to avoid conflating it with GAS default metadata. All logs/TRXs/binary hashes remain in their fresh evidence directories.

Proposed later minimal correction: initialize the bindable with Easy as its **Default and Value**, e.g. `new(BmsGasLowerLimit.Easy)`. Do not change APIMod or reference sources. Validate all three settings, real score/mod persistence, cloned Course states and regular replay chains; evaluate existing records whose gas_mode was already omitted, since the lost choice cannot be inferred from that field alone. Possible downstream chain/replay effects are risks, not complete end-to-end demonstrations here. No repair authorized by the translation exception.
