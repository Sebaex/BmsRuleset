# C01-TM — Cierre de lotes; validación amplia pendiente

## Resultado comprobado

Se reconciliaron las **219 identidades originales** con sus sucesoras: **218 pasan y F099 falla**, mediante ejecuciones focales acumuladas. No es el resultado de una nueva suite amplia. De los170 fallos iniciales,169 tienen una sucesora aprobada:168 mediante adaptación de tests y F133 mediante la traducción productiva autorizada, conservando su igualdad estricta de recursos. Las49 identidades ya reparadas conservan sus cuerpos originales y pasan nuevamente.

Se modificaron40 archivos existentes de tests. Se añadieron6 métodos de prueba y30 casos parametrizados/individuales en total: ventanas5, límites de lifecycle6, herencia6, GAS9, cultura1 y traducción3. No se redujo el número de escenarios. El mapa [original → sucesor](CASE_IDENTITY_MAPPING.csv) conserva cada identidad, su ejecución, resultado y commit; las claves semánticas utilizan las entradas que definen cada escenario, no sus expectativas antiguas. Los cambios de entrada específicos se justifican en [el registro de políticas](POLICY_EXPECTATIONS.md).

**427/427 protegidos aprobados**, con igualdad exacta del multiconjunto de clase/caso/resultado frente a la evidencia previa. TRX anterior SHA-256 `418639D252147D1E067FCEB815139C039B3FCDA65C470CF99C7F7126D6184812`; sucesor `368DAD2713B3127B14E788C092971F1BFD734D47AC72962B174764574C5FEE74`. Las49 identidades previamente corregidas se repitieron con un filtro de76 casos relacionados, todos aprobados. No se modificaron esos49 cuerpos ni sus expectativas.

F099 se repitió aisladamente sin modificarlo: mantiene la aserción GOOD frente a BAD y falla. No se investigó ni alteró el colector heterogéneo. Su contrato C# no dispone del equivalente Java directo que permitiría clasificarlo como defecto de paridad confirmado. Véase [la exclusión](F099_DEFERRED.md).

## Qué continúa protegiendo cada familia

| Lote | Cambio respaldado y protección conservada |
|---|---|
| A | Ventanas LR2 capturadas en Java, límites con ambos signos y vecino inmediato; selección, orden, medidor, progreso, offsets y trazas de integración. |
| B | Margen LR2 cero y grados de tail; reapply instrumentado, misma identidad/acumulador, continuidad HCN, teclado real sin replay, scratch, pooling, defaults, archivos y rewind completo. |
| C | Aritmética de daño/guts/muerte LR2; semillas válidas en2%, estados de cada etapa y ausencia de resurrección o recarga. |
| D | Pipeline Course real, cada control público/interno según su función, todas las cadenas4/5/6, atribución, historial y HUD real en.79/.8/.81. |
| E | Ambos charts, velocidades,192 pares, representación binaria y hashes justificados en U1-A, reforzados con límites físicos calculados independientemente. |
| F | Chart mixto original de20.000 notas y ocho columnas, fórmula de dificultad intacta; GREAT40 y leniency independientes, mismo rigor numérico. |
| G | Tres resoluciones y todas las agregaciones/selecciones posteriores; caso adicional con coma decimal en el hilo real del host y restauración de cultura. |
| H | Paleta exacta aceptada y HUD; igualdad estricta de recursos, traducciones chinas contextuales y retorno a inglés. |
| I | BAD colectivo homogéneo, orden de ticks sensible al umbral real de muerte, secuencias exactas de offsets y repetición de rewind; geometría y colores de los cuatro layouts del visualizador. |

Las pruebas siguen siendo sensibles a las regresiones que motivaron su diseño: una ventana o signo incorrectos, un resultado duplicado, un estado perdido al clonar/retroceder, una cadena incompleta, una atribución errónea o un reinicio de acumulador incumplen aserciones exactas. F140 incluye el control aritmético negativo: invertir el orden cruza por debajo de2% y mata Hard antes de recuperarlo. La documentación distingue ejecución diferencial Java de contratos C# y de equivalencias no certificadas; no basta con que la salida actual coincida con una expectativa.

## Correcciones autorizadas y límites

La prohibición inicial de producción tuvo **dos excepciones expresas**: completar/renombrar tres recursos GAS chinos y alinear Default/Value de GasMode con Easy. El resto de producción permanece intacto. El [informe de GAS](GAS_SERIALISATION_FIX.md) documenta causa, línea cambiada, fallo previo,139/139 tras la corrección y92/92 en persistencia ordinaria/Course. Assist ahora se guarda explícitamente. Los registros antiguos que perdieron esa elección no pueden recuperarla por inferencia.

El helper JSON global de osu! puede producir Settings=null cuando lo omite: se delimita como ruta externa independiente, sin modificar referencias ni declararla corregida. La ruta real ScoreInfo.ModsJson y la conversión APIMod ordinaria se verifican para las tres elecciones. La diferencia temporal HCN de C04 y la paridad completa del jugador Java siguen fuera de lo demostrado. No aparecieron otros fallos sin resolver en los filtros finales; la suite amplia todavía puede revelar otros casos.

## Integridad y evidencias

Los informes, inventarios y fixtures históricos no se reescribieron. Se conservaron todos los intentos fallidos, incluso compilaciones y aserciones exploratorias incorrectas, con directorios separados. El [manifiesto](VALIDATION_MANIFEST.json) enumera los29 intentos, hashes TRX/binarios o ausencia de TRX cuando no se llegó a ejecutar tests; no se sustituyen por repeticiones aprobadas. El [registro de ejecuciones](TEST_EXECUTION_RESULTS.md) explica los resultados sucesores.

