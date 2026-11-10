# Validación y procedencia

Herramientas: Windows, .NET SDK 10.0.401, Release/net10.0; JDK 21.0.13+9. Java compila fuentes fijadas antes del JAR auxiliar, comprobando CodeSource. JAR auxiliar SHA-256 `38101C00CB6918FEBB9E0671558AD9F11D1DB842A699AF1EE80CA5BA217BB249`; su commit fuente completo no está identificado. No se atribuyen a la referencia comportamientos ejecutados exclusivamente desde dependencias auxiliares. En particular, MathUtils.clamp y metadatos ClearType no equivalen a ejecutar el jugador/clear final original.

`FlowOracle.java` compone clases originales con el prefijo literal updateMicro:697–727, hash canónico `AA2F4BE6D6C94C4EB27917C381EE4E18E01CB3513E79C84DD1D1931CAB45DEDD`. El orden de teclas/minas y carry Course se siguen en callers estáticos; se transcriben esos órdenes en el caller, sin afirmar ejecución integral de JudgeManager.update. MineParserOracle ejecuta ambos parsing y gauge originales sobre texto idéntico. PROVENANCE.json registra fuentes compiladas; SOURCE_INDEX añade identidades Git. No hay corpus privado.

| Ejecución conservada | Total | Aprueban | Fallan | Observación |
|---|---:|---:|---:|---|
| audit-01 | 11 | 5 | 6 | Exploratoria; fixture anterior |
| audit-02 | 15 | 7 | 8 | Añade fases de mina |
| audit-03 | 16 | 7 | 9 | Añade Course |
| audit-04 | 20 | 8 | 12 | Añade mismo texto BMS/empty-POOR |
| audit-05 | 18 | 6 | 12 | Definitiva; excluye dos controles derivados de F099 |
| audit-05-repeat | 18 | 6 | 12 | Misma DLL final, identidades/resultados coincidentes |

No se suman repeticiones ni casos solapados como cobertura independiente. No hubo omisiones en el lote final. Los 12 fallos son resultados de auditoría conservados: cuatro historias GP-001, una GP-002, una GP-003, dos permutaciones GP-004, una GP-005 y tres tokens GP-006. Los seis controles positivos verifican estados mixtos, empty-POOR con combo, Normal/TOTAL300, orden canónico y las dos fases de mina con seed10. No comparan igualdad bit a bit de toda vida numérica.

Las cinco compilaciones de investigación fueron separadas y se conservaron TRX/logs/DLL/PDB. El manifest registra hashes de cada binario y resultado. audit-04 guarda snapshot de inputs; audit-05 lo captura automáticamente antes de compilar. Los intentos anteriores no disponen de un snapshot completo de todos los scripts; su DLL, recursos y TRX quedan preservados y no se les atribuye retrospectivamente el hash del fixture final.

java-01 falló al compilar el primer caller por campos de recentJudges ausentes; se conserva, no generó evidencia ejecutable. java-02–07 son iteraciones exploratorias conservadas. java-08 y java-09 regeneran el paquete definitivo con resultados idénticos byte a byte: 565 estados de flow y tres filas de minas. No se sobrescribieron intentos previos.

La fixture C# se almacena como `.cs.txt` y se incorpora mediante `audit.targets` sólo en compilación de investigación opt-in. No se ha cambiado csproj, suite ordinaria ni filtro CI. MSBuild sin opt-in confirma **cero** Compile/EmbeddedResource de esta carpeta (352 Compile y 4.433 EmbeddedResource ordinarios). Las aserciones diferenciales siguen rojas: no hay Skip/Ignore ni goldens C# ajustados para aceptar diferencias. El comando aislado devuelve exit 1 y debe conservarse como tal.

Se reutilizan, no se repiten, los controles existentes de C02–C06/LR2-JF y LR2-CL: foco JF627, 427 protegidos, 288 instrumentados y estabilidad142, con solapamientos. Son evidencia histórica finita sobre fuentes sin cambios productivos en esta fase. Los hashes originales permanecen en sus manifiestos; no se convierten en una nueva certificación integral del árbol ni en resultado de CI de esta auditoría.

Validación nueva adicional `ordinary-focus`: **39/39 aprobadas**, cero fallos/omisiones, sobre la misma DLL audit-05. El filtro exacto está en el manifest: métodos existentes de decoding de minas/base62 y BmsReplaySimulationTest. Son controles del comportamiento actual C#, no un oráculo Java; por ello pueden aprobar expectativas históricas de mitad de daño mientras los diferenciales GP-006 fallan. Se conservaron ambos resultados.

No se lanzó suite completa, benchmarks ni nuevo CI multiplataforma. Los builds muestran advertencias NuGet/CS8629 preexistentes; no se actualizaron paquetes ni se atribuyó una advertencia nueva a un cambio productivo inexistente. No se modificaron los 41 fallos históricos ni sus expectativas. Evidencia bruta local bajo el identificador `lr2-gp-20261109`; no se publican rutas personales, logs con rutas, DLL, JAR ni corpus. El manifest conserva hashes verificables y resultados por identidad.

Los hashes `sha256` de recibos/binarios representan bytes originales de la ejecución Windows. Los artifacts textuales publicados incluyen además `sha256_lf_utf8` para verificar el contenido pese a normalización de finales de línea por Git; SOURCE_INDEX usa esa misma forma canónica y conserva el blob Git del baseline. No se exige igualdad byte a byte de TRX repetidos con timestamps distintos; se exige igualdad de identidades/resultados y DLL.
