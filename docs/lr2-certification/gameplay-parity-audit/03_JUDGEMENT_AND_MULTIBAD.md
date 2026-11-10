# Judgement y Multi-BAD

Se reutilizan las tablas/ranks y selección de C02/C06 y los 536 vectores originales de LR2-JF: selección fija NoGood/NoGreat, collector y condiciones de consumo. La cifra no certifica todas las intercalaciones del jugador. Las suites C# focales previas y sus identidades están en los manifiestos enlazados; no se cambiaron expectativas para igualar C#.

La lectura siguió `JudgeManager.update:409–503` (selección → collector → BAD adicionales → principal), `updateMicro:696–748`, y las contrapartes engine/columna/hold registry/output/score/health. La tabla correcta debe usarse para seleccionar y para consumir; los BAD adicionales pueden afectar estado de holds y vida antes del principal. Esta auditoría conserva la política fija LR2-JF. El camino heterogéneo `JF-VARIABLE-01` sigue fuera del cierre de paridad, sin cambios en `#A0` ni en applyMultiBad.

El nuevo control `mixed` ejecuta PG/GREAT/GOOD/BAD/POOR/empty-POOR/PG y compara combo, EX y estados de vida después de cada evento. `empty-poor-positive-combo` comprueba que empty-POOR conserva un combo positivo. Son controles del procesamiento de resultados, no una ejecución nueva del selector Java completo.

GP-003 es una diferencia de integración: Java recorre teclas por prioridad de `keyassign`, mientras C# procesa el orden del lote encolado. Un replay equivalente como conjunto de teclas, con orden inverso, puede matar donde Java sobrevive. No se atribuye este resultado a una tabla incorrecta.

Cobertura parcial: no se ejecutaron todas las combinaciones de layouts, algoritmos, holds y Multi-BAD integrado contra un jugador Java completo. F099 permanece excluido; no se ejecutó de nuevo su fixture histórico ni se modificó su sustitución.
