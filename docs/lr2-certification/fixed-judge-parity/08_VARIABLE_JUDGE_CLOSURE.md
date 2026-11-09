# Cierre de investigación de judge variable — DEFERRED

Decisión LR2-CL/LR2-GP: **el judge fijo es el camino principal de paridad LR2; el judge variable se conserva provisionalmente como extensión de compatibilidad de BmsRuleset**. Se cierra la investigación activa, no el defecto. Estado: **DEFERRED**.

## Contrato y funcionamiento conservados

La clasificación usa las tasas efectivas de todos los heads jugables, no la mera presencia de A0. El parser conserva DEFEXRANK/EXRANK global y los eventos EXRANKxx/A0 ordenados por tick/secuencia; cada head captura su tasa y el tail conserva la de su head. Los eventos redundantes, anteriores/posteriores al contenido o sin heads bajo otra tasa no implican por sí solos un chart variable. [Política vigente](03_FIXED_VARIABLE_POLICY.md).

Con tasas distintas, el engine mantiene la selección previa de compatibilidad; no aplica a esa extensión la corrección de límites anidados para NoGood/NoGreat del camino fijo. Se conservan parser, tasas, procesamiento A0, ventanas por nota, reglas de hold, replay y formatos persistidos. El parser Java fijado no ofrece el mismo contrato temporal por nota: no se declara equivalente ni se inventa un oráculo Java para él.

## Evidencia y defecto pendiente

**JF-VARIABLE-01 permanece confirmado y sin corregir.** Multi-BAD puede utilizar la tabla de una principal de tasa distinta y consumir como BAD adicional una nota que, según su propia ventana, merece GOOD.

Reproducción mínima: Bme7K, Rank3, Duration a1030ms; nota1000ms/tasa.05 y nota1150ms/tasa1. La segunda tiene offset−120ms y GOOD según su propia tabla, pero el colector existente puede consumirla como BAD. [Entradas y evidencia U1-C](research/heterogeneous-reproduction.json), [investigación original](../c01-reconciliation/u1-investigation/U1_C_DURATION.md), [inventario pendiente](PENDING_DEFECTS.json).

Se conservan el [cuerpo histórico exacto de F099](research/F099-original.cs.txt), sus cuatro entradas y dos aserciones, el fallo histórico y la [justificación de sustitución](04_F099_RECONCILIATION.md). Su sucesor fijo respalda selección, pendientes y continuidad; no resuelve el defecto heterogéneo. El control VariablePrimaryRemainsStableWhileDeferredCollectorIsReported verifica el contrato estable de la principal y la propia ventana de la segunda, registrando el resultado defectuoso sin aceptarlo como golden. Los otros controles C# cubren asignaciones, otra columna heterogénea, reconfiguración y replay/prefijo; su aprobación no certifica todos los escenarios variables.

## Motivo y condiciones para reabrir

El proyecto prioriza reglas compartidas de gameplay de judge fijo. Corregir el colector heterogéneo exigiría definir expresamente qué tabla gobierna cada candidato y qué continuidad/replay se garantiza; no corresponde trasladar automáticamente una regla Java global a una extensión temporal propia.

La investigación podrá reabrirse con autorización expresa, un contrato heterogéneo aprobado, reproducción y expectativas independientes por nota, análisis del impacto en selección/LN/replay y pruebas de no regresión del camino fijo. Hasta entonces no se elimina soporte, no se modifica applyMultiBad y no se presenta el aplazamiento como una reparación ni como ausencia de riesgo.