Referencias intactas y sin cambios locales: Endless Dream `d4995153b0a759859ebc8f859dd868abae39db5a`, parser `52c5ddcb6bf96a2688c36fee30586c89bfe49f99`, osu! `9a10d935b05a3b896f4bd811d620f2023331db93`, framework `37c3e328c8cc7a018234ec78982ea12d3712096a`. No se tocaron dependencias, presets HTTP, workflows, benchmarks, exclusiones CI ni supresiones de tests. Las advertencias NuGet preexistentes permanecen; esta fase no las reclasifica ni las corrige. No hubo PR, merge, rebase, squash ni force push.

El código comprobado queda en `55e2305b670fecbb9a5e1c3f0a71e496406c0171`; la publicación documental final conserva exactamente sus árboles de producción `380209942ffb62c51ae1b21cd8cdedeecc66002e` y tests `2c172391bd41ff36ac920b8b2fab44139f3cb6c6`. Se entrega DLL Release/net10.0, SDK10.0.401, SHA-256 `D0D51489633E388BEDF3B2E265D25A1E3BB938E2A2E27CE0BBEC14BB00A15306`. Su entrega externa registra procedencia y tamaño; los binarios de diagnóstico no se incorporan al repositorio.

## Validación amplia que sigue pendiente

Se despachará una vez el workflow existente `ci.yml`, activo, con `gh workflow run ci.yml --repo hinobara-ctrl/BmsRuleset --ref fix/lr2-endlessdream-parity-incremental`, después de publicar esta documentación y verificar el SHA remoto. El recibo externo guardará SHA exacto, respuesta de aceptación, run ID/URL y destinos de logs/artefactos. Se finalizará el turno inmediatamente tras comprobar el despacho, sin polling.

CI ejecuta Windows y Ubuntu, Release/net10.0, SDK10.0.x resuelto por setup-dotnet; toma osu!2026.1005.0-lazer. El SDK efectivo y los resultados se verificarán en sus logs, no se presuponen. Los filtros permanecen literales: ordinario excluye Performance/Benchmark/JudgementInstrumentation; instrumentado utiliza los runsettings existentes e incluye JudgementInstrumentation, excluyendo únicamente Performance/Benchmark. Se mantienen visibles F099 y las tres omisiones preexistentes. Artefactos: `test-results-windows-latest`, `test-results-ubuntu-latest` y `bms-ruleset`; TestResults contiene tests.trx y judgement-instrumentation.trx. Los comandos completos están en el workflow inalterado y en los filtros de evidencia.

**Veredicto actual: lotes focales completados; aceptación integral pendiente de resultados amplios Windows/Ubuntu y comparación de identidades.** No se declara CI verde. Una conclusión failure por F099 no basta para clasificar el resto: deben revisarse ambos TRX y las omisiones por identidad al reanudar.

## Commits de implementación y tests publicados

Cada lote se publicó por push normal y se verificó el SHA remoto. El commit documental final y el despacho se identifican en el recibo de entrega para evitar una referencia circular al propio documento.

| SHA | Lote |
|---|---|
| 6d5b6da05e2b2dcc8cd0c5303803f5001cfcaffb | test(difficulty): modernize LR2 leniency and mixed rating expectations [TM-F] |
| e2b2a6248948d65dc53a6b7e0f82db0f669cdd0b | test(course): preserve result integration across host cultures [TM-G] |
| c5c578caed6a9185ace208a91c546bd24bcb6504 | test(lifetime): pair LR2 snapshots with independent interval bounds [TM-E] |
| 15d77ea4b6fd39989b115c3b520713dce81a40e4 | test(presentation): verify approved palette and explicit locale fallback [TM-H] |
| 482ba56430c2f12ccd659d6796818cd550648179 | fix(localisation): complete Chinese GAS settings and description [TM-H-TR] |
| 11cb743a23a7654dd43dc9623756e728aabf44f8 | test(judgement): anchor historical window consumers to LR2 vectors [TM-A1] |
| 72272058e907acc4d9844293d0ebba46f3cb52c7 | test(judgement): retain visual and replay discrimination under LR2 [TM-A2] |
| 97f729226b8d1439ff11bd09882cda233201ec7a | test(gauge): preserve inheritance at valid LR2 survival boundaries [TM-C] |
| 6310f45b9e61198dcb3f0bd1c6d3821c867cb786 | test(gas): validate seeded Course controls and configured graphs [TM-D1] |
| 029a4addd4b950765bd90c5f16858ab93f7cf179 | fix(gas): preserve Assist when serializing the lower limit [TM-GAS-FIX] |
| 9e1dbcfed31bfc5f4b7245b5e04efabf0437d526 | test(gas): preserve real HUD and result integration at clear boundaries [TM-D2] |
| 957639ddff9c6b370902df2d934939562c30398b | test(long-notes): preserve lifecycle checks with zero LR2 release margin [TM-B1] |
| a291813d5eee6f3bed6c76ee8323590927413b88 | test(hcn): reach full reapply assertions after immediate LR2 release [TM-B2] |
| 7d507491e2b26750f95c82634e05b1bdfca61a8f | test(long-notes): retain real input and pooled replay lifecycle coverage [TM-B3] |
| 589b11dc560dabb90228a10dae73703be935bf18 | test(long-notes): preserve full visual consumers with independent LR2 expectations [TM-B4] |
| 8a387e73ab20bf6a3ea65ae01e514f167290fa18 | test(replay): preserve homogeneous BAD traces and LR2 death-boundary ordering [TM-I] |
| 55e2305b670fecbb9a5e1c3f0a71e496406c0171 | test(gas): verify ordinary score persistence for every lower limit [TM-GAS-PERSISTENCE] |
