using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace VectorDraw;
public partial class VectorDialog : Window
{
    private sealed record PaletteColor(string Name,string Hex)
    {public Brush Brush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Hex)!);}
    private static readonly PaletteColor[] Palette =
    {
        new("Rosso","#D94848"),new("Blu","#2563EB"),new("Verde","#15936A"),
        new("Arancione","#EA580C"),new("Viola","#9333EA"),new("Azzurro","#0891B2"),
        new("Magenta","#DB2777"),new("Nero","#111827"),new("Grigio","#64748B"),
        new("Giallo ocra","#CA8A04")
    };
    private readonly int _dimension;
    public string VectorName {get;private set;}="";
    public string Components {get;private set;}="";
    public string VectorColor {get;private set;}="#2563EB";
    public VectorDialog(int dimension,VectorItem? item=null)
    {
        InitializeComponent();_dimension=dimension;
        YPanel.Visibility=dimension>=2?Visibility.Visible:Visibility.Collapsed;
        ZPanel.Visibility=dimension==3?Visibility.Visible:Visibility.Collapsed;
        ColorBox.ItemsSource=Palette;
        var parts=(item?.Components??"").Split(';',StringSplitOptions.TrimEntries);
        NameBox.Text=item?.Name??"v";
        XBox.Text=parts.Length>0?parts[0]:"0";
        YBox.Text=parts.Length>1?parts[1]:"0";
        ZBox.Text=parts.Length>2?parts[2]:"0";
        ColorBox.SelectedItem=Palette.FirstOrDefault(p=>p.Hex.Equals(item?.Color,StringComparison.OrdinalIgnoreCase))??Palette[1];
        Title=item is null?"Nuovo vettore":"Modifica vettore";
    }
    private void ConfirmClicked(object sender,RoutedEventArgs e)
    {
        var name=NameBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(name)){ErrorText.Text="Inserisci il nome del vettore.";return;}
        var inputs=new[]{XBox.Text,YBox.Text,ZBox.Text};
        var values=new List<string>();
        for(int i=0;i<_dimension;i++)
        {
            if(!double.TryParse(inputs[i],NumberStyles.Float,CultureInfo.CurrentCulture,out var number)
                && !double.TryParse(inputs[i],NumberStyles.Float,CultureInfo.InvariantCulture,out number)
                || !double.IsFinite(number))
            {ErrorText.Text=$"La componente {"xyz"[i]} deve essere un numero valido.";return;}
            values.Add(number.ToString("R",CultureInfo.InvariantCulture));
        }
        VectorName=name;Components=string.Join("; ",values);
        VectorColor=((PaletteColor)ColorBox.SelectedItem!).Hex;
        DialogResult=true;
    }
}