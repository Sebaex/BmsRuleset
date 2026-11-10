# Reconciliación explícita de F099

Identidad histórica: `BmsColumnNoteIndexTest.DurationSearchSkipsCloserHeadsWithNoHitWindow`. [Cuerpo original](research/F099-original.cs.txt), SHA-256 `F794C7EB1B4F5EFECB73C355A440AE83F509A9BF651A44C0663FB4C26ED0A62C`. El archivo es idéntico al campo body de F099 en ORIGINAL_METHODS.json: se verificaron igualdad exacta del texto y hash de bytes, incluidos sus caracteres de fin de línea originales.

Entradas históricas: tiempos900/1000/1060/1150, tasas1/.05/.05/1 y Duration a1030. Exigía únicamente GOOD1150. Hay dos cuestiones independientes: expectativa heredada de ventanas anteriores, y consumo prematuro de1150 como BAD adicional con la tabla heterogénea de la principal. U1-C demostró la segunda; no es una divergencia diferencial Java porque el motor fijado no aplica tasas temporales por nota equivalentes.

La nueva autorización permite **sustituir** esa identidad por un contrato fijo. No se utiliza Ignore, Explicit, Skip, catch de aserciones ni filtro para esconderla. Su sucesor es `DurationFixedJudgeKeepsEarlierTieAndLeavesGoodNeighboursPending`, con los mismos cuatro tiempos y Rank3 uniforme. El oráculo original confirma Duration: BAD900, GREAT1000;1060/1150 permanecen pendientes. El sucesor verifica además orden real, consumo y pendientes, segunda/tercera pulsación, ausencia de duplicados tras pasivo, combo, EX-score, gauge y reconstrucción del replay. No conserva artificialmente la expectativa GOOD1150.

Continuación C#: press1060 y1150 dan PG, cuatro resultados en total, combo3, progresión EX[0,0,1,3,5] y health.25 con TOTAL12/N4. Replay conserva orden y offsets[130,30,0,0]. La primera pulsación tiene respaldo Java ejecutable; la continuación completa es validación C# y no una ejecución del player Java.

Se retira de la suite activa la exigencia histórica heterogénea de un único resultado GOOD1150. Se conserva su cuerpo, entradas, aserciones, resultado fallido TM-F09901 y reproducción U1-C. [Inventario pendiente](PENDING_DEFECTS.json), [reproducción mínima](research/heterogeneous-reproduction.json). Un control ordinario verifica la principal estable y que la segunda nota merece GOOD según su propia tabla; registra el consumo observado como diagnóstico, **sin aceptar el BAD defectuoso como una expectativa correcta**.

Los registros activos C01-TM enlazan el sucesor y JF-VARIABLE-01. Los informes históricos F099_DEFERRED, FINAL_VERDICT, VALIDATION_MANIFEST y TRX se conservan tal como fueron publicados. Hay218 identidades históricas conservadas y una sustituida; no se afirma219 originales sin cambios. La identificación del commit sucesor se registra en las matrices después del commit de pruebas, evitando hashes autorreferenciales.

La extensión variable no se corrige: parser, tasas, serialización, replay y applyMultiBad permanecen intactos. Reconciliar un contrato obsoleto no resuelve el defecto heterogéneo.
