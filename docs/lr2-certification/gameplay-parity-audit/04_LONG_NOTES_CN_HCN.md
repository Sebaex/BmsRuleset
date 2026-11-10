# LN, CN y HCN

Se siguieron `JudgeManager` head CN/LN (475–503), liberación/reversión (540–631), automáticos/omisiones (640–675) y cuerpo HCN (331–357). C# distribuye el estado entre BmsHoldRuntime, HoldRegistry, reglas LongNote/ChargeNote/HellChargeNote y HellChargeBodyTracker.

LN aplaza el resultado principal hasta el extremo; CN mantiene head/tail independientes y puede producir dos POOR si falla la cabeza. Se revisaron head consumida, tail pendiente, margen de release LR2 cero, scratch/reversión, deadline estricto y registro de cuerpo independiente tras BAD adicional. No se encontró una nueva diferencia ejecutable en estas rutas durante esta fase; eso no prueba su equivalencia completa.

C04 conserva sus 15 vectores originales de parser/LNMODE/conteo y las regresiones C# de ciclos LN/CN/HCN. C05 conserva el escenario mixto CN live/replay. Son evidencias finitas; el parser Java completo más toda la máquina de holds no se ejecutaron diferencialmente aquí.

La diferencia HCN ya aceptada se conserva como `EXTENSION_INTENTIONAL`: Java emite como máximo un tick al superar 200.000 microsegundos en un update; C# recupera todos los umbrales pendientes mediante bucle y usa el instante estricto siguiente. No se reclasifica como defecto nuevo ni se propone revertirla.

Riesgo no demostrado: una liberación recibida después del final de LN en un frame atrasado puede recorrer en distinto orden release y tail automático. Java procesa inputs antes de determinados automáticos; C# adelanta deadlines antes del input tardío. Hace falta una prueba diferencial con timestamps de entrada y de update separados, head válida, tail y release tardío, conservando frame cadence. No se atribuye un resultado Java concreto sin ejecutar ese escenario. También quedan parciales las intercalaciones HCN/muerte/GAS/seek y cambios de rate durante hold.
