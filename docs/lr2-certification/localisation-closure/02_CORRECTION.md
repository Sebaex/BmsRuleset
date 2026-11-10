# LR2-CL — corrección acotada del fixture

Se modifican únicamente `osu.Game.Rulesets.BmsRuleset.Tests/Visualize/TestSceneBmsCourseResults.cs` y su nueva parte `TestSceneBmsCourseResults.Localisation.cs`. Producción Course, gameplay, judge variable, parser, recursos, referencias, paquetes, workflow y runsettings permanecen intactos.

## Secuencia corregida

1. Capturar un snapshot del locale, cultura/UI culture del update thread, sus valores ambient actuales, culturas predeterminadas y referencia del store. La captura se hace sobre el hilo de actualización; no se confunde con la cultura del hilo NUnit.
2. El escenario original mantiene el control inglés, el cambio real a es, ambas representaciones porcentuales y toda la secuencia de pantalla, agregados, estadísticas y stages.
3. Restaurar el locale mediante ConfigManager.SetValue. Las notificaciones sincrónicas de GameHost encolan las tareas de cultura. Después se encola el callback de restauración en el mismo host.UpdateThread.Scheduler, detrás de esas tareas.
4. Ese callback comprueba locale aceptado, cultura configurada y efectiva/UI según CultureInfoHelper.TryGetCultureInfo y store original. Un finally restaura los valores capturados aunque falle una verificación.
5. El resultado se captura con ExceptionDispatchInfo; el callback siempre señala finalización y el teardown limpia el campo de estado. La espera sigue siendo el AddUntilStep acotado existente, sin ampliar su presupuesto ni bloquear el hilo de actualización. El siguiente paso vuelve a lanzar el mismo fallo con su pila, después de la limpieza. No se elimina ni se convierte en éxito una verificación fallida.

El snapshot reúne los campos anteriores para evitar que un fallo antes de su borrado deje una importación parcial de estado entre tests. Es ayuda privada de esta fixture, no un servicio productivo ni un hook en referencias externas. La cola del host debe permanecer operativa; no se afirma que una fixture pueda limpiar un hilo cuyo host ya haya terminado inesperadamente. Esa situación seguiría haciendo fallar la espera acotada.

## Regresiones nuevas

TestLocaleRoundTripRestoresStateEvenWhenVerificationFails tiene dos entradas:locale inicial vacío y en explícito. Cada una guarda primero el estado real de entrada, establece/captura el estado controlado y hace dos rondas en→es→restauración. Ambas verifican77,00% localizado y77.00% invariante. La primera ronda verifica éxito; la segunda inyecta una excepción privada de verificación y exige recibir exactamente esa misma excepción mientras todos los valores y el store ya están restaurados. El teardown final devuelve además el estado de entrada real, no deja el locale controlado como estado global.

Las aserciones de restauración comprueban independientemente el locale y la misma referencia de store, cultura configurada del update thread, CurrentCulture, CurrentUICulture y ambas culturas predeterminadas, incluidos valores nulos. La comprobación por referencia exige restaurar el snapshot original, no basta comparar dos representaciones incorrectas entre sí. La excepción deliberada solo se acepta dentro de este control negativo; el teardown real la propaga si se produce una verificación fallida.

Las11 identidades originales se conservan y se añaden2. Se repite la fixture completa en procesos nuevos y se valida el foco de localización y las regresiones LR2-JF. Los hashes distinguen los binarios diagnósticos previos de la corrección. [Resultados y CI](03_REGRESSION_AND_CI.md).

## Alcance documental

Se añade [08_VARIABLE_JUDGE_CLOSURE](../fixed-judge-parity/08_VARIABLE_JUDGE_CLOSURE.md), estado DEFERRED. Mantiene JF-VARIABLE-01, la reproducción U1-C, F099 histórico, su sustitución y los controles C# existentes. Cerrar la investigación activa no corrige ni elimina el soporte variable. Los informes y manifiestos históricos permanecen como evidencia de su fase; los índices enlazan el nuevo cierre.
