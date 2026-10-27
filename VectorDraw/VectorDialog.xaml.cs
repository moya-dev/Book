using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace VectorDraw;
public partial class VectorDialog : Window
{
    private sealed record PaletteColor(string Name,string Hex)
    {public Brush Brush => string.IsNullOrWhiteSpace(Hex) ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Hex)!);}
    private static readonly PaletteColor[] Palette =
    {
        new(I18n.T("Rosso"),"#D94848"),new(I18n.T("Blu"),"#2563EB"),new(I18n.T("Verde"),"#15936A"),
        new(I18n.T("Arancione"),"#EA580C"),new(I18n.T("Viola"),"#9333EA"),new(I18n.T("Azzurro"),"#0891B2"),
        new(I18n.T("Magenta"),"#DB2777"),new(I18n.T("Nero"),"#111827"),new(I18n.T("Grigio"),"#64748B"),
        new(I18n.T("Giallo ocra"),"#CA8A04")
    };
    private static readonly PaletteColor Inherit = new(I18n.T("Come il vettore"), "");
    private static readonly PaletteColor[] ComponentPalette = new[]{Inherit}.Concat(Palette).ToArray();
    private readonly int _dimension;
    public string VectorName {get;private set;}="";
    public string Components {get;private set;}="";
    public string VectorColor {get;private set;}="#2563EB";
    public string ComponentXColor {get;private set;}="";
    public string ComponentYColor {get;private set;}="";
    public string ComponentZColor {get;private set;}="";
    public VectorDialog(int dimension,VectorItem? item=null)
    {
        InitializeComponent();_dimension=dimension;
        YPanel.Visibility=dimension>=2?Visibility.Visible:Visibility.Collapsed;
        ZPanel.Visibility=dimension==3?Visibility.Visible:Visibility.Collapsed;
        YColorPanel.Visibility=dimension>=2?Visibility.Visible:Visibility.Collapsed;
        ZColorPanel.Visibility=dimension==3?Visibility.Visible:Visibility.Collapsed;
        XColorBox.ItemsSource=ComponentPalette;YColorBox.ItemsSource=ComponentPalette;ZColorBox.ItemsSource=ComponentPalette;
        XColorBox.SelectedItem=ComponentPalette.FirstOrDefault(p=>p.Hex==item?.ComponentXColor)??Inherit;
        YColorBox.SelectedItem=ComponentPalette.FirstOrDefault(p=>p.Hex==item?.ComponentYColor)??Inherit;
        ZColorBox.SelectedItem=ComponentPalette.FirstOrDefault(p=>p.Hex==item?.ComponentZColor)??Inherit;
        ColorBox.ItemsSource=Palette;
        var parts=(item?.Components??"").Split(';',StringSplitOptions.TrimEntries);
        NameBox.Text=item?.Name??"v";
        XBox.Text=parts.Length>0?parts[0]:"0";
        YBox.Text=parts.Length>1?parts[1]:"0";
        ZBox.Text=parts.Length>2?parts[2]:"0";
        ColorBox.SelectedItem=Palette.FirstOrDefault(p=>p.Hex.Equals(item?.Color,StringComparison.OrdinalIgnoreCase))??Palette[1];
        Title=I18n.T(item is null?"Nuovo vettore":"Modifica vettore");
        I18n.TranslateTree(this);
    }
    private void ConfirmClicked(object sender,RoutedEventArgs e)
    {
        var name=NameBox.Text.Trim();
        if(string.IsNullOrWhiteSpace(name)){ErrorText.Text=I18n.T("Inserisci il nome del vettore.");return;}
        var inputs=new[]{XBox.Text,YBox.Text,ZBox.Text};
        var values=new List<string>();
        for(int i=0;i<_dimension;i++)
        {
            if(!double.TryParse(inputs[i],NumberStyles.Float,CultureInfo.CurrentCulture,out var number)
                && !double.TryParse(inputs[i],NumberStyles.Float,CultureInfo.InvariantCulture,out number)
                || !double.IsFinite(number))
            {ErrorText.Text=string.Format(I18n.T("La componente {0} deve essere un numero valido."),"xyz"[i]);return;}
            values.Add(number.ToString("R",CultureInfo.InvariantCulture));
        }
        VectorName=name;Components=string.Join("; ",values);
        VectorColor=((PaletteColor)ColorBox.SelectedItem!).Hex;
        ComponentXColor=(XColorBox.SelectedItem as PaletteColor)?.Hex ?? "";
        ComponentYColor=(YColorBox.SelectedItem as PaletteColor)?.Hex ?? "";
        ComponentZColor=(ZColorBox.SelectedItem as PaletteColor)?.Hex ?? "";
        DialogResult=true;
    }
}