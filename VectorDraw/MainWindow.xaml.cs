using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
namespace VectorDraw;
public partial class MainWindow : Window
{
    private readonly ObservableCollection<VectorItem> _vectors = new();
    private double _yaw=-0.65, _pitch=0.48;
    private Point? _drag;
    private bool _ready;
    private sealed record ProjectData(int Dimension, double Scale, double Yaw, double Pitch, bool Grid, bool Names, List<VectorItem> Vectors, bool ComponentsVisible=false);
    private sealed record Segment(double X1,double Y1,double X2,double Y2,string Color,double Width,bool Arrow=false,bool Dashed=false);
    private sealed record Caption(double X,double Y,string Text,string Color,int Size=14,string Subscript="");
    private readonly List<Segment> _segments=new();
    private readonly List<Caption> _labels=new();
    public MainWindow()
    {
        InitializeComponent(); VectorGrid.ItemsSource=_vectors;
        _vectors.Add(new VectorItem{Name="u",Components="3; 2; 1",Color="#D94848"});
        _vectors.Add(new VectorItem{Name="v",Components="-2; 3; 2",Color="#2563EB"});
        foreach(var v in _vectors) v.PropertyChanged+=VectorChanged;
        _ready=true; Render();
    }
    private int Dim=>DimensionBox.SelectedIndex+1;
    private double Scale=>ScaleSlider.Value;
    private void VectorChanged(object? sender,PropertyChangedEventArgs e)=>Render();
    private void SettingsChanged(object sender, RoutedEventArgs e){if(_ready)Render();}
    private void CanvasResized(object sender,SizeChangedEventArgs e)=>Render();
    private void AddClicked(object sender,RoutedEventArgs e)
    {
        var dialog=new VectorDialog(Dim){Owner=this};
        if(dialog.ShowDialog()!=true)return;
        var v=new VectorItem{Name=dialog.VectorName,Components=dialog.Components,Color=dialog.VectorColor};
        v.PropertyChanged+=VectorChanged;_vectors.Add(v);Render();
    }
    private void EditClicked(object sender,RoutedEventArgs e)=>EditSelected();
    private void VectorDoubleClicked(object sender,MouseButtonEventArgs e)=>EditSelected();
    private void EditSelected()
    {
        if(VectorGrid.SelectedItem is not VectorItem v)return;
        var dialog=new VectorDialog(Dim,v){Owner=this};
        if(dialog.ShowDialog()!=true)return;
        v.Name=dialog.VectorName;v.Components=dialog.Components;v.Color=dialog.VectorColor;Render();
    }
    private void RemoveClicked(object sender,RoutedEventArgs e)
    {if(VectorGrid.SelectedItem is VectorItem v){v.PropertyChanged-=VectorChanged;_vectors.Remove(v);Render();}}
    private static bool Parse(VectorItem v,int dim,out double[] numbers)
    {
        var parts=v.Components.Split(new[]{';',' ', '\t'},StringSplitOptions.RemoveEmptyEntries);
        numbers=Array.Empty<double>();
        if(parts.Length!=dim)return false;
        var result=new double[dim];
        for(int i=0;i<dim;i++)
            if(!double.TryParse(parts[i],NumberStyles.Float,CultureInfo.CurrentCulture,out result[i]) &&
               !double.TryParse(parts[i],NumberStyles.Float,CultureInfo.InvariantCulture,out result[i]))return false;
        if(result.Any(x=>!double.IsFinite(x)))return false;
        numbers=result;return true;
    }
    private Point Project(double x,double y,double z,double cx,double cy)
    {
        if(Dim==1)return new(cx+x*Scale,cy);
        if(Dim==2)return new(cx+x*Scale,cy-y*Scale);
        var a=x*Math.Cos(_yaw)-y*Math.Sin(_yaw);
        var b=x*Math.Sin(_yaw)+y*Math.Cos(_yaw);
        return new(cx+a*Scale,cy+(b*Math.Sin(_pitch)-z*Math.Cos(_pitch))*Scale);
    }
    private void AddLine(Point a,Point b,string color,double width=1,bool arrow=false,bool dashed=false)
        =>_segments.Add(new(a.X,a.Y,b.X,b.Y,color,width,arrow,dashed));
    private void Build(double width,double height)
    {
        _segments.Clear();_labels.Clear();
        var cx=width/2;var cy=height/2;var extent=Math.Max(1,Math.Min(width,height)/Scale*0.38);
        if(GridCheck.IsChecked==true && Dim<=2)
        {
            var n=(int)Math.Min(100,Math.Ceiling(Math.Max(width,height)/Scale));
            for(int i=-n;i<=n;i++)
            {
                if(Dim==2)AddLine(Project(i,-n,0,cx,cy),Project(i,n,0,cx,cy),"#E8EDF3",0.7);
                AddLine(Project(-n,i,0,cx,cy),Project(n,i,0,cx,cy),"#E8EDF3",0.7);
                if(Dim==1)break;
            }
        }
        AddLine(Project(-extent,0,0,cx,cy),Project(extent,0,0,cx,cy),"#475569",1.5,true);
        _labels.Add(new Caption(Project(extent,0,0,cx,cy).X+5,Project(extent,0,0,cx,cy).Y+15,"x","#334155"));
        if(Dim>=2){AddLine(Project(0,-extent,0,cx,cy),Project(0,extent,0,cx,cy),"#475569",1.5,true);
            var p=Project(0,extent,0,cx,cy);_labels.Add(new Caption(p.X+5,p.Y-5,"y","#334155"));}
        if(Dim==3){AddLine(Project(0,0,-extent,cx,cy),Project(0,0,extent,cx,cy),"#475569",1.5,true);
            var p=Project(0,0,extent,cx,cy);_labels.Add(new Caption(p.X+5,p.Y-5,"z","#334155"));}
        var invalid=0;
        foreach(var v in _vectors)
        {
            if(!Parse(v,Dim,out var c)){invalid++;continue;}
            var color=TryColor(v.Color,out _)?v.Color:"#15936A";
            var p=Project(c[0],Dim>1?c[1]:0,Dim>2?c[2]:0,cx,cy);
            if(ComponentsCheck.IsChecked==true)
            {
                // Ogni componente è un vettore lungo il rispettivo asse.
                // La linea tratteggiata collega l'origine alla proiezione assiale.
                for(int axis=0;axis<Dim;axis++)
                {
                    var px=axis==0?c[0]:0;
                    var py=axis==1?c[1]:0;
                    var pz=axis==2?c[2]:0;
                    var end=Project(px,py,pz,cx,cy);
                    AddLine(new(cx,cy),end,color,1.4,false,true);
                    if(NamesCheck.IsChecked==true)
                    {
                        var middle=new Point(cx+(end.X-cx)*0.55,cy+(end.Y-cy)*0.55);
                        _labels.Add(new Caption(middle.X+5,middle.Y-5,v.Name,color,13,"xyz"[axis].ToString()));
                    }
                }
                // Linee ausiliarie dal punto finale alle proiezioni assiali.
                if(Dim>=2)
                {
                    for(int axis=0;axis<Dim;axis++)
                    {
                        var end=Project(axis==0?c[0]:0,axis==1?c[1]:0,axis==2?c[2]:0,cx,cy);
                        AddLine(end,p,color,0.9,false,true);
                    }
                }
            }
            AddLine(new(cx,cy),p,color,2.6,true);
            if(NamesCheck.IsChecked==true)_labels.Add(new Caption(p.X+8,p.Y-8,v.Name,color,15));
        }
        Status.Text=invalid>0?$"{invalid} vettore/i con componenti non valide per ℝ{Dim}.":$"ℝ{Dim} · {_vectors.Count} vettori";
    }
    private static bool TryColor(string text,out Color color)
    {try{color=(Color)ColorConverter.ConvertFromString(text)!;return true;}catch{color=Colors.SeaGreen;return false;}}
    private static Point[] Arrow(Point a,Point b,double size)
    {
        var dx=b.X-a.X;var dy=b.Y-a.Y;var len=Math.Sqrt(dx*dx+dy*dy);
        if(len<0.01)return Array.Empty<Point>();
        var ux=dx/len;var uy=dy/len;
        return new[]{b,new Point(b.X-ux*size-uy*size*.43,b.Y-uy*size+ux*size*.43),new Point(b.X-ux*size+uy*size*.43,b.Y-uy*size-ux*size*.43)};
    }
    private void Render()
    {
        if(!_ready||DrawingCanvas.ActualWidth<=0||DrawingCanvas.ActualHeight<=0)return;
        Build(DrawingCanvas.ActualWidth,DrawingCanvas.ActualHeight);DrawingCanvas.Children.Clear();
        foreach(var s in _segments)
        {
            TryColor(s.Color,out var color);var brush=new SolidColorBrush(color);
            DrawingCanvas.Children.Add(new Line{X1=s.X1,Y1=s.Y1,X2=s.X2,Y2=s.Y2,Stroke=brush,StrokeThickness=s.Width,StrokeDashArray=s.Dashed?new DoubleCollection{5,4}:null});
            if(s.Arrow){var pts=Arrow(new(s.X1,s.Y1),new(s.X2,s.Y2),10);
                if(pts.Length>0)DrawingCanvas.Children.Add(new Polygon{Points=new PointCollection(pts),Fill=brush});}
        }
        foreach(var t in _labels){TryColor(t.Color,out var c);var label=new TextBlock{Text=t.Text,Foreground=new SolidColorBrush(c),FontSize=t.Size};
            if(t.Subscript.Length>0)label.Inlines.Add(new System.Windows.Documents.Run(t.Subscript){BaselineAlignment=BaselineAlignment.Subscript,FontSize=t.Size*0.72});
            Canvas.SetLeft(label,t.X);Canvas.SetTop(label,t.Y-14);DrawingCanvas.Children.Add(label);}
    }
    private void CanvasDown(object sender,MouseButtonEventArgs e)
    {if(Dim==3){_drag=e.GetPosition(DrawingCanvas);DrawingCanvas.CaptureMouse();}}
    private void CanvasUp(object sender,MouseButtonEventArgs e){_drag=null;DrawingCanvas.ReleaseMouseCapture();}
    private void CanvasMove(object sender,MouseEventArgs e)
    {
        if(_drag is not Point old||Dim!=3)return;
        var now=e.GetPosition(DrawingCanvas);_yaw+=(now.X-old.X)*.009;
        _pitch=Math.Clamp(_pitch+(now.Y-old.Y)*.009,-1.45,1.45);_drag=now;Render();
    }
    private static string F(double v)=>v.ToString("0.###",CultureInfo.InvariantCulture);
    private static string Xml(string? s)=>System.Security.SecurityElement.Escape(s??"")??"";
    private static string SubscriptSvg(Caption t) => t.Subscript.Length==0 ? "" : "<tspan baseline-shift=\'sub\' font-size=\'70%\'>"+Xml(t.Subscript)+"</tspan>";
    private string MakeSvg(double width,double height)
    {
        Build(width,height);
        var sb=new System.Text.StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(width)}\" height=\"{F(height)}\" viewBox=\"0 0 {F(width)} {F(height)}\">");
        foreach(var s in _segments)
        {
            var dashAttribute = s.Dashed ? " stroke-dasharray=\'5 4\'" : "";
            sb.AppendLine($"  <line x1=\"{F(s.X1)}\" y1=\"{F(s.Y1)}\" x2=\"{F(s.X2)}\" y2=\"{F(s.Y2)}\" stroke=\"{Xml(s.Color)}\" stroke-width=\"{F(s.Width)}\"{dashAttribute} />");
            if(s.Arrow){var pts=Arrow(new(s.X1,s.Y1),new(s.X2,s.Y2),10);
                if(pts.Length>0)sb.AppendLine($"  <polygon points=\"{string.Join(" ",pts.Select(p=>$"{F(p.X)},{F(p.Y)}"))}\" fill=\"{Xml(s.Color)}\" />");}
        }
        foreach(var t in _labels)sb.AppendLine($"  <text x=\"{F(t.X)}\" y=\"{F(t.Y)}\" fill=\"{Xml(t.Color)}\" font-family=\"Arial, sans-serif\" font-size=\"{t.Size}\">{Xml(t.Text)}{SubscriptSvg(t)}</text>");
        sb.AppendLine("</svg>");return sb.ToString();
    }
    private void ExportClicked(object sender,RoutedEventArgs e)
    {
        VectorGrid.CommitEdit(DataGridEditingUnit.Row,true);
        var dlg=new SaveFileDialog{Filter="SVG (*.svg)|*.svg",FileName="vettori.svg"};
        if(dlg.ShowDialog()==true){File.WriteAllText(dlg.FileName,MakeSvg(Math.Max(600,DrawingCanvas.ActualWidth),Math.Max(450,DrawingCanvas.ActualHeight)));Status.Text="SVG esportato: "+dlg.FileName;}
    }
    private void SaveClicked(object sender,RoutedEventArgs e)
    {
        VectorGrid.CommitEdit(DataGridEditingUnit.Row,true);
        var dlg=new SaveFileDialog{Filter="VectorDraw JSON (*.json)|*.json",FileName="progetto.json"};
        if(dlg.ShowDialog()==true){var p=new ProjectData(Dim,Scale,_yaw,_pitch,GridCheck.IsChecked==true,NamesCheck.IsChecked==true,_vectors.ToList(),ComponentsCheck.IsChecked==true);
            File.WriteAllText(dlg.FileName,JsonSerializer.Serialize(p,new JsonSerializerOptions{WriteIndented=true}));Status.Text="Progetto salvato.";}
    }
    private void OpenClicked(object sender,RoutedEventArgs e)
    {
        var dlg=new OpenFileDialog{Filter="VectorDraw JSON (*.json)|*.json"};if(dlg.ShowDialog()!=true)return;
        try{
            var p=JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(dlg.FileName))??throw new InvalidDataException("Progetto vuoto");
            if(p.Dimension is <1 or >3)throw new InvalidDataException("Dimensione non valida");
            foreach(var v in _vectors)v.PropertyChanged-=VectorChanged;
            _vectors.Clear();foreach(var v in p.Vectors){v.PropertyChanged+=VectorChanged;_vectors.Add(v);}
            DimensionBox.SelectedIndex=p.Dimension-1;ScaleSlider.Value=p.Scale;_yaw=p.Yaw;_pitch=p.Pitch;
            GridCheck.IsChecked=p.Grid;NamesCheck.IsChecked=p.Names;ComponentsCheck.IsChecked=p.ComponentsVisible;Render();Status.Text="Progetto caricato.";
        }catch(Exception ex){MessageBox.Show("Impossibile aprire il progetto: "+ex.Message,"VectorDraw",MessageBoxButton.OK,MessageBoxImage.Error);}
    }
}