# Corrección autorizada de la serialización GAS

Autorización expresa del usuario después de reproducir TM-NEW-01. Esta es una excepción productiva limitada al alcance original de C01-TM, independiente de la traducción previamente autorizada.

## Causa y cambio

`BmsModGaugeAutoShift.GasMode` usaba `new() { Value = Easy }`: el constructor genérico dejaba `Default=Assist` (valor cero del enum). El serializador `osu.Game/Online/API/APIMod.cs:40–45` omite los ajustes cuyo bindable declara `IsDefault`. Al elegir Assist, omitía `gas_mode`; `ToMod()` creaba otro GAS cuyo valor inicial era Easy. El clon conservaba Assist: la pérdida se producía al serializar.

Se sustituye exclusivamente esa inicialización por `new(BmsGasLowerLimit.Easy)`. El constructor `osu.Framework/Bindables/Bindable.cs:154–156` establece simultáneamente Value y Default. La elección inicial sigue siendo Easy, Assist y Normal se serializan explícitamente y Easy puede omitirse sin perder información. Restablecer el ajuste vuelve a Easy. No cambian las cadenas, cálculos de salud, reglas LR2, Course, referencias ni formatos de almacenamiento.

## Regresión y evidencia

Se conserva el escenario previamente fallido para Normal, Easy y Assist: semilla Course real `.81`, DeepClone, APIMod, JSON y restauración. Se añaden aserciones independientes de Default=Easy, presencia de `gas_mode` únicamente para valores distintos de Easy y conservación de la semilla. Una prueba nueva verifica construcción, IsDefault, selección de Assist y SetDefault. Estas comprobaciones detectan tanto la pérdida original como una corrección que cambiara solamente el valor inicial o la omisión del serializador.

TM-D03 conserva el fallo previo: 137/138; TRX SHA-256 `1D32DB70FFEE7BB501BC11D8D4AA7A7095EC7117756A68836F6C35E970ED4A0B`. TM-GAS-FIX01 compila Release/net10.0 con SDK 10.0.401 y pasa **139/139**, sin omisiones. Incluye todo el filtro D1: GAS, Course, gráficos y paridad GAS protegida. Logs, filtro, TRX y hashes de ambos binarios se conservan en directorios de evidencia separados; el manifiesto sucesor registra sus hashes exactos. No se ha repetido todavía la suite amplia ni se afirma validación completa de Windows/Ubuntu.

## Compatibilidad y límites

Los registros anteriores con `gas_mode` explícito conservan su significado; los nuevos representan correctamente las tres elecciones. Cuando un registro antiguo omitió Assist por el defecto, ya no existe información suficiente para distinguirlo de la elección inicial: no se inventa una migración ni se reinterpreta toda ausencia como Assist. El intento anterior con JSON sin ajustes (TM-D02) se mantiene documentado; esta corrección no modifica APIMod ni declara resuelta esa ruta independiente.

No se modifica F099, Duration/Multi-BAD, los 49 casos previamente reparados, las expectativas Java ni documentación histórica. La modernización incremental continúa después de este commit independiente.

## Verificación sucesora de persistencia ordinaria

`TestRegularGasLowerLimitSurvivesScorePersistence` añade tres controles sin semillas Course: Normal, Easy y Assist pasan por el almacenamiento real `ScoreInfo.ModsJson`, se restauran desde un ScoreInfo nuevo y generan exactamente sus cadenas4/5/6. También se comprueba la conversión directa APIMod.ToMod y que no se cree contexto Course. TM-GAS-PERSISTENCE01 pasa92/92, incluidas herencia Course y cadenas configuradas; no añade cambios de producción.

La ruta de TM-D02 queda delimitada por el código: `JsonSerializableExtensions.CreateGlobalSettings` usa IgnoreAndPopulate/Replace y puede sustituir Settings omitido por null. `ScoreInfo.cs:332–360` utiliza JsonConvert con su configuración ordinaria para ModsJson y conserva el diccionario vacío inicial. La nueva prueba cubre esa ruta productiva, incluido Easy sin ningún ajuste adicional. No se afirma que el helper global de osu! haya sido corregido ni que todas las rutas externas de JSON estén certificadas.
