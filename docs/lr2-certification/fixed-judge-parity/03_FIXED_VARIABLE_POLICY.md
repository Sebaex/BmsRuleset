# Contrato fijo y variable — LR2-JF

**Decisión de proyecto:** judge fijo sigue Endless Dream; judge variable conserva provisionalmente el comportamiento C# y no está certificado. Clasificar es una propiedad semántica de los heads jugables, no una búsqueda de texto A0.

Se examinan las tasas efectivas asignadas a notas normales y heads LN/CN/HCN, en todas las columnas, después del parse/conversión y mods que transforman objetos. Una única tasa efectiva significa fijo, aunque sea no estándar o provenga de declaraciones redundantes. Dos tasas efectivas distintas significan variable. Las diferencias entre tablas de normal/scratch/head/tail no significan variabilidad. Un chart sin heads no tiene contenido que certificar; no activa una ruta jugable distinta.

`#RANK` usa su valor real (incluida la equivalencia LR2 de Rank4/2). `#DEFEXRANK` inicial y el alias C# `#EXRANK` global conservan su conversión actual. EXRANKxx define valores; solo referencias válidas en canalxxxA0 producen eventos. Antes de la primera nota pueden fijar una nueva tasa común; después de la última no cambian las tasas de heads ya asignadas. Repeticiones, referencias indefinidas o transiciones intermedias sin ningún head bajo la otra tasa no convierten por sí solas el contenido jugable en variable.

El parser ordena eventos por tick/secuencia y captura la tasa vigente en cada head. Los tails conservan la tasa de su head; un A0 durante un hold no se reinterpreta para cambiarle el tail en esta fase. Si un head posterior recibe otra tasa, el chart es variable. Esta es la semántica C# existente, no una equivalencia Java inventada: el parser Java fijado no ofrece un escenario EXRANKxx/A0 equivalente que permita certificar esa extensión.

No se crean dos motores. El núcleo actual se reutiliza. La diferencia confirmada con No Good/No Great requiere únicamente seleccionar límites anidados en contextos fijos: el límite efectivo de GREAT es max(PG,GREAT), y el de GOOD es max(PG,GREAT,GOOD). Las filas y resultados de todos los demás consumidores permanecen iguales. Con tasas variables se conservan las comparaciones previas, sin corregir indirectamente Multi-BAD heterogéneo. El contexto abarca todas las columnas registradas y se invalida al reconfigurar notas mediante la API existente.

La determinación inicial y los cambios de contexto se validan con casos sintéticos públicos, sin corpus privado. Mutar notas sin RefreshNoteConfiguration ya incumple el contrato de ventanas pasivas/configuración; esta fase no introduce soporte para esa mutación silenciosa. Replays, parser, asignación/serialización de tasas, caches del proveedor, algoritmos Duration y applyMultiBad no se rediseñan.

JF-VARIABLE-01 conserva el defecto de consumo prematuro heterogéneo de U1-C, su reproducción mínima y F099 original en `research/`. Retirar el contrato obsoleto de la suite ordinaria no implica corregir ese defecto ni certificar la extensión.
