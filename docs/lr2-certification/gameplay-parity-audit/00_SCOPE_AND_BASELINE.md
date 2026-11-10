# LR2-GP — alcance y baseline

Auditoría de gameplay del 9 de noviembre de 2026, sobre `afa95e3a5d9881adbf29e1f89fd61329173ccc29`, rama `fix/lr2-endlessdream-parity-incremental`, fork `hinobara-ctrl/BmsRuleset`. Árbol productivo Git: `77488b1eacb4c5d01af9bce992dcb375db84b538`. Al inicio: árbol e índice limpios, sin archivos untracked preexistentes y HEAD coincidente con origin.

Por instrucción posterior del propietario, LR2-CL no es una dependencia de esta auditoría. Sus pendientes de localización y su CI tienen cierre independiente; aquí no se afirma haberlos resuelto. El baseline publicado es válido para investigar gameplay.

Referencias de solo lectura:

| Fuente | Commit |
|---|---|
| Endless Dream | `d4995153b0a759859ebc8f859dd868abae39db5a` |
| jbms-parser | `52c5ddcb6bf96a2688c36fee30586c89bfe49f99` |
| osu! | `9a10d935b05a3b896f4bd811d620f2023331db93` |
| osu-framework | `37c3e328c8cc7a018234ec78982ea12d3712096a` |

Java ejecutable tiene prioridad para reglas compartidas; C02–C06 y LR2-JF se reutilizan dentro de sus límites. Las decisiones C# explícitas gobiernan extensiones, presentación y contratos propios. Ni el comportamiento actual C# ni un golden histórico justifican por sí solos cambiar expectativas.

Se siguieron callers, condiciones, orden y efectos en selección → resultados → vida/score → muerte/clear → replay/Course. Las matrices separan ejecución diferencial, análisis estático y áreas no determinadas. La ejecución Java nueva compone clases originales y un fragmento literal de `updateMicro`; **no ejecuta el jugador Java completo**. El parser de minas sí decodifica el mismo texto BMS en ambos lados.

Sin modificaciones de producción, dependencias, workflows, runsettings, presets, goldens históricos o referencias. Judge variable permanece `DEFERRED`; F099 queda excluido del paquete final y su cuerpo histórico intacto. Dos controles derivados de F099 aparecieron indebidamente en intentos exploratorios: se retiraron, conservando esos intentos y sus resultados. No respaldan las conclusiones finales.

La cobertura es parcial y dirigida por riesgos. No constituye certificación integral 1:1, aprobación de merge ni autorización para implementar las correcciones propuestas.
