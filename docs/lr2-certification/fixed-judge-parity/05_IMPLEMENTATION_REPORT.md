# Implementación y límites — LR2-JF

## Corrección productiva demostrada

No Good/No Great anulan filas C# que Java mantiene anidadas en la anterior. El resultado por offset coincidía, pero el selector utilizaba el0 literal como umbral. La evidencia previa JF-JAVA03-BASELINE contiene26 divergencias frente al fragmento/colector original. No se corrigió por similitud estructural: había diferencias observables en principal, admisión de LN y consumo posterior.

| Archivo | Cambio y justificación |
|---|---|
| Gameplay/Judgement/BmsJudgementSelector.cs:27,63,94,124,144,166 | Detecta uniformidad del conjunto público de candidatos; PressSearch recibe contexto fijo del engine. Solo los límites usados en selección, corte y admisión LN se anidan con max(PG,GREAT,GOOD), como JudgeProperty:251–260. Tasas variables usan literalmente los umbrales anteriores. La firma pública SelectPress no cambia. |
| Gameplay/Judgement/BmsJudgementEngine.cs:32,43,78 | Determina homogeneidad de todos los heads registrados, incluidas otras columnas. Conserva las referencias de conjuntos distintos para que el frontend que repite el array completo no lo escanee una vez por columna. Mantiene un único núcleo y sus fases temporales. |
| Gameplay/Judgement/BmsColumnJudgementRuntime.cs:24,72,83,166,253 | Comparte el contexto del engine y registra la tasa de cada head. RefreshNoteConfiguration invalida el contexto solo ante un cambio real; reapply/pooling habitual no provoca escaneos completos por cada nota. applyMultiBad:269 permanece idéntico. |

No se añade cache de ventanas ni otro motor. El array de tasas es contabilidad de invalidación:8 bytes por head más cabecera/referencias, además de un escaneo inicial O(N). Un cambio real de tasa provoca un nuevo escaneo O(N); reapply sin cambio conserva comprobación O(1). Un lote de K cambios/refrescos reales puede costar O(KN), incluso al cambiar una propiedad global compartida: no se introduce infraestructura de batching para ese escenario fuera del flujo habitual. No se ejecutaron benchmarks ni se certifica rendimiento fuera de estas propiedades estructurales. Comparar tasas efectivas exactas conserva la clasificación de los valores ya asignados; no se aproxima una tasa no estándar a un rank.

El registro debe contener el chart jugable completo. BmsPlayfield:91 pasa el array completo a las columnas; BmsReplaySimulation:64 entrega conjuntos por columna que juntos contienen el chart completo. Cambiar tasas sin RefreshNoteConfiguration sigue fuera del contrato existente de reconfiguración. El helper público sin contexto de chart solo puede clasificar el conjunto recibido; gameplay usa el contexto completo y no llama a ese helper público.

Se descartaron: normalizar globalmente las tablas/mods (alteraría variable y consumidores), dos motores/servicios (innecesarios), reescribir Multi-BAD (sin divergencia homogénea demostrada), y recalcular todo el chart en cada pulsación o reapply (coste innecesario). No se alteran parser, asignación de tasas, reglas LN/CN/HCN, deadlines pasivos, replay/storage, audio, gauges, Course productivo, presets, dependencias, workflow ni runsettings. Las referencias permanecen de solo lectura.

## Pruebas y reconciliaciones

| Archivo | Razón y respaldo |
|---|---|
| Tests/Normal/Gameplay/Judgement/BmsFixedJudgeJavaParityTest.cs |536 vectores originales comparados con selector y runtime real; controles variables entre columnas, invalidación y principal estable del defecto pendiente. |
| Tests/Normal/Gameplay/Judgement/BmsJudgeRateCompatibilityTest.cs |14 escenarios semánticos de asignación global/temporal, replay variable y tres continuaciones CN con mods. Expectativas de parser proceden del contrato C# aceptado; heads modificados proceden de positive-long-* Java; tails/replay son continuidad C#. |
| Tests/Normal/Gameplay/Judgement/BmsColumnNoteIndexTest.cs |Sustituye F099 por el escenario fijo confirmado en Java; añade continuidad, score/gauge y replay. [Trazabilidad](04_F099_RECONCILIATION.md). |
| Tests/Normal/Gameplay/Judgement/BmsJudgementSelectorTest.cs:137 |Reconcilia TestAlgorithmsUseModifiedWindows con modified-future/modified-at Java:1060 PG, en lugar de1000 BAD; conserva identidad, inputs, algoritmos, restauración y añade control sin mod. [Original](research/TestAlgorithmsUseModifiedWindows.baseline.cs.txt). |
| Tests/osu.Game.Rulesets.BmsRuleset.Tests.csproj |Añade exclusivamente el EmbeddedResource del nuevo CSV. |
| Tests/reference_fixtures/fixed-judge-parity/* |Caller/generador,536 filas y procedencia de fuentes fijadas; sin binarios, corpus ni rutas personales. |
| Tests/Visualize/TestSceneBmsCourseResults.cs |Commit independiente752d769: utiliza locale registrado es, controla cultura efectiva/propagación al update thread, localización real frente a representación invariante y restaura estado. Conserva secuencia original, estadísticas, stages y resolución. |

Course: LocalisationManager del framework fijado valida los locales registrados y revierte es-CL, que no está registrado. La máquina local heredaba coma decimal y ocultaba ese error de fixture; ambos CI del baseline fallaban. El control en primero demuestra el estado decimal de partida y el posterior es aceptado demuestra el cambio real. No se prolonga la espera ni se cambia agregación Course.

Los427 casos protegidos conservan identidades y resultados. **No significa427 cuerpos intactos:**426 conservan sus aserciones y uno recibe la reconciliación Java anterior. F099 es otra identidad, fuera de esos427; se sustituye explícitamente. No hay modificaciones para consagrar el BAD heterogéneo conocido.

## Compatibilidad y publicación

No cambia ninguna API pública ni formato de archivo/replay. La reconstrucción de replays de judge fijo con NoGood/NoGreat afectados puede cambiar de resultado al corregir su selección: es una consecuencia observable y deliberada. La extensión variable conserva sus límites y colector previos; los controles son finitos, no una garantía universal sobre todas sus combinaciones.

Los commits de pruebas y producción son separados y dependientes. El commit de pruebas contiene expectativas Java que son rojas contra el baseline; el siguiente corrige la divergencia. Se publican juntos con el informe y validación del árbol combinado, no se presenta cada estado intermedio como validado verde. Course tiene su propio commit previo. Documentación y matrices sucesoras se publican aparte. No se abre PR ni se modifica historia compartida.

Los históricos C01–C06/C01-TM se conservan; solo índices y fila F099 de registros activos reciben enlaces sucesores. El manifiesto nuevo autentica código, fixtures y evidencias sin incluir rutas personales. [Validación](06_VALIDATION_REPORT.md), [veredicto](07_FINAL_VERDICT.md).
