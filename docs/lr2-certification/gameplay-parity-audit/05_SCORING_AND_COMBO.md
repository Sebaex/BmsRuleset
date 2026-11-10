# Score, EX y combo

El oráculo nuevo ejecuta ScoreData original y el prefijo literal `JudgeManager.updateMicro:697–727`, compuesto con GrooveGauge original. En C# ejecuta los procesadores reales. PG/GREAT aportan 2/1 EX; los otros resultados no aportan EX. Los controles mixtos y empty-POOR concuerdan en las secuencias ejercitadas. El score normalizado osu! y sus estadísticas propias no son numéricamente intercambiables con EX-score Java.

GP-002: la mina Java aplica daño y sonido sin pasar por updateMicro; no rompe combo ni aumenta el número de notas juzgadas. C# publica un resultado Meh de mina en el pipeline de score: rompe combo y participa en el progreso del score normalizado. El EX queda igual, pero eso no hace equivalente el scoring completo. PG → mina → PG produce máximos 2 frente a 1.

Existe un contrato histórico C# explícito: `c01-reconciliation/test-modernization/POLICY_EXPECTATIONS.md:91` preserva el corte por mina y el progreso del score. Es una diferencia confirmada respecto a Java, no una autorización para cambiar esa política ni una regresión recién introducida. Requiere decisión del propietario.

GP-005: Java conserva coursecombo/coursemaxcombo entre etapas; C# crea procesadores por etapa y agrega el máximo de los máximos individuales. Dos etapas perfectas de 2 y 3 notas producen 5 frente a 3. U1-D documenta el contrato C# de max por etapa. Cambiarlo exige decidir la semántica Course; sumar máximos individuales sería incorrecto cuando hay cortes.

No se certificaron todas las métricas osu!, accuracy de todos los extremos, clear lamps ni récords persistidos. Si se decide alinear minas, hay que separar penalización/feedback del progreso y combo, preservando eventos, replay, completion y estadísticas deliberadas. Si se decide combo continuo Course, hace falta estado explícito current/max y su reconstrucción; no basta retocar la vista de resultados.
