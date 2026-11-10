# Validación y procedencia — LR2-JF

Baseline `b84d77d56814c3ae9ab95de02102fbcfd4e9a8bc`; Course `752d7699cc55a0e80e8254b6537e45c8edb21025`; pruebas `1481d94c2501bee2ecb822434bda39d054194bd8`; corrección productiva `48bd25aec9490d0e34be2bf501cce738f2007952`. Los commits documentales posteriores no cambian sus árboles de código. El [manifiesto sucesor](VALIDATION_MANIFEST.json) registra árboles Git, hashes LF/UTF-8 del candidato, filtros exactos, hashes de cada log/TRX/binario, todas las identidades fallidas y las omisiones del baseline CI. No sustituye manifiestos anteriores.

Entorno local Windows x64, SDK.NET10.0.401, Release net10.0, osu! y framework fijados en la auditoría; JDK Temurin21.0.13+9. Se emplearon rutas de artefactos/caches externas al checkout y el compresor nativo existente. Ninguna versión de dependencia, referencia, workflow o runsettings cambió. C06_PROVENANCE autentica13 archivos de los oráculos anteriores: todos coinciden; checkouts de las cuatro referencias limpios y con sus HEAD fijados.

## Ejecuciones conservadas

Los identificadores corresponden a directorios externos nuevos de `evidencias-paridad`. Cada ejecución conserva build.log, y cuando compila, filter.txt, hashes.json, tests.log y tests.trx. Las duraciones siguientes son el intervalo VSTest del TRX; no incluyen restauración/compilación. No sumar estos focos como casos únicos: se solapan.

| ID | Resultado | Segundos | Interpretación |
|---|---:|---:|---|
| JF-A-BASELINE01 |13/14,1 fallo |9.92 |F099 original reproduce fallo. Course pasa localmente por entorno heredado; no invalida sus fallos CI. |
| JF-COURSE01 |11/11 |16.56 |Fixture Course completo, previo a commit752d769. |
| JF-JAVA01 |Compilación fallida |— |Faltaba importar extensión GetEndTime en fixture nuevo; no hubo tests. |
| JF-JAVA02 |446/452,6 fallos |3.76 |Cuatro errores del harness al identificar records simultáneos; dos divergencias productivas. Se preserva la evidencia. |
| JF-JAVA03-BASELINE |463/489,26 fallos |2.97 |488 vectores originales y control variable. Corregida la identidad del harness,26 divergencias fijas reales; producción todavía baseline. |
| JF-JAVA04 |550/551,1 fallo |2.14 |Todos488 vectores diferenciales aprobados tras corrección; falla expectativa protegida antigua de NoGreat. |
| JF-FIXED05 |Compilación fallida |— |Setter TimeOffset interno de osu!; el fixture lo elimina y valida offsets por replay. No hubo tests. |
| JF-FIXED06 |624/624 |2.00 |Matriz final536, parser/variable, sucesor F099 y expectativa NoGreat respaldada por Java. |
| JF-PROTECTED01 |427/427 |2.30 |Mismo multiset exacto de clase+caso+resultado que TM-PROTECTED01, incluida multiplicidad. |
| JF-INSTRUMENTATION01 |288/288 |134.68 |Filtro y runsettings originales, sin benchmarks. |
| JF-STABILITY01 |140/140 |20.41 |Replay, Course, scratch y NoBad focales. |
| JF-FIXED07 |627/627 |2.02 |Añade tres continuaciones CN con mods; compilación final20.45s,0 errores. |

Los536 vectores finales se regeneraron dos veces en java-03/java-04: CSV idéntico, SHA-256 en [comparación](02_JAVA_CSHARP_PARITY.md). java-01/java-02 y todos los intentos anteriores siguen disponibles; no se sobrescribieron.

El foco protegido se comparó con el TRX original TM-PROTECTED01, SHA-256 `368DAD2713B3127B14E788C092971F1BFD734D47AC72962B174764574C5FEE74`. El sucesor tiene hash `E26824387C43E2647290E42A54B1FC6163E1DEB4E6EF70766B237486551B9058`. [Las427 identidades/resultados](research/protected-427-results.csv) coinciden exactamente. Una aserción de TestAlgorithmsUseModifiedWindows cambió por los vectores exactos Java; no se declara que todos sus cuerpos permanezcan intactos.

Los tres últimos controles de replay se añadieron después de instrumentación/estabilidad. Esos dos TRX usan su propia DLL de tests anterior: producción, fixtures instrumentados y casos de estabilidad son idénticos al candidato final; JF-FIXED07 recompila y valida el nuevo fixture. Los hashes por ejecución distinguen esos binarios. El CI pendiente debe validar el árbol completo en ambos sistemas.

## Reproducción

