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

## Novità interfaccia
- Trascinare il separatore verticale tra elenco e disegno per espandere il canvas; la finestra è ridimensionabile.
- **Mostra vettori componenti** abilita le frecce lungo gli assi; **Tratteggi del parallelepipedo** abilita separatamente le costruzioni ausiliarie. I tratteggi seguono il colore della componente parallela.
- Nel popup ogni componente x/y/z può avere un colore indipendente, oppure **Come il vettore** (valore predefinito).
- All'avvio viene riaperto l'ultimo file JSON salvato o aperto, se ancora disponibile. Il percorso è conservato in `%APPDATA%\\VectorDraw\\last-project.txt`.
- Passando da ℝ² a ℝ³ viene aggiunta la coordinata z=0; passando a dimensioni inferiori vengono rimosse le coordinate eccedenti.

## Area Matrici
La finestra principale contiene due tab: **Vettori e grafici** e **Matrici**.
In **Matrici**, le sottoschede **Matrice A** e **Matrice B** consentono di definire matrici di dimensioni da 1×1 a 10×10 e inserire valori numerici. Le celle esistenti vengono mantenute quando si modificano le dimensioni.
La sottoscheda **Risultato** offre:
- Somma matriciale A+B (richiede dimensioni uguali).
- Prodotto matriciale A×B (colonne A = righe B).
- Somma scalare A+k (somma k a ciascuna cella; convenzione elemento per elemento).
- Prodotto scalare k×A (moltiplica ciascuna cella per k).
Il pulsante **Esegui operazione** calcola il risultato, segnala errori di input o dimensioni e mostra una griglia di sola lettura. Questa prima versione non salva le matrici nel JSON dei vettori.

## Aggiornamento: lingue, zoom e impostazione operazioni
- Selettore lingua in alto: Italiano, English, Français, Deutsch, Español. Le principali etichette dell'interfaccia e della sezione matrici vengono aggiornate senza riavvio.
- Zoom del canvas: rotellina del mouse sopra il disegno, oppure cursore **Scala** (2–400).
- Matrici: sottoscheda **Impostazioni** con numero di matrici (1–10) e scelta dell'operazione; sottoschede A, B, C… generate automaticamente; sottoscheda **Risultato** con pulsante di calcolo e matrice risultante.
- Somma e prodotto matriciale su più operandi sono applicati da sinistra a destra; le operazioni con scalare usano A.
- Le matrici restano separate dal JSON dei progetti vettoriali.

## Disegnare il risultato matriciale
Dopo aver eseguito il calcolo nella sottoscheda **Risultato**, attivare **Disegna il risultato sul canvas**, scegliere **Vettori nelle colonne** oppure **Vettori nelle righe** e premere **Trasferisci al canvas**.
- Con vettori nelle colonne, il numero di righe della matrice deve essere da 1 a 3; ciascuna colonna è un vettore.
- Con vettori nelle righe, il numero di colonne deve essere da 1 a 3; ciascuna riga è un vettore.
- La matrice può contenere fino a 10 vettori; sono usati i 10 colori distinti della palette.
- Il trasferimento **sostituisce** i vettori presenti nell'elenco e nel canvas (non è un semplice nascondimento), imposta ℝ¹/ℝ²/ℝ³ e seleziona il tab dei grafici.
- Il file JSON già salvato su disco resta invariato finché l'utente non preme **Salva progetto**.

## Zoom adattivo del canvas
Il canvas permette di ingrandire e rimpicciolire il contenuto con i pulsanti **+**, **−**, lo slider **Scala** e la rotellina del mouse. Il pulsante **Adatta vettori** calcola la scala in base all'ingombro proiettato dei vettori, con margini del 39% per semiasse. Anche i vettori molto lunghi possono così essere visualizzati rimpicciolendo lo spazio. Dopo l'importazione dei vettori da una matrice, l'adattamento viene eseguito automaticamente.

## Sostituzione o aggiunta dei vettori da una matrice
Nella sottoscheda **Risultato**, dopo il calcolo, scegliere **Sostituisci vettori esistenti** (predefinito) oppure **Aggiungi ai vettori esistenti**, quindi **Trasferisci al canvas**.
- **Sostituisci**: rimuove i vettori precedenti, adegua ℝ alla dimensione del risultato e crea i nuovi vettori.
- **Aggiungi**: conserva i vettori e la dimensione corrente del canvas; richiede che la dimensione corrente sia maggiore o uguale a quella dei vettori importati. Le componenti mancanti vengono completate con zero (es. ℝ² → ℝ³: (x,y,0)).
- Se la dimensione del risultato supera quella del canvas, compare un avviso e non viene effettuata alcuna modifica.
- I colori non ancora usati nella palette hanno priorità; quando sono esauriti vengono riutilizzati. I nuovi nomi sono univoci.
- Dopo il trasferimento la vista si adatta automaticamente ai vettori.

## Equazioni e curve sul canvas
Il tab **Funzioni** permette di inserire un'equazione, un intervallo e un colore. **Disegna sul canvas** aggiunge il risultato ai vettori e alle curve presenti. Sono disponibili funzioni esplicite 2D (`y=f(x)`, `x=f(y)`), curve implicite 2D (`x^2+y^2=25`) e superfici esplicite 3D (`z=f(x,y)`, rappresentate con reticolo). Funzioni: `sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `sqrt`, `abs`, `exp`, `ln`, `log`, `floor`, `ceil`; costanti `pi` ed `e`. La moltiplicazione richiede `*`. Le superfici implicite 3D non sono ancora supportate. Il tab **Vettori e grafici** dispone di **Ripulisci canvas** per cancellare vettori e curve. I risultati delle matrici vengono sempre aggiunti al canvas, ampliando la dimensione se necessario. Le curve sono salvate nel JSON del progetto ed esportate nell'SVG.

### Funzioni tridimensionali
Nel tab Funzioni sono disponibili tre modalità:
- **Superficie esplicita:** `z=x^2+y^2` o `z=sin(x)*cos(y)`.
- **Superficie implicita:** `x^2+y^2+z^2=25` (sfera), `x^2+y^2=9` (cilindro: attenzione, questa espressione priva di z viene attualmente interpretata come curva 2D).
- **Curva parametrica 3D:** `x=cos(t); y=sin(t); z=t`, con intervallo riferito al parametro `t`.

Le superfici sono rese come reticoli di segmenti ruotabili, non come solidi ombreggiati. Il motore implicito usa una discretizzazione a tetraedri e può non individuare dettagli più piccoli della griglia.
