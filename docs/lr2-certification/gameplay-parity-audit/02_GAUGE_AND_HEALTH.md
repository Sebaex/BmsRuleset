# Gauge, vida y muerte

Se siguieron GaugeProperty/GrooveGauge, validación TOTAL/notas de BMSPlayerRule, updateMicro → BMSPlayer.update, muerte/GAS y persistencia Course. En C#: ProfileFactory → Calculator → HealthProcessor, selección de mods, snapshots y replay. Los valores normalizados C# se convierten a porcentaje para comparar; no se equipara la representación interna.

C03 conserva valor para sus 23 historias cortas y seis agregados EX. Su tolerancia numérica `2e-7` no demuestra que una historia larga conserve decisiones booleanas. La nueva comparación verifica supervivencia y qualification **exactamente**, sin tolerancia para esas decisiones; la traza conserva todos los porcentajes.

GP-001 demuestra que `float` porcentual Java frente a acumulación `double` normalizada C# cambia decisiones. Hard y ExClass, 1.000 notas/TOTAL 300, seed 20%, cinco BAD: Java termina muerto, C# conserva aproximadamente 2% y puede recuperar. Class reproduce el problema con quince BAD. No basta con castear la comparación de DeathThreshold: también importa el redondeo de cada incremento, multiplicación y acumulación anterior.

Normal, TOTAL 200/1.000 notas, seed 20%, 300 PG: Java llega a 79,99992370605469% y no cumple el umbral; C# sí lo cumple. Es un **prefijo** de una canción de 1.000 notas, no una ejecución completa que pruebe clear final. Sí demuestra diferencia del predicado utilizado para qualification/GAS. El control TOTAL 300/200 PG conserva la misma decisión en ambos lados.

GP-003/004 muestran además que el orden de resultados puede cambiar muerte irreversible aun con fórmulas iguales. GP-006 cambia el daño interpretado desde BMS. Por ello la equivalencia de gauges no puede darse por cerrada.

Propuesta pendiente de autorización: reproducir la aritmética LR2 en porcentaje float en el orden original, conservando la API normalizada si resulta viable. No usar epsilon para permitir supervivencias distintas. Validar TOTAL, gains, guts (incluido 32%), multiplicadores HCN, límites 2%, Course, GAS, replay y no-revivir tras muerte. El alcance más amplio exige revisar todos los oráculos C03 antes de implementar; no se aplicó ningún cambio.