Variables de rutas externas sin información personal; ejecutar desde el checkout. Las versiones/pins deben coincidir y cada RESULTS debe ser nuevo.

```powershell
dotnet build osu.Game.Rulesets.BmsRuleset.Tests/osu.Game.Rulesets.BmsRuleset.Tests.csproj -c Release "-p:OsuSourcePath=$OSU_SOURCE" --artifacts-path $ARTIFACTS "-p:BmsNativeBrotliToolPath=$NATIVE_TOOL" -p:UseSharedCompilation=false -m:1 -nr:false --verbosity minimal
dotnet test $TEST_DLL --filter $FILTER --logger 'trx;LogFileName=tests.trx' --results-directory $RESULTS -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None
# Instrumentación: mismo comando anterior más estos argumentos antes del separador --:
# --settings osu.Game.Rulesets.BmsRuleset.Tests/judgement-instrumentation.runsettings
```

`FILTER` de cada ejecución está íntegro en el manifiesto. Para los627 casos es la unión OR de BmsFixedJudgeJavaParityTest, BmsJudgeRateCompatibilityTest, BmsColumnNoteIndexTest, BmsJudgementSelectorTest, BmsExecutedJavaJudgementTest, BmsMultiBadParityTest y BmsModNoGoodNoGreatTest, cada término `FullyQualifiedName~Nombre`. Instrumentación usa exactamente `FullyQualifiedName!~.Performance.&FullyQualifiedName!~Benchmark&TestCategory!=Performance&TestCategory!=Benchmark&TestCategory=JudgementInstrumentation`. El foco protegido reutiliza su filtro original sin modificaciones.

```powershell
& osu.Game.Rulesets.BmsRuleset.Tests/reference_fixtures/fixed-judge-parity/Generate.ps1 -Jdk $JDK -EndlessDream $ENDLESS -Parser $PARSER -SupportJar $SUPPORT_JAR -Output $FRESH_JAVA_RESULTS
```

No se ejecutó una suite local sin filtro ni benchmarks. No se normalizó el archivo F099 original: preserva los caracteres de su región archivada. `git -c core.whitespace=cr-at-eol diff --check` distingue esos CR preservados de espacios sobrantes; no cambia configuración ni atributos Git.

## CI, omisiones y límites

[Run baseline42494001031](https://github.com/hinobara-ctrl/BmsRuleset/actions/runs/42494001031), SHA b84d77d: builds correctos. Windows ordinario3958=3953 aprobados+2 fallos+3 omitidos; Ubuntu3958=3945+2+11. Fallan F099 y Course en ambos. Instrumentación288/288 en ambos. Las omisiones se cuentan por los resultados UnitTestResult: el atributo Counters.notExecuted del TRX vale0 a pesar de esos registros; el manifiesto conserva ambas cifras sin perder casos.

Windows omite corpus replay ausente y dos escenarios incompatibles con FFmpeg nativo incluido. Ubuntu añade cinco controles de CPU exclusivos de Windows y tres de file-sharing de Windows. Las identidades y motivos exactos están en el manifiesto. No se reclasifican los41 fallos históricos ni las investigaciones posteriores: se conservan sus informes y evidencia originales.

La compilación final muestra54 advertencias NU1902/NU1903, códigos ya presentes en baseline. No hay códigos nuevos; CS8629 aparecía en la compilación baseline y no reaparece en la última incremental, sin modificar por ello su fuente. No se deduce de ese recuento una reparación de dependencias ni una auditoría de seguridad.

Publicación prevista: push normal de la rama activa. El trigger push existente de ci.yml inicia la matriz Windows/Ubuntu; se reutilizará ese run para evitar duplicarlo. Solo si no se crea, se despachará el workflow existente con la misma referencia. La recepción de ejecución y SHA/URL se conserva fuera de Git al publicar. **CI final pendiente**: no se afirma suite completa aprobada, ni se conoce todavía su conjunto final de omisiones. Tras confirmar lanzamiento de la ejecución larga, termina la intervención sin esperar/pollear resultados, conforme a la instrucción de duración vigente.

## DLL Release

La DLL comprobada proviene del build exitoso JF-FIXED07, árbol productivo `77488b1eacb4c5d01af9bce992dcb375db84b538` del commit48bd25a. SHA-256 `25CE80CF3A5E93D5A9469054E7EB080290698B392FA4F704014B26DFBE7102F9`. La DLL de tests de esa ejecución: `91205783967119CFDE6CB9F492E9731D26F466D328D9A86C9CE6258EAD345F1F`. Entregable externo con recibo de hash y commit; no se publica binario diagnóstico en Git. La receta permite reconstruir las fuentes; no se promete identidad binaria de compilaciones distintas del proceso de empaquetado.
