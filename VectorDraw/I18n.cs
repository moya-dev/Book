using System.Windows;
using System.Windows.Controls;
namespace VectorDraw;
public static class I18n
{
    public static string Language {get;private set;}="it";
    public static event Action? Changed;
    private static readonly Dictionary<string,string[]> Words=new()
    {
        // Italiano, English, Français, Deutsch, Español
        ["Disegna il risultato sul canvas"]=["Disegna il risultato sul canvas","Draw result on canvas","Dessiner le résultat sur le canevas","Ergebnis auf Zeichenfläche darstellen","Dibujar el resultado en el lienzo"],
        ["Vettori nelle colonne"]=["Vettori nelle colonne","Vectors in columns","Vecteurs en colonnes","Vektoren in Spalten","Vectores en columnas"],
        ["Vettori nelle righe"]=["Vettori nelle righe","Vectors in rows","Vecteurs en lignes","Vektoren in Zeilen","Vectores en filas"],
        ["Trasferisci al canvas"]=["Trasferisci al canvas","Transfer to canvas","Transférer vers le canevas","Auf Zeichenfläche übertragen","Transferir al lienzo"],
        ["Il vettore deve avere da 1 a 3 componenti."]=["Il vettore deve avere da 1 a 3 componenti.","Vectors must have 1 to 3 components.","Les vecteurs doivent avoir de 1 à 3 composantes.","Vektoren müssen 1 bis 3 Komponenten haben.","Los vectores deben tener entre 1 y 3 componentes."],
        ["Vettori e grafici"]=["Vettori e grafici","Vectors and graphs","Vecteurs et graphiques","Vektoren und Grafiken","Vectores y gráficos"],
        ["Matrici"]=["Matrici","Matrices","Matrices","Matrizen","Matrices"],
        ["Matrice"]=["Matrice","Matrix","Matrice","Matrix","Matriz"],
        ["Impostazioni"]=["Impostazioni","Setup","Paramètres","Einstellungen","Configuración"],
        ["Risultato"]=["Risultato","Result","Résultat","Ergebnis","Resultado"],
        ["Righe"]=["Righe","Rows","Lignes","Zeilen","Filas"],
        ["Colonne"]=["Colonne","Columns","Colonnes","Spalten","Columnas"],
        ["Numero di matrici (1–10)"]=["Numero di matrici (1–10)","Number of matrices (1–10)","Nombre de matrices (1–10)","Anzahl Matrizen (1–10)","Número de matrices (1–10)"],
        ["Operazione"]=["Operazione","Operation","Opération","Operation","Operación"],
        ["Scalare k"]=["Scalare k","Scalar k","Scalaire k","Skalar k","Escalar k"],
        ["Le operazioni tra matrici sono applicate in ordine A, B, C…"]=["Le operazioni tra matrici sono applicate in ordine A, B, C…","Matrix operations are evaluated in order A, B, C…","Les opérations matricielles sont effectuées dans l'ordre A, B, C…","Matrixoperationen erfolgen in der Reihenfolge A, B, C…","Las operaciones matriciales se realizan en orden A, B, C…"],
        ["Somma matriciale"]=["Somma matriciale","Matrix addition","Addition matricielle","Matrixaddition","Suma matricial"],
        ["Moltiplicazione matriciale"]=["Moltiplicazione matriciale","Matrix multiplication","Multiplication matricielle","Matrixmultiplikation","Multiplicación matricial"],
        ["Somma scalare"]=["Somma scalare","Scalar addition","Addition scalaire","Skalare Addition","Suma escalar"],
        ["Moltiplicazione scalare"]=["Moltiplicazione scalare","Scalar multiplication","Multiplication scalaire","Skalarmultiplikation","Multiplicación escalar"],
        ["Esegui operazione"]=["Esegui operazione","Calculate","Calculer","Berechnen","Calcular"],
        ["Premi Esegui operazione per aggiornare il risultato."]=["Premi Esegui operazione per aggiornare il risultato.","Press Calculate to update the result.","Appuyez sur Calculer pour mettre à jour le résultat.","Klicken Sie auf Berechnen, um das Ergebnis zu aktualisieren.","Pulse Calcular para actualizar el resultado."],
        ["Valore non valido"]=["Valore non valido","Invalid value","Valeur invalide","Ungültiger Wert","Valor no válido"],
        ["Servono almeno due matrici."]=["Servono almeno due matrici.","At least two matrices are required.","Il faut au moins deux matrices.","Mindestens zwei Matrizen sind erforderlich.","Se necesitan al menos dos matrices."],
        ["Per la somma le matrici devono avere le stesse dimensioni."]=["Per la somma le matrici devono avere le stesse dimensioni.","Matrices must have the same dimensions for addition.","Les matrices doivent avoir les mêmes dimensions pour l'addition.","Für die Addition müssen die Matrizen gleich groß sein.","Las matrices deben tener las mismas dimensiones para la suma."],
        ["Colonne della matrice precedente e righe della successiva non coincidono."]=["Colonne della matrice precedente e righe della successiva non coincidono.","Previous matrix columns must equal next matrix rows.","Le nombre de colonnes doit correspondre au nombre de lignes suivant.","Spaltenzahl und nachfolgende Zeilenzahl müssen übereinstimmen.","Las columnas deben coincidir con las filas de la siguiente matriz."],
        ["Scalare non valido."]=["Scalare non valido.","Invalid scalar.","Scalaire invalide.","Ungültiger Skalar.","Escalar no válido."],
        ["Errore"]=["Errore","Error","Erreur","Fehler","Error"],
        ["Spazio vettoriale"]=["Spazio vettoriale","Vector space","Espace vectoriel","Vektorraum","Espacio vectorial"],
        ["Vettori (doppio clic per modificare)"]=["Vettori (doppio clic per modificare)","Vectors (double-click to edit)","Vecteurs (double-clic pour modifier)","Vektoren (Doppelklick zum Bearbeiten)","Vectores (doble clic para editar)"],
        ["+ Aggiungi"]=["+ Aggiungi","+ Add","+ Ajouter","+ Hinzufügen","+ Añadir"],
        ["Modifica"]=["Modifica","Edit","Modifier","Bearbeiten","Editar"],
        ["− Rimuovi"]=["− Rimuovi","− Remove","− Supprimer","− Entfernen","− Eliminar"],
        ["Mostra griglia"]=["Mostra griglia","Show grid","Afficher la grille","Raster anzeigen","Mostrar cuadrícula"],
        ["Mostra vettori componenti"]=["Mostra vettori componenti","Show component vectors","Afficher les vecteurs composantes","Komponentenvektoren anzeigen","Mostrar vectores componentes"],
        ["Tratteggi del parallelepipedo"]=["Tratteggi del parallelepipedo","Dashed construction lines","Traits de construction","Gestrichelte Hilfslinien","Líneas auxiliares discontinuas"],
        ["Mostra nomi"]=["Mostra nomi","Show labels","Afficher les noms","Namen anzeigen","Mostrar nombres"],
        ["Scala"]=["Scala","Scale","Échelle","Maßstab","Escala"],
        ["In ℝ³ trascina sull'anteprima per ruotare."]=["In ℝ³ trascina sull'anteprima per ruotare.","In ℝ³ drag to rotate. Scroll to zoom.","En ℝ³, glissez pour tourner. Molette pour zoomer.","In ℝ³ ziehen zum Drehen. Mausrad zum Zoomen.","En ℝ³ arrastre para girar. Rueda para zoom."],
        ["Esporta SVG"]=["Esporta SVG","Export SVG","Exporter SVG","SVG exportieren","Exportar SVG"],
        ["Salva progetto"]=["Salva progetto","Save project","Enregistrer le projet","Projekt speichern","Guardar proyecto"],
        ["Apri progetto"]=["Apri progetto","Open project","Ouvrir le projet","Projekt öffnen","Abrir proyecto"],
        ["Nome"]=["Nome","Name","Nom","Name","Nombre"],
        ["Componenti"]=["Componenti","Components","Composantes","Komponenten","Componentes"],
        ["Colore"]=["Colore","Color","Couleur","Farbe","Color"],
        ["Nuovo vettore"]=["Nuovo vettore","New vector","Nouveau vecteur","Neuer Vektor","Nuevo vector"],
        ["Modifica vettore"]=["Modifica vettore","Edit vector","Modifier le vecteur","Vektor bearbeiten","Editar vector"],
        ["Nome del vettore"]=["Nome del vettore","Vector name","Nom du vecteur","Vektorname","Nombre del vector"],
        ["Componente x"]=["Componente x","x component","Composante x","x-Komponente","Componente x"],
        ["Componente y"]=["Componente y","y component","Composante y","y-Komponente","Componente y"],
        ["Componente z"]=["Componente z","z component","Composante z","z-Komponente","Componente z"],
        ["Colore del vettore"]=["Colore del vettore","Vector color","Couleur du vecteur","Vektorfarbe","Color del vector"],
        ["Colori delle componenti (predefinito: colore vettore)"]=["Colori delle componenti (predefinito: colore vettore)","Component colors (default: vector color)","Couleurs des composantes (par défaut : vecteur)","Komponentenfarben (Standard: Vektorfarbe)","Colores de componentes (predeterminado: vector)"],
        ["Annulla"]=["Annulla","Cancel","Annuler","Abbrechen","Cancelar"],
        ["Conferma"]=["Conferma","Confirm","Confirmer","Bestätigen","Confirmar"],
        ["Come il vettore"]=["Come il vettore","Same as vector","Comme le vecteur","Wie der Vektor","Como el vector"],
        ["Retta"]=["Retta","Line","Droite","Gerade","Recta"],
        ["Piano"]=["Piano","Plane","Plan","Ebene","Plano"],
        ["Spazio"]=["Spazio","Space","Espace","Raum","Espacio"]
    };
    public static string T(string key)=>Words.TryGetValue(key,out var values)?values[Array.IndexOf(new[]{"it","en","fr","de","es"},Language)]:key;
    public static void Set(string code){if(!new[]{"it","en","fr","de","es"}.Contains(code))return;Language=code;Changed?.Invoke();}
    public static void TranslateTree(DependencyObject root)
    {
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if(child is TextBlock text)TranslateProperty(text,text.Text,v=>text.Text=v);
            if(child is ContentControl content && content.Content is string value)TranslateProperty(content,value,v=>content.Content=v);
            if(child is HeaderedContentControl header && header.Header is string h)TranslateProperty(header,h,v=>header.Header=v);
            TranslateTree(child);
        }
    }
    private static readonly Dictionary<object,string> Original=new();
    private static void TranslateProperty(object element,string current,Action<string> setter)
    {
        if(!Original.TryGetValue(element,out var original)){original=current;Original[element]=original;}
        setter(T(original));
    }
}