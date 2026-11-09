# LR2-CL — regresiones y CI

Baseline `eb54f7ebde2c31cbe0b66b0093f2f22f9ba0775a`; corrección de fixture `8775657344b986ede3542043d8ac4fc22ce81210`. SDK.NET10.0.401, Windows x64, Release/net10.0, osu! y framework fijados en [causa raíz](01_ROOT_CAUSE.md). No hay cambios de producción ni de dependencias. El [manifiesto](VALIDATION_MANIFEST.json) conserva resultados por ejecución, filtros exactos, hashes de logs/TRX/binarios, diferencias de identidades y motivos de omisión del CI anterior.

## Todos los intentos

Los directorios externos nuevos usan estos identificadores en evidencias-paridad. Las duraciones son el intervalo VSTest del TRX, no la compilación. Los focos se solapan: no sumar sus cantidades como identidades independientes.

| Ejecución | Resultado | Segundos | Alcance |
|---|---:|---:|---|
| CL-BASELINE-EMPTY01 |0/1,1 fallo |16.57 |Arreglo controlado de locale vacío, diagnóstico mínimo y teardown original. |
| CL-BASELINE-CASCADE01 |7/11,4 fallos |53.78 |Reproduce los cuatro fallos y la contaminación entre tests del baseline. |
| CL-COURSE01 |13/13 |13.76 |Primera corrección y dos regresiones nuevas. |
| CL-COURSE02 |13/13 |13.98 |Candidato final: ConfigManager.SetValue sin TriggerChange redundante en copia enlazada. Release0 errores,16.02s de build. |
| CL-COURSE03 |13/13 |13.58 |Repetición completa, proceso nuevo y misma DLL verificada. |
| CL-LOCALISATION01 |20/20 |.98 |Recurso/localización existente. |
| CL-FIXED01 |627/627 |1.77 |Filtro LR2-JF sin alterar; incluye536 vectores Java capturados. |
| CL-PROTECTED01 |427/427 |2.06 |Mismo multiset de clase+nombre+resultado que JF-PROTECTED01. |
| CL-STABILITY01 |142/142 |20.71 |Los140 anteriores más exactamente las dos regresiones de locale. Ninguna pérdida o cambio de resultado anterior. |
| CL-INSTRUMENTATION01 |288/288 |120.30 |Mismos filtro y runsettings de instrumentación existentes. |

La comparación Course conserva las11 identidades originales:las cuatro que fallaban en la reproducción ahora aprueban, las otras siete siguen aprobadas; se añaden dos casos. No se borran resultados fallidos ni se sustituyen sus TRX. La diferencia de estabilidad es exclusivamente la adición de esas dos identidades. El manifiesto registra las comparaciones con multiplicidad y los hashes de origen/destino.

Los controles nuevos capturan: locale vacío→host es-CL/store es; locale en→host en/store en, con SystemCulture es-CL. Cada uno realiza tanto la ronda normal como el control de limpieza ante excepción. Se restauran las referencias originales y se comprueba el mismo fallo deliberado, no solo que dos campos tengan el mismo texto.

CL-COURSE02 compila el candidato final; todos los focos posteriores comprueban sus hashes antes de reutilizar sus DLL. Test DLL SHA-256 `7BB456529B8EC12FC0F4C4115745E1360C3E03ECDFF83FE7370014D25B97EF82`; ruleset DLL `2E0C60C61264AADBA069232A916FFA8735F0D49866E88C5BFC97A736B88B6EC2`. El build se ejecutó con HEAD eb54f7e y el delta de los dos fixtures ahora registrado en8775657. El árbol productivo es idéntico al baseline; no se afirma que el build se realizara después de crear ese commit. CL-COURSE01 y los diagnósticos tienen sus propios hashes; no se confunden con el candidato final.

## Receta

Desde el checkout; las variables representan rutas externas configuradas por quien reproduce. Cada RESULTS debe ser nuevo.

```powershell
dotnet build osu.Game.Rulesets.BmsRuleset.Tests/osu.Game.Rulesets.BmsRuleset.Tests.csproj -c Release "-p:OsuSourcePath=$OSU_SOURCE" --artifacts-path $ARTIFACTS "-p:BmsNativeBrotliToolPath=$NATIVE_TOOL" -p:UseSharedCompilation=false -m:1 -nr:false --verbosity minimal
dotnet test $TEST_DLL --filter $FILTER --logger 'trx;LogFileName=tests.trx' --results-directory $RESULTS -- NUnit.ConsoleOut=0 NUnit.ExplicitMode=None
```

Instrumentación añade antes del separador `--`: `--settings osu.Game.Rulesets.BmsRuleset.Tests/judgement-instrumentation.runsettings`. Filtro exacto: `FullyQualifiedName!~.Performance.&FullyQualifiedName!~Benchmark&TestCategory!=Performance&TestCategory!=Benchmark&TestCategory=JudgementInstrumentation`. Los filtros de Course/localización son FullyQualifiedName~TestSceneBmsCourseResults y FullyQualifiedName~BmsLocalisationTest; los otros se reutilizan literalmente desde LR2-JF y están íntegros en el manifiesto. No se ejecutan benchmarks ni suites sin filtro.

## Validación amplia pendiente

El [CI anterior42656398221](https://github.com/hinobara-ctrl/BmsRuleset/actions/runs/42656398221) tiene los resultados exactos descritos en causa raíz:Windows4508/4515,4 fallos,3 omitidos; Ubuntu4503/4515,1 fallo,11 omitidos; instrumentación288/288 en ambos. Los TRX y metadatos descargados se guardan en una carpeta nueva; hashes y omisiones constan en el manifiesto. El atributo Counters.notExecuted del TRX no sustituye el recuento de UnitTestResult omitidos.

Las omisiones se deben al corpus replay externo ausente, escenarios incompatibles con FFmpeg nativo incluido y controles específicos de CPU/file-sharing de Windows; se conservan sus identidades y motivos. No se reclasifican los41 fallos históricos ni se editan expectativas de otras fases.

Tras publicar los commits normales separados se despachará el workflow ci.yml existente sobre el SHA remoto verificado, conservando sus filtros, exclusiones y runsettings. Ejecutará la suite ordinaria y la instrumentación en Windows/Ubuntu. El recibo externo registrará SHA, comando, run y jobs. **No se ha observado todavía ese CI final**: la reproducción y los focos locales no bastan para afirmar que todos los fallos previos estén resueltos en ambos sistemas. Conforme a la instrucción vigente para ejecuciones largas, al confirmar su lanzamiento termina la intervención sin pollear resultados.

La compilación final conserva54 advertencias NU1902/NU1903 preexistentes; el diagnóstico inicial también muestra CS8629 preexistente. No se modificaron paquetes ni se atribuye al nuevo fixture una reparación de esas advertencias.
