# Replay y Course

Se siguieron frame/recorder → handler/simulación → engine → outputs → snapshots, archive y reconstrucción playfield. Los frames conservan la lista de acciones; Run transforma estados en transiciones y el engine conserva su orden. GP-003 se reproduce con el replay real C# y sus consumidores: igual conjunto de teclas con orden inverso produce estados de supervivencia distintos. El orden Java está respaldado estáticamente y por LaneProperty compilada; no se ejecutó su reproductor completo.

El archive conserva arrays de acciones y compacta frames equivalentes por estado; no se demostró una corrupción de roundtrip. No se equiparan formatos binarios Java/osu!. La reconstrucción restablece scope, eventos y clocks mediante finally; depende de miembros privados de la referencia fijada y requiere revisar compatibilidad al actualizarla. C05 y las regresiones instrumentadas anteriores validan rutas C#, no todos los replays Java.

Course: Session conserva snapshots de gauge, failed, familia y mods clonados; el avance valida estado. Player nuevo por etapa recibe la configuración de salud. GP-001 y GP-006 pueden modificar los snapshots que llegan a la siguiente etapa; GP-005 demuestra además una semántica distinta de máximo combo agregado.

No se ejecutó diferencialmente el Course Java completo (guardar/cargar, abortar, avance UI, clear final y todas las ramas GAS). El caso nuevo compone los contadores originales y la transferencia de estado leída en init/PlayerResource, comparándolos con Session/ScoreAggregation C# reales. La evidencia de clear de Course no puede extrapolarse a partir de ese caso.

Se preservan los contratos propios de agregación documentados por U1-D hasta decisión expresa. La paridad integral live/replay/Course sigue sin certificar; los formatos y la presentación son extensiones y no el objetivo de esta fase.
