# Comparación ejecutable Java/C# — LR2-JF

Referencias inmutables y política: [auditoría](01_DESIGN_AUDIT.md), [contrato](03_FIXED_VARIABLE_POLICY.md). Los escenarios no se derivaron de resultados C#. El CSV y su procedencia están en `osu.Game.Rulesets.BmsRuleset.Tests/reference_fixtures/fixed-judge-parity/`.

## Qué se ejecutó

Se compilaron desde los checkouts fijados las clases originales JudgeProperty, JudgeAlgorithm, JudgeManager, GaugeProperty, GrooveGauge, BMSPlayerRule, ScoreData y las clases del parser enumeradas en PROVENANCE.json. El caller verifica que sus clases clave proceden del directorio recién compilado. El JAR auxiliar aporta dependencias, no sustituye esas clases; su SHA-256 obligatorio es `38101C00CB6918FEBB9E0671558AD9F11D1DB842A699AF1EE80CA5BA217BB249`. No se ha identificado su commit de distribución; se fija su contenido por hash y no se ejecuta su player ni sus versiones de los algoritmos recompilados.

`Generate.ps1` extrae literalmente JudgeManager.update:409–456, desde `state.lanemodel.reset()` hasta `multiBadCollector.filter(tnote)`. Hash LF/UTF-8 del fragmento: `86AF4096507AE86CA832D537653948345867E23B75C6C02AEECF699CD3348D44`. Un adaptador mínimo enumera los objetos de la lane; ejecuta el fragmento, JudgeAlgorithm.compare, JudgeProperty.LR2.getJudge y el **MultiBadCollector privado original** mediante reflexión. El contenedor JudgeManager se reserva sin construir el jugador; el colector se construye con su constructor original. Esta dependencia privada está fijada por commit/hash y hará fallar una regeneración incompatible.

No se ejecutan el jugador gráfico, todo update(), updateMicroMultiBad(), ni la aplicación Java de score/health/holds. Los callbacks y estados posteriores se verifican en el runtime C#. Por ello la equivalencia de selección y conjunto/orden del colector es diferencial; la aplicación posterior es una comprobación C#, apoyada por los contratos C03–C05, sin certificación integral del player Java.

536 filas reproducibles. Dos ejecuciones independientes del generador final producen CSV idéntico, SHA-256 `1F15C9842DE78B33D0C44A1D3281DF554FD54BE871A9682E1316A2AEB1533700`. Los mensajes stderr del colector sobre una principal ya juzgada se conservan en evidencia; no contaminan el CSV. La suite exige exactamente536 filas y no omite filas fallidas.

## Matriz de evidencia

`selection.csv` es la matriz detallada: cada fila contiene identificador, rank/EXRANK, scratch, mod, algoritmo, microtiempos, tipo N/L, máscara de juzgados, índice principal, juicio Java y extras ordenados. `BmsFixedJudgeJavaParityTest.SelectionAndRuntimeApplicationMatchOriginalJavaComponents` comprueba principal/juicio/emptyPOOR, identidad y orden de callbacks, cada BAD adicional, ausencia de hold en LN adicional y todos los objetos consumidos o pendientes.

| Entrada / propiedad | Fuente independiente | Resultado / comparación | Nivel y límite |
|---|---|---|---|
| Ventanas y extremos normales/scratch/head/tail | C02:126 vectores originales JudgeProperty; C06 conserva sus hashes | Tests originales siguen aprobando; no se cambiaron tablas ni fixtures C02–C06 | Java original ejecutado, dominio finito |
| Cuatro algoritmos y nueve colectores anteriores | C02/C06 JudgeAlgorithm y MultiBadCollector | Reutilizados en el foco protegido; sin cambios de fixtures | Java original ejecutado; no player completo |
| F099 fijo:900/1000/1060/1150, Rank3, press1030 | Fragmento original + colector; filas f099-fixed | Combo/Duration/Score: principal1000 GREAT, extra900 BAD, vecinos pendientes. Lowest:900 BAD, sin extras | Java original ejecutado + aplicación C# |
| Empates, simultáneos, GOOD/BAD, primary BAD, LN anteriores/posteriores, juzgados y emptyPOOR | Filas tie-right, stacked, good-neighbour, late-long, long-extra, long-primary, judged, empty-* y bad-* | Coinciden principal, extras y orden; se mantienen pendientes no recolectados | Java original ejecutado; cada fila limita su propio dominio |
| Normal/scratch × No Good/No Great/ninguno × cuatro algoritmos | JudgeProperty:224–260; selección:409–456; vectores parametrizados | Baseline diverge en26 casos de la matriz488. Corrección: todos536 vectores finales coinciden | Diferencia confirmada antes; coincidencia diferencial después |
| LN positiva+10/+30 y filas desactivadas | positive-long-pg/great | Admisión y resultado coinciden; ningún LN adicional inicia hold | Head Java ejecutado; estado hold posterior solo C# |
| Rank0–4 y DEFEXRANK60/87/100/143/200 | BMSPlayerRule + JudgeProperty originales, filas rank/exrank | Respeta rank real y escalado no estándar; normal/scratch, cuatro algoritmos | Java original ejecutado para esas entradas |
| NoGreat, notas1000/1060, press1050/1060 | modified-future/modified-at | Combo/Score eligen1060 PG; sin mod a1050 eligen1000 GREAT | Java original ejecutado; reconcilia una expectativa protegida |
| Segunda/tercera pulsación, pasivo posterior, combo/EX/gauge/replay de F099 fijo | Contratos C03–C05 y sucesor F099 | Cuatro resultados únicos; EX5, combo3 y health.25; offsets130/30/0/0 | Solo C# para la continuación; primera pulsación diferencial |
| Eventos redundantes, A0 real, evento durante LN, charts vacíos, replay variable | Semántica existente del parser C# y política de proyecto |14 casos de asignación y replay/prefijo sin alterar tasas | Solo C#; parser Java fijado no ofrece equivalente temporal |
| Reconfiguración de tasas y heterogeneidad en otra columna | Contrato C# de RefreshNoteConfiguration | Cambia contexto únicamente al cambiar tasas; conserva desempate variable previo | Solo C#; no certifica el defecto heterogéneo |
| HCN bajo saltos de frame | C04, BmsHcnFrameReferenceTest | Se conserva catch-up C# aceptado, distinto del límite Java por frame | Diferencia intencional ya documentada; no resuelta aquí |
| Gameplay completo, todos los mods/skins/chart rates posibles | No existe oráculo integral ejecutado | No determinado fuera del dominio documentado | No afirmar paridad LR2 integral |

Los records idénticos de la API pública se comparan por valor; su identidad no está representada en esa API. El callback real del runtime comprueba los índices de los objetos originales y su orden, por lo que los candidatos simultáneos no quedan validados únicamente por igualdad de valores.
