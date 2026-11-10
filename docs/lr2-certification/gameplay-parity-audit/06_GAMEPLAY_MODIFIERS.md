# Modificadores jugables

Se revisaron aplicación de gauge y GAS, selección de tablas NoGood/NoGreat, ámbito de mods de simulación, modo de long note, autoplay/auto-scratch, NoMine y rutas de branch/replay. Se reutilizan C02–C06 y LR2-JF para las propiedades ejecutadas previamente. La lectura de NoteRandom/Invert y rate es parcial, no una certificación de todas las transformaciones.

Gauge y modos de long note que representan reglas LR2 deben preservar sus efectos; los hallazgos de vida y minas pueden propagarse aunque el mod se aplique correctamente. La selección fija NoGood/NoGreat ya corregida en LR2-JF se conserva.

NoBad, score normalizado, rate y otras combinaciones propias no se fuerzan a un resultado Java sin equivalente. Se distinguen sus contratos C# de las reglas compartidas. Los mods de simulación se aíslan por scope y los seeds/estados Course se clonan; no se modificó ninguna extensión funcional por diferencias de arquitectura.

GAS: se revisaron familias/orden, lower bound, failed monotónico, snapshot y selección. C05 respalda una cadena/configuración finita y BESTCLEAR sobre rangos elegibles equivalentes; no todas las políticas Java SELECT_TO_UNDER/NONE/CONTINUE/SURVIVAL_TO_GROOVE son una sola política C#. No se afirma equivalencia total de GAS.

Riesgo heredado MOD-02: NoteRandom indexa asignaciones al distribuir notas en más posiciones que columnas disponibles. Se conserva la clasificación anterior; no hubo reproducción sobre este baseline, ni se declara defecto nuevo demostrado. Judge variable sigue `DEFERRED`. UI/localización quedan fuera de esta investigación.
