# Correspondencia de flujos

`GAMEPLAY_PARITY_MATRIX.csv` contiene las responsabilidades, estados iniciales, transformaciones, observables, fuentes con líneas y commits y decisiones. `SOURCE_INDEX.json` añade blob Git y SHA-256 canónico LF/UTF-8 de cada archivo referenciado.

Java concentra el update en `JudgeManager.update`: notas que pasan y minas, cuerpos HCN, recorrido de teclas, selección/Multi-BAD, liberaciones y vencimientos. `updateMicro` actualiza ScoreData/combo y llama a `BMSPlayer.update`, que aplica el gauge. El caller decide muerte y GAS; Course conserva gauge y contadores en PlayerResource. Por tanto, certificar tablas o fórmulas no certifica su orden integrado.

C# distribuye esas responsabilidades entre parser/converter, provider de ventanas, engine, columnas/registro de holds, reglas LN/CN/HCN, output de judgement, procesadores de vida/score y consumidores live/replay/Course. `QueueInput` conserva el orden de inserción; `ProcessFrame` adelanta eventos anteriores, aplica el lote igual-tiempo y después los automáticos del instante. Ese orden es relevante para GP-003 y GP-004.

La simulación de replay aplica score antes de health. El Player live aplica health antes de score. Esta diferencia arquitectónica no se clasificó como defecto: `Player.PerformFail` pospone la conclusión del score y el scope de reconstrucción aísla eventos/failure. No se demostró un estado final distinto por ese motivo aislado.

Los resultados ejecutables anteriores se citan como **reutilizados**, no como nuevas ejecuciones. Los nuevos controles no sustituyen las certificaciones completas de C02–C06/LR2-JF. La matriz desglosa responsabilidades precisamente para impedir que un caso positivo certifique una clase entera.
