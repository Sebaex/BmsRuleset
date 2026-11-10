# Gameplay audit research harness

This is an opt-in **differential investigation**, not a replacement for ordinary tests. Its assertions use independently generated Java results and intentionally remain failing where the audited baseline differs. It returns exit code 1. Do not turn those differences into accepted C# goldens or ignore them.

Inputs and findings: `docs/lr2-certification/gameplay-parity-audit/`. Pins and source hashes: PROVENANCE.json and SOURCE_INDEX.json there. No production hooks, external-reference edits or ordinary project/filter changes are required. F099 is excluded from the final harness.

From the repository root, supply local read-only checkouts and a matching auxiliary JAR:

```powershell
./osu.Game.Rulesets.BmsRuleset.Tests/reference_fixtures/gameplay-parity-audit/Generate.ps1 `
  -Jdk <jdk21> -EndlessDream <pinned-endlessdream> -Parser <pinned-parser> `
  -SupportJar <verified-auxiliary-jar> -Output <fresh-java-output>
```

Verify generated flow.csv/mines.csv against the checked-in files. The generator checks both HEADs, JAR hash and CodeSource of evaluated classes. It compiles original algorithms plus an extracted literal score/combo prefix; it does not launch the original player. Flow phase order and Course carry are composed from the static callers. Mine parsing invokes original BMSDecoder directly on the same chart text used in C#.

```powershell
./osu.Game.Rulesets.BmsRuleset.Tests/reference_fixtures/gameplay-parity-audit/Run-Audit.ps1 `
  -OsuSource <pinned-osu> -NativeTool <built-native-brotli-tool-dll> `
  -Output <fresh-audit-output>
```

This builds Release with CustomAfterMicrosoftCommonTargets pointing to audit.targets, using a separate artifacts path. The `.cs.txt` fixture and two resources are included only then. The script snapshots inputs, captures logs/TRX/hashes, runs only `FullyQualifiedName~GameplayParityAudit` and propagates its actual exit code. Ordinary SDK Compile/EmbeddedResource contains none of these opt-in inputs. Do not reuse an existing output directory.

Final baseline result: 18 cases, 12 differential failures, six passing controls, zero omitted. Repeat the same compiled DLL with `dotnet test <test-dll> --filter FullyQualifiedName~GameplayParityAudit --logger trx --results-directory <fresh-output> -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None`. Compare identities/outcomes, not TRX bytes containing timestamps. No benchmark or whole-suite run is part of this command.
