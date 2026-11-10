# Tiempo y ciclo de vida

Java usa microsegundos enteros para eventos, con cálculos intermedios temporales en double en el parser. C# mantiene milisegundos double y deadlines estrictos mediante el siguiente representable. Se revisaron BPM/STOP acumulados y orden de conversión; C04 no certifica todos los colliders de tiempo ni redondeos submicrosegundo. SCROLL de presentación no se equipara a cambio del reloj jugable.

El engine C# toma el número de entradas al comenzar, prohíbe ProcessFrame recursivo y permite encolar desde observers para un lote posterior. En finally retira sólo el lote capturado y restaura processing. Esto protege reentrada, pero no canonicaliza el orden de las transiciones de un mismo instante.

GP-003: para un mismo estado de dos teclas, Java itera por keyassign y C# por inserción. GP-004: minas en el passing loop Java preceden las teclas; C# procesa automáticos del instante después de las pulsaciones. Son diferencias de estado observables por muerte irreversible, no diferencias cosméticas.

La propuesta no debe ordenar indiscriminadamente todos los eventos: timestamps diferentes, liberación/repress y scratch tienen semántica propia. Para minas puede utilizarse un snapshot prospectivo de teclas del lote sin alterar prematuramente el estado real de release; hacerlo directamente podría suprimir tails por considerar la tecla ya liberada.

Live aplica health antes de score (`osu Player:399–403`), simulación aplica score antes de health (`BmsReplaySimulation:173–174`). La conclusión fallida se agenda tras los judgements del frame (`Player.PerformFail:973–1009`); reconstrucción aísla callbacks de failure/NewJudgement mediante scope con restauración. No se demostró un defecto por esa inversión aislada. Quedan pendientes original-Java frame stalls, seek bajo todos los mods y simultaneidad de múltiples lanes/holds.
