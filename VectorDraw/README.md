# VectorDraw

Editor WPF per vettori matematici in ℝ¹, ℝ² e ℝ³, con esportazione SVG e salvataggio JSON.

## Requisiti
- Windows 10/11
- Visual Studio con workload **Sviluppo desktop .NET** e SDK .NET 10

## Avvio
Aprire `VectorDraw.csproj` in Visual Studio e premere F5, oppure eseguire `dotnet run` da Windows.

## Uso
- Selezionare ℝ¹, ℝ² o ℝ³.
- Premere **Aggiungi** per aprire il popup con nome e campi separati x, y, z (in base alla dimensione).
- Selezionare il colore dalla palette; per modificare un vettore usare **Modifica** o doppio clic sulla riga.
- Abilitare **Mostra componenti tratteggiate e pedici** per visualizzare le proiezioni assiali e le etichette vₓ, vᵧ, v_z anche in SVG.
- In ℝ³ trascinare il mouse sul grafico per ruotare la vista.
- Esportare SVG con sfondo trasparente o salvare/riaprire il progetto JSON.

## Note
Il rendering SVG è vettoriale; l'anteprima e l'esportazione condividono le stesse primitive. La proiezione 3D è ortografica. I numeri decimali sono accettati secondo la cultura locale o in formato internazionale. I vettori con componenti non valide sono saltati e segnalati nella barra di stato.

## Limiti della versione iniziale
Nessun test di compilazione eseguito in Windows in questo ambiente. La palette comprende dieci colori predefiniti; non include ancora un selettore colore arbitrario, trascinamento del punto d'origine, frecce/etichette con formule LaTeX o occlusione 3D.
