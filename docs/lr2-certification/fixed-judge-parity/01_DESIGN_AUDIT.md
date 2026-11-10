# LR2-JF — Auditoría de diseño y consumidores

Baseline local/remoto limpio: `b84d77d56814c3ae9ab95de02102fbcfd4e9a8bc`. La corrección independiente del fixture Course se publicó en `752d7699cc55a0e80e8254b6537e45c8edb21025`. Referencias: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`, osu! `9a10d935b05a3b896f4bd811d620f2023331db93`, framework `37c3e328c8cc7a018234ec78982ea12d3712096a`.

## Flujo y alcance

Parser/header/A0 → BmsParsedHitObject.JudgementRate → decoder → BmsHitObject.EffectiveJudgementRate → proveedor de tablas + mods → selector/colector y reglas pasivas/LN. Engine mantiene orden temporal; gameplay y reconstrucción de replay usan ese mismo núcleo. Presentación, audio y dificultad consumen proyecciones de estas ventanas y no deben convertirse en otro motor de juicio.

La búsqueda inicial encontró 81 referencias directas a tasas, GetTable, ventanas pasivas y mods; las dependencias indirectas de registro, configuración y reconstrucción se revisaron además. Rutas de esta tabla relativas a `osu.Game.Rulesets.BmsRuleset`.

[Anclas exactas de archivo/línea del árbol final](research/window-consumers-final.txt): búsqueda reproducible con `rg -n 'JudgementRate|EffectiveJudgementRate|GetTable\(|BmsNoteJudgementWindows|UseWindowMods|SetActiveWindowMods|ClearActiveWindowMods' osu.Game.Rulesets.BmsRuleset -g '*.cs'`. Sus75 coincidencias usan un patrón distinto del inventario inicial; la tabla siguiente incluye también consumidores indirectos y métodos de lifecycle, no solo esas coincidencias.

| Consumidor / responsabilidad | Procedencia y actualización | Contexto / equivalente Java | Riesgo y decisión |
|---|---|---|---|
| BmsParser/BmsParser.cs: collectJudgementRateEvents, initialJudgementRate, applyJudgementRates | Header inicial; A0 ordenado por tick/secuencia; captura la última tasa en cada head durante parse | Global y por objeto. Java BMSDecoder/BMSPlayerRule valida DEFEXRANK global; este parser fijado no implementa EXRANKxx/A0 equivalente | Alto: no modificar asignación ni eventos |
| BmsParser/BmsParseResult.cs: BmsParsedHitObject y ShiftedBy | Campo de tasa se conserva al desplazar tiempos | Datos intermedios C# | No modificar formato/defaults |
| Beatmaps/BmsBeatmapDecoder.cs:64 | Copia la tasa parseada a cada hit object | Puente a gameplay | No modificar |
| Beatmaps/Objects/BmsHitObject.cs:20–26 | Tasa explícita; si NaN, ExRank o Rank del beatmap; CreateHitWindows usa esa tasa | Por head; BMSPlayerRule + tablas de JudgeManager | No modificar fallback ni API |
| Gameplay/Judgement/BmsJudgementProfileProvider.cs | Cache por layout/tasa y mods; GetTable distingue normal/scratch/head/tail; scopes de replay y mods activos | Cuatro tablas de JudgeManager:196–201; valores C02 ya ejecutados | Alto: conservar perfiles, cache, tasas y mods |
| BmsJudgementProfile.cs / BmsJudgementWindowTable.cs | Cuatro tablas; consultas firmadas/inclusivas y deadline estricto | JudgeProperty y comparaciones Java | Conservar valores y límites |
| BmsJudgementSelector.cs:16,63,166 SelectPress/PressSearch/selectionWindow | Consulta tabla de cada candidato al pulsar; comparación y corte anticipado | JudgeManager.update:409–456 y JudgeAlgorithm.compare | Diferencia confirmada con filas anuladas por mods; se normalizan únicamente sus límites de selección en contexto fijo |
| BmsColumnJudgementRuntime.cs: selectPress/applyMultiBad | Tasa por candidato; colector reutiliza tabla de la principal; filtrado y aplicación preceden a principal | MultiBadCollector y updateMicroMultiBad | Uniforme: tablas equivalentes. Heterogéneo: defecto conocido separado. Mantener applyMultiBad intacto |
| BmsColumnJudgementRuntime.cs:166 RefreshNoteConfiguration, registros e índice | Reconstruye ventanas pasivas o configura holds cuando cambian defaults/tasa | Contrato C# de reapply/configuración | Recalcula el contexto fijo en esta ruta existente solo ante cambios de tasa |
| BmsJudgementEngine.cs | Recibe todos los objetos/columnas; ordena inputs, deadlines, pasivos y body ticks | Orden de update de JudgeManager; catch-up HCN C# de C04 | Conservar simulación. Lugar mínimo para conocer homogeneidad del contexto registrado |
| BmsNoteJudgementRule.cs / BmsNoteJudgementWindows | BAD pasivo capturado al registrar/configurar; expira por resta estricta | JudgeManager:642 y siguientes | Conservar; BAD fijo y NoBad mantiene deadline |
| State/BmsHoldRuntime.cs / BmsHoldRegistry.cs | Guarda head/tail al construir y RefreshWindows; tasa capturada del head; margen0 | Cuatro tablas/márgenes Java; estados C# | Conservar LN/CN/HCN, reapply y cuerpo independiente |
| Rules/BmsLongNoteJudgementRule.cs, BmsChargeNoteJudgementRule.cs, BmsHellChargeNoteJudgementRule.cs | Consumen ventanas del hold para release, passive y finalización | Ramas LN/CN/HCN originales; no todas ejecutadas diferencialmente | Alto: ninguna modificación prevista |
| Framework/BmsHitWindows.cs | Ventanas del proveedor al construir/SetDifficulty; proyección para framework | Adaptador C#, sin equivalente estructural exigible | Conservar |
| UI/Gameplay/BmsPlayfield.cs y BmsDrawableRuleset.cs | Engine con todas las columnas; mods registrados antes de uso, retirados al disponer | Driver de input/presentación | Conservar frontends y lifecycle |
| Gameplay/Replays/BmsReplaySimulation.cs | Engine completo y scope de mods por reconstrucción; input/clock/replay deterministas | No original Java replay ejecutado | Conservar storage/driver; validar estados posteriores |
| UI/Gameplay/Judgement/BmsJudgementBridge.Lifecycle.cs | Reapply solicita RefreshNoteConfiguration existente | Representación/estado C# | Conservar integración; cubrir invalidación de contexto |
| Gameplay/Mods/BmsModNoGood.cs, BmsModNoGreat.cs, BmsModNoBad.cs | Transformaciones finitas de tablas; rows de resultados anulados quedan0; NoBad conserva deadline | NO_GOOD/NO_GREAT Java también anula, pero luego anida umbrales; NoBad es extensión C# | No cambiar clases de mods: afectaría al judge variable y UI |
| Gameplay/Mods/BmsModInvert.cs | Copia JudgementRate al crear objetos transformados | Extensión C# | Conservar |
| UI/Gameplay/BmsGameplayAudioController.cs | Consulta BAD temprano/tardío por objeto al programar muestras | Presentación/audio | Conservar; no cambiar volumen ni scheduling |
| UI/Gameplay/BmsHitObjectLifetimeEntry.cs y Drawables/Objects/Presentation/BmsLongNotePresentation.cs | Ventanas por objeto para lifetime/presentación, recalculadas con defaults | Contrato visual C# | Conservar |
| UI/HudComponents/BmsHitErrorMeter.cs | Primera nota jugable para dominio; tablas de head/tail para display | UI C#; dominio variable completo no certificado | Conservar |
| Gameplay/Difficulty/BmsHitLeniency.cs, BmsStarRatingProcessor.cs | GREAT de contexto global/archivo o primera tasa durante preprocess | Dificultad propia C# | Conservar; no rediseñar rating variable |
| BmsRuleset.cs: métricas de beatmap | Cuatro categorías por tasa global de atributos | UI C# | Conservar |
| UI/Result/Course/BmsCourseAggregateStatistics.cs, HudComponents/BmsScoreGraph.cs, Result/Statistic/BmsStatisticsPanel.cs | Scopes de mods para simulación/estadísticas | Reconstrucción C# | Conservar |

## Diferencia acotada demostrada antes de modificar producción

`JudgeProperty.java:251–260` aplica los porcentajes de mods y anida cada fila al menos en la anterior. Bajo No Good, GOOD queda tan amplio como GREAT; bajo No Great, GREAT/GOOD quedan al menos en PGREAT. C# anula esas filas con0: los resultados finales por offset siguen iguales porque primero se consultan las filas válidas, pero `PressSearch` usa literalmente GOOD/ GREAT para comparar, cortar búsqueda y rechazar heads LN tardíos. No son representaciones observacionalmente equivalentes en esas operaciones.

El fragmento original y colector ejecutados confirman: dos heads1000/1000, press1030, Rank3, No Good+Combo → Java consume el primero; C# baseline consume el segundo. La matriz se amplía con heads LN a+10/+30 para delimitar la admisión. Una comprobación inicial de identidad del selector público confundía records idénticos: se corrige comparando su valor; la identidad real permanece verificada mediante callbacks del runtime. Ese problema del fixture se distingue del fallo productivo anterior.

Corrección implementada: conservar todas las tablas y los mods; usar límites de selección anidados solo en contexto homogéneo. Un booleano de homogeneidad del engine se comparte con sus columnas y se recalcula mediante RefreshNoteConfiguration. No hay segundo motor, estrategia, servicio ni cache de ventanas nueva. La política variable conserva literalmente los umbrales previos. El selector público puede determinar homogeneidad del conjunto completo de candidatos que recibe; el gameplay usa el contexto completo registrado en el engine, incluyendo otras columnas. Los controles de homogeneidad entre columnas e invalidación al reconfigurar están aprobados. Detalles y coste de la contabilidad de invalidación en [implementación](05_IMPLEMENTATION_REPORT.md).
