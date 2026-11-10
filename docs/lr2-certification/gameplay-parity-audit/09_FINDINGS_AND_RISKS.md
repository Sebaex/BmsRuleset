# Hallazgos y propuestas para revisión

Todos los resultados siguientes corresponden al baseline C# `afa95e3a5d9881adbf29e1f89fd61329173ccc29`, Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a` y, cuando hay parsing, jbms-parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`. `SOURCE_INDEX.json` permite verificar los archivos y las matrices completan las referencias. No se ha implementado ninguna propuesta. Severidad indica impacto potencial demostrado, no frecuencia de ocurrencia en una biblioteca de canciones.

## GP-001 — P1: aritmética acumulada altera muerte y qualification

**Confirmado, confianza alta.** Java `play/GrooveGauge.java:218–250,263–284` conserva porcentajes float; C# `Gameplay/Scoring/Gauge/BmsGaugeCalculator.cs:45–65,99–113` calcula en double normalizado y `BmsGaugeProfile.cs:24–29` sólo castea el umbral final. HealthProcessor aplica el resultado y decide pass (`:262–280,336–379`).

Entrada: Hard/ExClass, 1.000 notas, TOTAL 300, seed 20%, cinco BAD. Java: 16,3999996185 → 12,7999992371 → 9,1999988556 → 5,5999984741 → **0**. C#: 16,4 → 12,8 → 9,2 → 5,6 → **2,0000000000000004**. PG posterior deja Java muerto y C# en 2,1%. Class reproduce el desacuerdo tras quince BAD. Son procesadores C# y gauges Java reales, no cálculo manual de la expectativa.

Normal, TOTAL 200, 1.000 notas, 300 PG desde 20%: qualification Java false a 79,99992370605469%, C# true aproximadamente a 80%. Es un prefijo, no prueba de clear de una canción entera. Los controles TOTAL 300 y mixto no divergen en decisiones.

Causa: orden y precisión de operaciones; fórmulas algebraicamente equivalentes no garantizan iguales transiciones de muerte. Propuesta: emular la secuencia LR2 float porcentual, incluyendo gain/rate/guts antes del clamp; preservar almacenamiento público si es posible. Riesgo medio/alto por propagación a GAS/Course/HCN/replay. Aceptación: oráculos C03 más los nuevos límites, rate/guts, no-revivir, ambas familias y carry. No introducir tolerancia en supervivencia. Estado: **PROPUESTO, autorización pendiente**.

## GP-002 — P2: mina entra al score y rompe combo

**Diferencia confirmada; contrato C# documentado, decisión pendiente.** Java `JudgeManager.java:266–272` sólo resta daño y emite sonido; no llama a updateMicro. C# `Rules/BmsLandmineJudgementRule.cs:22–42` publica Meh; `Scoring/BmsScoreProcessor.cs:218–243` rompe combo y el ScoreProcessor osu! cuenta el resultado.

Normal, 2 notas/TOTAL 12, seed 80%, PG → mina de 20% → PG: ambos gauges 86 → 66 → 72% y ambos EX 2 → 2 → 4. Java combo 1 → 1 → 2; C# 1 → 0 → 1. Score osu! avanza 333333 → 666667 → 1000000, incluso en el evento de mina. La escala osu! propia no es en sí un defecto de paridad EX.

Autoridad C# existente: `test-modernization/POLICY_EXPECTATIONS.md:91` conserva expresamente el corte por mina. No se cambia esa decisión por iniciativa de auditoría. Si se elige alineación LR2, separar penalización/feedback de minas del progreso/combo estándar; restaurar únicamente combo no resolvería todos los efectos. Riesgo medio/alto: estadísticas, completion, ledger, replay y score. Tests: combo positivo, mina evitada, varias minas, morte, Normal/Hard, NoMine, autoplay y seek. Estado: **DECISIÓN DE CONTRATO**.

## GP-003 — P1: orden de teclas simultáneas cambia supervivencia

**Diferencia confirmada en replay C#; orden Java respaldado estáticamente.** Java `JudgeManager.java:365–503` recorre keyassign ascendente; `LaneProperty.java` fija el orden 7K (validado ejecutando su clase original). C# `BmsJudgementEngine.cs:122–174` conserva inserción y `BmsReplaySimulation.cs:79–135` deriva transiciones del frame.

ExClass, 1.000 notas/TOTAL 300, seed float Java 5,55% (5,550000190734863), una nota de columna 1 recibe PG y otra de columna 2 BAD al presionar ambas a 1000 ms (heads 1000 y 850). Las otras 998 notas están después. Java PG → BAD sobrevive en 2,049999952316284%. Replay C# `[Key1,Key2]` sobrevive; `[Key2,Key1]` procesa BAD → PG y queda muerto. El input inicial, gauge y conjunto de teclas coinciden; sólo cambia la serialización del conjunto dentro del frame.

Se ejecutaron gauges/score Java originales con el orden demostrado, no el loop completo de entrada Java. No se afirma que todo replay real genere la permutación; sí que un frame válido la reproduce.

Propuesta: prioridad canónica de transiciones simultáneas en el engine compartido, respetando orden real de timestamps y release/repress. Riesgo alto si se ordena sin modelar scratch y holds. Tests: ambas permutaciones, live/replay/seek, acordes, BAD/Multi-BAD y muerte. Estado: **PROPUESTO, autorización pendiente**.

## GP-004 — P1: minas y juicios del mismo instante tienen fases opuestas

