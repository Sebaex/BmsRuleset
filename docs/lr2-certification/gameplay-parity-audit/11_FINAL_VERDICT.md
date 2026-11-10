# Veredicto LR2-GP

**PARIDAD PARCIAL; NO CERTIFICACIÓN INTEGRAL.** La auditoría identifica seis diferencias reproducibles, cuatro P1 y dos P2 sujetas a contratos C# previos. Producción permanece intacta. LR2-CL no condiciona este diagnóstico y sus resultados de CI/localización no se dan por resueltos aquí.

1. Se revisaron gauges/muerte, selección/Multi-BAD, LN/CN/HCN, scoring/combo, mods, orden temporal, replay y Course, siguiendo callers y efectos. La cobertura por ruta está en COVERAGE_MATRIX.
2. Las tablas y selección fija cubiertas por C02–C06/LR2-JF, EX agregado y algunos estados de vida/combo tienen evidencia finita favorable. Esto no certifica sus clases completas.
3. Java original ejecutado respalda tablas/selector/collector anteriores; ahora gauge/counter slice, parser real de minas y controles mixtos. Hay 565 estados de flow y tres parseos; no son 568 escenarios independientes.
4. Orden de fases/teclas, transferencia Course y protección de finalización live/rebuild incluyen lectura estática de callers. No se ejecutó el jugador Java completo ni todo su Course/replay.
5. Los gauges LR2 no son observacionalmente equivalentes en todas las condiciones: GP-001 cambia muerte/qualification; GP-006 cambia daño interpretado.
6. La selección fija conserva sus certificaciones; el orden integrado de aplicación diverge en GP-003/004. No se reabre judge variable.
7. Hay diferencias de pérdida/recuperación por precisión y daño de minas. C03 sigue siendo válido dentro de sus vectores cortos.
8. GP-001/003/004/006 demuestran decisiones de supervivencia distintas. Algunas rutas usan composición Java más orden estático, con ese límite explícito.
9. LN/CN mantienen controles previos y correspondencias estáticas; HCN catch-up es una diferencia intencional. Release tardío/frames atrasados y combinaciones completas siguen sin demostrar.
10. EX de controles coincide; minas alteran combo/progreso C# y Course usa max por etapa. GP-002/005 requieren decidir contratos existentes antes de corregir.
11. Mods fijos conservan la evidencia JF; NoBad/rate/otras extensiones no reciben un equivalente Java inventado. GAS completo y randomización tienen cobertura parcial.
12. Replay C# conserva controles anteriores, pero GP-003 muestra sensibilidad al orden del frame. No se certifica equivalencia entre formatos ni todos los seeks/intercalaciones.
13. Nuevos hallazgos: precisión, minas en score, orden de teclas, fases de minas, máximo Course y daño BMS a mitad. Detalle/evidencia en 09 y FINDINGS.csv.
14. Se preservan score normalizado, políticas propias documentadas y catch-up HCN. Judge variable conserva su defecto DEFERRED. Diferencia documentada no implica autorización para cambiarla.
15. Priorizar daño de minas y precisión; después ordenar fases/input con diseño acotado. Precisión y orden tienen efectos amplios; no basta cambiar un umbral. Combo de minas/Course requiere decisión del propietario.
16. Sin certificar: todas las cadencias Java originales, layouts/algoritmos combinados, LN tardío, HCN/GAS/seek completos, Course persistido integral, randomización completa y redondeos complejos BPM/STOP. UI/localización y F099 fuera del alcance.
17. Antes del próximo PR de paridad: revisar independientemente GP-001/003/004/006, autorizar correcciones acotadas y sus regresiones; decidir GP-002/005; ejecutar validaciones focales afectadas y conservar rojas las discrepancias no resueltas. El estado CI global debe comprobarse por separado cuando se prepare ese PR.

No se propone reescritura ni cambios preventivos. Esta fase entrega el diagnóstico, pruebas aisladas y propuestas; ninguna corrección productiva nueva queda autorizada por sus conclusiones.
