# LR2-CL — causa raíz y reproducción

Baseline local/remoto limpio `eb54f7ebde2c31cbe0b66b0093f2f22f9ba0775a`, rama fix/lr2-endlessdream-parity-incremental, origin hinobara-ctrl/BmsRuleset. Referencias de solo lectura: Endless Dream d4995153b0a759859ebc8f859dd868abae39db5a, parser52c5ddcb6bf96a2688c36fee30586c89bfe49f99, osu!9a10d935b05a3b896f4bd811d620f2023331db93, framework37c3e328c8cc7a018234ec78982ea12d3712096a. No hay una regla Java aplicable a este defecto de fixture.

## Fallo demostrado

`TestSceneBmsCourseResults.cs` del baseline:75–76 esperaba que host.UpdateThread.CurrentCulture.Name fuera CultureInfo.GetCultureInfo(previousHostLocale).Name. Con locale vacío, esa segunda expresión exige la cultura invariante. El framework usa otro contrato:

| Fuente fijada, relativa al framework | Regla |
|---|---|
| Configuration/FrameworkConfigManager.cs:46 |Locale predeterminado: string.Empty. |
| Localisation/CultureInfoHelper.cs:18–36 |SystemCulture captura la cultura de inicialización; TryGetCultureInfo("") devuelve esa cultura, no la invariante. |
| Platform/GameHost.cs:1293–1303 |El cambio de locale actualiza culturas predeterminadas y llama ThreadRunner.SetCulture con el resultado del helper. |
| Platform/ThreadRunner.cs:238–253 |Actualiza cultura/UI culture del hilo actual y encola la configuración de cada GameThread. |
| Threading/GameThread.cs:90–98,477–485 |Guarda la cultura configurada y la aplica a CurrentCulture/CurrentUICulture en su hilo. |
| Localisation/LocalisationManager.cs:117–170 |Locale vacío selecciona el mapping de SystemUICulture; locale registrado selecciona su store; un locale inválido se revierte. La cultura de formato del host y el nombre del store no tienen por qué ser idénticos. |
| Configuration/ConfigManager.cs:314; Bindables/Bindable.cs:309–331 |GetBindable devuelve una copia enlazada; Value propaga cambios sincrónicos, TriggerChange sobre la copia no propaga hacia afuera. No es una barrera ni fuerza una nueva notificación del host. |

El CI previo no registraba el locale inicial exacto. Su valor vacío es una explicación apoyada por el default y el timeout, no una observación añadida retrospectivamente al log. La reproducción controlada local sí captura la causa directamente: locale='', ambient=es-CL, ui=es-CL, host=es-CL, system=es-CL, literal-empty='', store=es. La condición anterior es imposible bajo esas condiciones aunque la propagación del framework haya terminado correctamente.

CL-BASELINE-EMPTY01 fuerza únicamente locale vacío antes del escenario original, espera su propagación con el contrato real y registra esos valores. El resto de Course y el teardown anterior permanecen activos. Resultado:1/1 falla exactamente en "restored locale reaches update thread", después de ejecutar el escenario principal. Fuente diagnóstica, diff, DLL hashes y TRX se conservan aparte; no se publica esa instrumentación temporal.

## Contaminación de las pruebas siguientes

El siguiente AddStep del teardown contenía la limpieza y el borrado de previousHostLocale. `Testing/TestScene.cs:229–258` detiene la secuencia cuando un paso falla; ese siguiente paso no llega a ejecutarse. La misma instancia de fixture se reutiliza (`Testing/TestSceneTestRunner.cs:70–74`). Por tanto el campo permanece no nulo y otros tests que nunca cambiaron locale vuelven a ejecutar el mismo teardown fallido.

CL-BASELINE-CASCADE01 ejecuta la fixture completa con el mismo diagnóstico:7/11 aprueban y4 fallan, en este orden: TestCourseSummaryWithCommaDecimalHostLocale, TestFailedStageDoesNotShadeCourseTimeline, TestIntermediateStageEnterAdvancesInsteadOfTogglingStatistics, TestIntermediateStageResultIsRestricted. Es el mismo conjunto y orden de los cuatro fallos Windows del CI. Demuestra una contaminación de estado de fixture por limpieza inalcanzable; no demuestra un defecto de la agregación Course.

## Evidencia CI recuperada

[Run42656398221](https://github.com/hinobara-ctrl/BmsRuleset/actions/runs/42656398221), SHA eb54f7e. Windows:4515 casos,4508 aprobados,4 fallidos y3 omitidos. Ubuntu:4515,4503 aprobados,1 fallido y**11 omitidos** según UnitTestResult. El encargo indicaba6 omitidos para Ubuntu; el TRX descargado demuestra11. No se corrige ni se oculta el archivo original. Ambos builds e instrumentación288/288 aprueban.

Todos los fallos ordinarios citan RestoreHostLocale y el mismo timeout. Que Ubuntu solo falle una identidad no niega la ruta de contaminación demostrada: la reutilización, orden y resultado del runner condicionan qué tests posteriores quedan afectados. No se atribuye a un sistema operativo una causa distinta sin evidencia.

La comprobación de representación localizada/invariante original se conserva: resolver .77 mediante LocalisationManager español da77,00%; LocalisableString.ToString() da77.00%. Ese contrato y el defecto es-CL no registrado ya investigado en LR2-JF son distintos del error actual de restauración de locale vacío.