**Confirmado por ejecución C# y composición Java; phase order Java estático.** Java `JudgeManager.java:240–270` procesa minas antes del recorrido de teclas `:365`; C# engine avanza excluyendo el instante y ejecuta ProcessAutomatic después de inputs (`:151–174`).

ExClass, seed float 2,45% (2,450000047683716), 1.000 notas/TOTAL 300, PG y mina de daño lógico 0,5% al mismo instante, ambas teclas presionadas. Java mina primero cruza el límite 2% y mata; PG no revive. C# publica `Note|Landmine`, PG antes del daño deja 2,0500000476837157% vivo, en ambas permutaciones. Control seed 10%: ambos sobreviven a aproximadamente 9,6%.

Es daño lógico asignado equivalente, **no el mismo token BMS**; el parsing distinto se separa en GP-006. Propuesta: fase de minas/passing alimentada por snapshot prospectivo de teclas. No mutar anticipadamente PressedDirections: podría hacer que release real no procese el tail. Riesgo alto: prioridad de cuerpos HCN, tails, minas exactas y saltos de frame. Tests: presión/liberación en la mina, tecla ya sostenida, scratch, tail simultáneo, replay/seek y controles sin muerte. Estado: **PROPUESTO, autorización pendiente**.

## GP-005 — P2: máximo combo de Course no es continuo

**Diferencia confirmada; contrato C# documentado.** Java `JudgeManager.java:216–217,716–725` recupera y actualiza coursecombo/coursemaxcombo; BMSPlayer conserva esos valores y CourseResult usa el máximo continuo (`CourseResult.java:256`). C# `BmsCourseGameplay` crea Player por etapa, Session transporta gauges/mods; `BmsCourseScoreAggregation.cs:35` usa máximo de máximos individuales.

Dos etapas de 2 y 3 PG, sin cortes: original counter slice con carry leído en init produce 5; Session/procesadores/aggregator reales C# producen 3. No se ejecutó el Course Java completo. U1-D (`U1_D_COURSE_RESULTS.md:21`) documenta agregación C# por etapa.

Propuesta condicionada a decisión: estado explícito continuo current/max entre etapas y en reconstrucción; no sumar máximos. Riesgo medio/alto: scores guardados, abortos, replay y UI. Tests con corte en principio/final, empty-POOR, mina, LN tails, stage abortado y restore. Estado: **DECISIÓN DE CONTRATO**.

## GP-006 — P1: mismo BMS aplica la mitad del daño de mina

**Confirmado, confianza alta, ambos parsers ejecutados.** jbms-parser `Section.java:491` crea MineNote con el token base36 sin dividir; `MineNote.java:15–33` conserva damage; Java lo resta en `JudgeManager.java:269`. C# `BmsParser/BmsParser.cs:1216–1230` divide por 2; decoder conserva ese valor y HealthProcessor `:350–365` normaliza a fracción.

Texto idéntico, ASCII:

```text
#PLAYER 1
#BPM 120
#RANK 3
#TOTAL 300
#00118:01
#001D2:0A
```

La nota normal determina 7K y el conteo; mina en columna 2 a 2000 ms, tecla sostenida desde 1990. Oráculo Java original BMSDecoder → validation → MineNote → GrooveGauge; C# decoder → converter → replay real. Tokens 01/0A/10 producen daño Java 1/10/36%, C# 0,5/5/18%. ExClass seed 11,5% y token 0A: Java cae bajo 2% y muere; C# conserva 6,5%. Replay termina a 2001, antes del fallo pasivo de la nota normal no pulsada. 01 y 10 controlan ambos extremos sin cambiar la decisión de vida entre implementaciones, aunque el daño siga distinto.

Esto prueba la paridad con **estas referencias fijadas**, no la interpretación universal de todas las especificaciones BMS. No se encontró una decisión explícita aceptando el daño a mitad como excepción LR2.

Sí existen expectativas históricas de mitad de daño: `Normal/BeatmapTest/Decoding/BmsBeatmapDecoderTest.cs:494,511–512,528,978,998` fija 5% para 0A, 647,5% para ZZ y 25% para 1E. Sus métodos incluyen TestBase62LandmineChannelWithLowercaseIsUnaffected, TestBase62LandmineChannelZzIsInstantDeath y TestDecoderParsesLandmineChannels. No se modificaron. Demuestran el contrato actual de decoding C#, pero no justifican por sí solos una excepción a la referencia Java según la jerarquía autorizada. Una corrección necesitaría revisar estas aserciones con la evidencia independiente y autorización, conservando el resto del escenario.

Propuesta mínima: retirar el divisor en la conversión de minas nativas, tras aprobación; no mezclar con orden de fases GP-004. Riesgo medio: charts previamente importados, replay/score asociados, base36/base62 y metadata convertida. Antes de implementar, verificar si el formato persistido recalcula desde líneas originales o conserva damage y definir compatibilidad. Tests mismos tres textos, muerte, Normal/Hard, NoMine, autoplay, Course y replay. Estado: **PROPUESTO, autorización pendiente**.

## Riesgos no demostrados y exclusiones

No se elevan a defectos nuevos: LN release tardío frente a tail automático; cobertura completa de GAS; redondeos temporales complejos BPM/STOP; layouts no ejercitados; MOD-02 heredado sin reproducción actual. La diferencia HCN de catch-up es intencional aceptada. Judge variable conserva JF-VARIABLE-01 `DEFERRED`; no se reabre. No se reclasifican los fallos históricos, F099 ni las advertencias NuGet. No se investigó transporte de tablas ni los pendientes de notificaciones P4.
