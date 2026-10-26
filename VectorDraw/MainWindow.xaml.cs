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
using Path = System.IO.Path;
namespace VectorDraw;
public partial class MainWindow : Window
{
    private readonly ObservableCollection<VectorItem> _vectors = new();
    private sealed record PaletteColor(string Name,string Hex)
    {
        public Brush Brush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Hex)!);
    }
    private static PaletteColor[] CurvePalette =>
    {
        new(I18n.T("Rosso"),"#D94848"),new(I18n.T("Blu"),"#2563EB"),new(I18n.T("Verde"),"#15936A"),
        new(I18n.T("Arancione"),"#EA580C"),new(I18n.T("Viola"),"#9333EA"),new(I18n.T("Azzurro"),"#0891B2"),
        new(I18n.T("Magenta"),"#DB2777"),new(I18n.T("Nero"),"#111827"),new(I18n.T("Grigio"),"#64748B"),
        new(I18n.T("Giallo ocra"),"#CA8A04")
    };
    private double _yaw=-0.78, _pitch=0.62;
    private Point? _drag;
    private bool _ready;
    private int _previousDim=2;
    private bool _loadingProject;
    private static readonly string LastProjectPointer=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"VectorDraw","last-project.txt");
    private sealed record ProjectData(int Dimension, double Scale, double Yaw, double Pitch, bool Grid, bool Names, List<VectorItem> Vectors, bool ComponentsVisible=false, bool GuidesVisible=true, List<CurveLine>? Curves=null, List<CurveDefinition>? CurveDefinitions=null);
    private sealed record Segment(double X1,double Y1,double X2,double Y2,string Color,double Width,bool Arrow=false,bool Dashed=false,bool Dotted=false);
    private sealed record Caption(double X,double Y,string Text,string Color,int Size=14,string Subscript="");
    private sealed record CurveLine(double X1,double Y1,double Z1,double X2,double Y2,double Z2,string Color);
    private sealed record CurveDefinition(string Equation,double Minimum,double Maximum,string Color,List<CurveLine> Segments);
    private readonly ObservableCollection<CurveDefinition> _curveDefinitions=new();
    private CurveDefinition? _editingCurve;
    private readonly List<CurveLine> _curves=new();
    private void RebuildCurves()
    {
        _curves.Clear();
        foreach(var curve in _curveDefinitions)_curves.AddRange(curve.Segments);
        Render();
    }
    private void CurveDoubleClicked(object sender,MouseButtonEventArgs e)=>EditCurveClicked(sender,e);
    private void EditCurveClicked(object sender,RoutedEventArgs e)
    {
        if(CurveGrid.SelectedItem is not CurveDefinition selected)return;
        _editingCurve=selected;
        EquationBox.Text=selected.Equation;
        RangeMinBox.Text=selected.Minimum.ToString("R",CultureInfo.InvariantCulture);
        RangeMaxBox.Text=selected.Maximum.ToString("R",CultureInfo.InvariantCulture);
        EquationColorBox.SelectedItem=EquationColorBox.Items.OfType<PaletteColor>().FirstOrDefault(p=>p.Hex.Equals(selected.Color,StringComparison.OrdinalIgnoreCase));
        EquationStatus.Text=I18n.T("Modifica i campi e premi Salva modifiche.");
        SaveCurveButton.Content=I18n.T("Salva modifiche");
    }
    private void RemoveCurveClicked(object sender,RoutedEventArgs e)
    {
        if(CurveGrid.SelectedItem is not CurveDefinition selected)return;
        if(ReferenceEquals(_editingCurve,selected))_editingCurve=null;
        _curveDefinitions.Remove(selected);
        SaveCurveButton.Content=I18n.T("+ Aggiungi");
        RebuildCurves();
        EquationStatus.Text=I18n.T("Curva rimossa.");
    }
    private readonly List<Segment> _segments=new();
    private readonly List<Caption> _labels=new();
    public MainWindow()
    {
        InitializeComponent(); VectorGrid.ItemsSource=_vectors;
        CurveGrid.ItemsSource=_curveDefinitions;
        EquationColorBox.ItemsSource=CurvePalette;
        EquationColorBox.SelectedIndex=4;
        _vectors.Add(new VectorItem{Name="u",Components="3; 2",Color="#D94848"});
        _vectors.Add(new VectorItem{Name="v",Components="-2; 3",Color="#2563EB"});
        foreach(var v in _vectors) v.PropertyChanged+=VectorChanged;
        Matrices.DrawResultRequested+=ImportMatrixVectors;
        var defaultLanguage=I18n.Language;
        foreach(var option in LanguageBox.Items.OfType<ComboBoxItem>())
            if((string?)option.Tag==defaultLanguage){LanguageBox.SelectedItem=option;break;}
        _ready=true; LoadLastProject(); I18n.TranslateTree(this); I18n.TranslateColumns(VectorGrid); I18n.TranslateColumns(CurveGrid); Matrices.SetLanguage(); Render();
    }
    private static readonly string[] MatrixVectorColors=
    {
        "#D94848","#2563EB","#15936A","#EA580C","#9333EA",
        "#0891B2","#DB2777","#111827","#64748B","#CA8A04"
    };
    private void ImportMatrixVectors(double[,] matrix,bool byColumns)
    {
        int dim=byColumns?matrix.GetLength(0):matrix.GetLength(1);
        int count=byColumns?matrix.GetLength(1):matrix.GetLength(0);
        if(dim is <1 or >3||count<1)return;
        int canvasDim=Math.Max(Dim,dim);
        if(canvasDim!=Dim)
        {
            // Amplia lo spazio mantenendo i vettori esistenti, completati con zeri.
            _loadingProject=true;
            try{DimensionBox.SelectedIndex=canvasDim-1;_previousDim=canvasDim;}
            finally{_loadingProject=false;}
            foreach(var old in _vectors)
            {
                var parts=old.Components.Split(';',StringSplitOptions.TrimEntries).ToList();
                while(parts.Count<canvasDim)parts.Add("0");
                old.Components=string.Join("; ",parts);
            }
        }
        var usedColors=new HashSet<string>(_vectors.Select(v=>v.Color),StringComparer.OrdinalIgnoreCase);
        var usedNames=new HashSet<string>(_vectors.Select(v=>v.Name),StringComparer.OrdinalIgnoreCase);
        for(int vector=0;vector<count;vector++)
        {
            var values=new string[canvasDim];
            for(int axis=0;axis<canvasDim;axis++)
                values[axis]=(axis<dim?(byColumns?matrix[axis,vector]:matrix[vector,axis]):0d)
                    .ToString("R",CultureInfo.InvariantCulture);
            // Prima utilizza i colori non ancora presenti; riusa la palette solo quando esaurita.
            var color=MatrixVectorColors.FirstOrDefault(c=>!usedColors.Contains(c))
                      ??MatrixVectorColors[vector%MatrixVectorColors.Length];
            usedColors.Add(color);
            int suffix=1;
            while(usedNames.Contains($"v{suffix}"))suffix++;
            var name=$"v{suffix}";usedNames.Add(name);
            var item=new VectorItem
            {
                Name=name,
                Components=string.Join("; ",values),
                Color=color
            };
            item.PropertyChanged+=VectorChanged;
            _vectors.Add(item);
        }
        MainTabs.SelectedIndex=0;
        Dispatcher.BeginInvoke(new Action(FitVectorsToCanvas),System.Windows.Threading.DispatcherPriority.Loaded);
        Render();
        Status.Text=string.Format(I18n.T("ℝ{0} · {1} vettori aggiunti dalla matrice risultato."),canvasDim,count);
    }
    private int Dim=>DimensionBox.SelectedIndex+1;
    private double Scale=>ScaleSlider.Value;
    private void VectorChanged(object? sender,PropertyChangedEventArgs e)=>Render();
    private void SettingsChanged(object sender, RoutedEventArgs e)
    {
        if(!_ready)return;
        if(ReferenceEquals(sender,DimensionBox)&&!_loadingProject)
        {
            var next=Dim;
            foreach(var v in _vectors)
            {
                var parts=v.Components.Split(';',StringSplitOptions.TrimEntries).ToList();
                while(parts.Count<next)parts.Add("0");
                if(parts.Count>next)parts=parts.Take(next).ToList();
                v.Components=string.Join("; ",parts);
            }
            _previousDim=next;
        }
        Render();
    }
    private void CanvasResized(object sender,SizeChangedEventArgs e)=>Render();
    private void ZoomInClicked(object sender,RoutedEventArgs e)=>ZoomBy(1.25);
    private void ZoomOutClicked(object sender,RoutedEventArgs e)=>ZoomBy(0.8);
    private void FitClicked(object sender,RoutedEventArgs e)=>FitVectorsToCanvas();
    private void ZoomBy(double factor)
    {
        ScaleSlider.Value=Math.Clamp(ScaleSlider.Value*factor,ScaleSlider.Minimum,ScaleSlider.Maximum);
    }
    private void FitVectorsToCanvas()
    {
        var width=DrawingCanvas.ActualWidth;
        var height=DrawingCanvas.ActualHeight;
        if(width<=0||height<=0)return;
        double maxX=0,maxY=0;
        foreach(var v in _vectors)
        {
            if(!Parse(v,Dim,out var c))continue;
            // Calcola l'ingombro in coordinate proiettate, indipendentemente dallo zoom.
            var x=c[0];var y=Dim>=2?c[1]:0;var z=Dim==3?c[2]:0;
            void Measure(double px,double py,double pz)
            {
                var projected=Project(px,py,pz,0,0);
                maxX=Math.Max(maxX,Math.Abs(projected.X/Scale));
                maxY=Math.Max(maxY,Math.Abs(projected.Y/Scale));
            }
            Measure(x,y,z);
            if(ComponentsCheck.IsChecked==true)
            {
                Measure(x,0,0);
                if(Dim>=2)Measure(0,y,0);
                if(Dim==3)
                {
                    Measure(0,0,z);
                    if(GuidesCheck.IsChecked==true)
                    {
                        Measure(x,y,0);Measure(x,0,z);Measure(0,y,z);
                    }
                }
            }
        }
        foreach(var curve in _curves)
        {
            var a=Project(curve.X1,curve.Y1,curve.Z1,0,0);
            var b=Project(curve.X2,curve.Y2,curve.Z2,0,0);
            foreach(var p in new[]{a,b})
            {
                if(double.IsFinite(p.X)&&double.IsFinite(p.Y))
                {
                    maxX=Math.Max(maxX,Math.Abs(p.X/Scale));
                    maxY=Math.Max(maxY,Math.Abs(p.Y/Scale));
                }
            }
        }
        if(maxX<1e-12&&maxY<1e-12)return;
        // Lascia un margine per frecce e nomi; riduce o ingrandisce secondo necessità.
        var desired=Math.Min(maxX<1e-12?double.PositiveInfinity:width*0.39/maxX,
                             maxY<1e-12?double.PositiveInfinity:height*0.39/maxY);
        if(double.IsFinite(desired))
            ScaleSlider.Value=Math.Clamp(desired,ScaleSlider.Minimum,ScaleSlider.Maximum);
    }
    private void CanvasWheel(object sender,MouseWheelEventArgs e)
    {
        ZoomBy(e.Delta>0?1.12:1/1.12);
        e.Handled=true;
    }
    private void LanguageChanged(object sender,SelectionChangedEventArgs e)
    {
        if(!_ready||LanguageBox.SelectedItem is not ComboBoxItem item)return;
        I18n.Set(item.Tag?.ToString()??"it");
        I18n.TranslateTree(this);
        Matrices.SetLanguage();
        var selectedHex=(EquationColorBox.SelectedItem as PaletteColor)?.Hex??"#9333EA";
        var palette=CurvePalette;
        EquationColorBox.ItemsSource=palette;
        EquationColorBox.SelectedItem=palette.FirstOrDefault(p=>p.Hex.Equals(selectedHex,StringComparison.OrdinalIgnoreCase))??palette[4];
        SaveCurveButton.Content=I18n.T(_editingCurve is null?"+ Aggiungi":"Salva modifiche");
        I18n.TranslateColumns(VectorGrid);
        I18n.TranslateColumns(CurveGrid);
    }
    private void ClearCanvasClicked(object sender,RoutedEventArgs e)
    {
        foreach(var v in _vectors)v.PropertyChanged-=VectorChanged;
        _vectors.Clear();_curves.Clear();_curveDefinitions.Clear();_editingCurve=null;SaveCurveButton.Content=I18n.T("+ Aggiungi");Render();
        Status.Text=I18n.T("Canvas ripulito.");
    }
    private static void PlotImplicitSurface(EquationEngine expression,double min,double max,Action<double,double,double,double,double,double> add)
    {
        const int n=30;
        double step=(max-min)/n;
        var values=new double[n+1,n+1,n+1];
        for(int i=0;i<=n;i++)for(int j=0;j<=n;j++)for(int k=0;k<=n;k++)
            values[i,j,k]=expression.Evaluate(min+i*step,min+j*step,min+k*step);
        int[,] offsets={{0,0,0},{1,0,0},{1,1,0},{0,1,0},{0,0,1},{1,0,1},{1,1,1},{0,1,1}};
        int[,] tetra={{0,5,1,6},{0,1,2,6},{0,2,3,6},{0,3,7,6},{0,7,4,6},{0,4,5,6}};
        int[,] edges={{0,1},{0,2},{0,3},{1,2},{1,3},{2,3}};
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)for(int k=0;k<n;k++)
        {
            var corners=new (double X,double Y,double Z,double V)[8];
            for(int v=0;v<8;v++)
            {
                int ix=i+offsets[v,0],iy=j+offsets[v,1],iz=k+offsets[v,2];
                corners[v]=(min+ix*step,min+iy*step,min+iz*step,values[ix,iy,iz]);
            }
            for(int t=0;t<6;t++)
            {
                var pts=new List<(double X,double Y,double Z)>();
                for(int e=0;e<6;e++)
                {
                    var a=corners[tetra[t,edges[e,0]]];
                    var b=corners[tetra[t,edges[e,1]]];
                    if(!double.IsFinite(a.V)||!double.IsFinite(b.V)||((a.V<0)==(b.V<0)))continue;
                    var ratio=a.V/(a.V-b.V);
                    pts.Add((a.X+ratio*(b.X-a.X),a.Y+ratio*(b.Y-a.Y),a.Z+ratio*(b.Z-a.Z)));
                }
                if(pts.Count==3)
                {
                    for(int q=0;q<3;q++)
                    {
                        var a=pts[q];var b=pts[(q+1)%3];
                        add(a.X,a.Y,a.Z,b.X,b.Y,b.Z);
                    }
                }
                else if(pts.Count==4)
                {
                    int[,] tri={{0,1,2},{0,2,3}};
                    for(int q=0;q<2;q++)for(int e=0;e<3;e++)
                    {
                        var a=pts[tri[q,e]];var b=pts[tri[q,(e+1)%3]];
                        add(a.X,a.Y,a.Z,b.X,b.Y,b.Z);
                    }
                }
            }
        }
    }
    private void PlotEquationClicked(object sender,RoutedEventArgs e)
    {
        try
        {
            var input=EquationBox.Text.Trim();
            var parts=input.Split('=',2);
            if(parts.Length!=2)throw new FormatException(I18n.T("Inserisci un'equazione con =."));
            if(!double.TryParse(RangeMinBox.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out var min)&&
               !double.TryParse(RangeMinBox.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out min))
                throw new FormatException(I18n.T("Limite minimo non valido."));
            if(!double.TryParse(RangeMaxBox.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out var max)&&
               !double.TryParse(RangeMaxBox.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out max))
                throw new FormatException(I18n.T("Limite massimo non valido."));
            if(!double.IsFinite(min)||!double.IsFinite(max)||max<=min||max-min>100000)
                throw new FormatException(I18n.T("Intervallo non valido."));
            var color=(EquationColorBox.SelectedItem as PaletteColor)?.Hex ?? "#9333EA";
            if(!TryColor(color,out _))throw new FormatException(I18n.T("Colore non valido."));
            var left=parts[0].Trim().ToLowerInvariant();
            var right=parts[1].Trim().ToLowerInvariant();
            bool parametric=input.Contains(';');
            int required=parametric||left=="z"||left.Contains('z')||right.Contains('z')?3:2;
            var generated=new List<CurveLine>();
            void Add(double x1,double y1,double z1,double x2,double y2,double z2)
            {
                if(new[]{x1,y1,z1,x2,y2,z2}.All(double.IsFinite)&&generated.Count<30000)
                    generated.Add(new CurveLine(x1,y1,z1,x2,y2,z2,color));
            }
            const int n=180;
            var step=(max-min)/n;
            if(parametric)
            {
                var definitions=input.Split(';',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
                var expressions=new Dictionary<string,EquationEngine>(StringComparer.OrdinalIgnoreCase);
                foreach(var definition in definitions)
                {
                    var assignment=definition.Split('=',2,StringSplitOptions.TrimEntries);
                    if(assignment.Length!=2||assignment[0] is not ("x" or "y" or "z")||expressions.ContainsKey(assignment[0]))
                        throw new FormatException(I18n.T("Usa x=...; y=...; z=... con una definizione per coordinata."));
                    expressions.Add(assignment[0],new EquationEngine(assignment[1]));
                }
                if(expressions.Count!=3)throw new FormatException(I18n.T("La curva parametrica richiede x(t), y(t) e z(t)."));
                const int samples=1200;
                var dt=(max-min)/samples;
                for(int i=0;i<samples;i++)
                {
                    var t1=min+i*dt;var t2=t1+dt;
                    double X(double t)=>expressions["x"].Evaluate(t:t);
                    double Y(double t)=>expressions["y"].Evaluate(t:t);
                    double Z(double t)=>expressions["z"].Evaluate(t:t);
                    Add(X(t1),Y(t1),Z(t1),X(t2),Y(t2),Z(t2));
                }
            }
            else if(left=="y"&&!right.Contains('y')&&!right.Contains('z'))
            {
                var expr=new EquationEngine(right);
                for(int i=0;i<n;i++)
                {
                    var x=min+i*step;var y1=expr.Evaluate(x);var y2=expr.Evaluate(x+step);
                    if(Math.Abs(y2-y1)<(max-min)*3)Add(x,y1,0,x+step,y2,0);
                }
            }
            else if(left=="x"&&!right.Contains('x')&&!right.Contains('z'))
            {
                var expr=new EquationEngine(right);
                for(int i=0;i<n;i++)
                {
                    var y=min+i*step;var x1=expr.Evaluate(0,y);var x2=expr.Evaluate(0,y+step);
                    if(Math.Abs(x2-x1)<(max-min)*3)Add(x1,y,0,x2,y+step,0);
                }
            }
            else if(left=="z"&&!right.Contains('z'))
            {
                var expr=new EquationEngine(right);
                const int mesh=55;
                var delta=(max-min)/mesh;
                for(int i=0;i<=mesh;i++)for(int j=0;j<=mesh;j++)
                {
                    var x=min+i*delta;var y=min+j*delta;var z=expr.Evaluate(x,y);
                    if(i<mesh){var z2=expr.Evaluate(x+delta,y);if(Math.Abs(z2-z)<(max-min)*4)Add(x,y,z,x+delta,y,z2);}
                    if(j<mesh){var z2=expr.Evaluate(x,y+delta);if(Math.Abs(z2-z)<(max-min)*4)Add(x,y,z,x,y+delta,z2);}
                }
            }
            else
            {
                if(required==3)
                {
                    PlotImplicitSurface(new EquationEngine("("+left+")-("+right+")"),min,max,Add);
                }
                else
                {
                // Curve implicite 2D: contorni di F(x,y)=0 con marching squares.
                var expr=new EquationEngine("("+left+")-("+right+")");
                var values=new double[n+1,n+1];
                for(int i=0;i<=n;i++)for(int j=0;j<=n;j++)
                    values[i,j]=expr.Evaluate(min+i*step,min+j*step);
                // Gli zeri isolati (ad esempio x²+y²=0) non attraversano
                // alcuna cella: rappresentali come punti anziché scartarli.
                var pointKeys=new HashSet<(int,int)>();
                for(int i=0;i<=n;i++)for(int j=0;j<=n;j++)
                {
                    if(!double.IsFinite(values[i,j])||Math.Abs(values[i,j])>1e-12)continue;
                    bool isolated=true;
                    for(int di=-1;di<=1;di++)for(int dj=-1;dj<=1;dj++)
                    {
                        int ni=i+di,nj=j+dj;
                        if((di==0&&dj==0)||ni<0||ni>n||nj<0||nj>n)continue;
                        if(double.IsFinite(values[ni,nj])&&values[ni,nj]<-1e-12)isolated=false;
                    }
                    if(isolated&&pointKeys.Add((i,j)))
                    {
                        var px=min+i*step;var py=min+j*step;
                        Add(px,py,0,px,py,0);
                    }
                }
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)
                {
                    double x=min+i*step,y=min+j*step;
                    var corners=new (double X,double Y,double V)[]
                    {
                        (x,y,values[i,j]),(x+step,y,values[i+1,j]),
                        (x+step,y+step,values[i+1,j+1]),(x,y+step,values[i,j+1])
                    };
                    var hits=new List<(double X,double Y)>();
                    for(int k=0;k<4;k++)
                    {
                        var a=corners[k];var b=corners[(k+1)%4];
                        if(!double.IsFinite(a.V)||!double.IsFinite(b.V)||((a.V<0)==(b.V<0)))continue;
                        var t=a.V/(a.V-b.V);
                        hits.Add((a.X+t*(b.X-a.X),a.Y+t*(b.Y-a.Y)));
                    }
                    for(int k=0;k+1<hits.Count;k+=2)
                        Add(hits[k].X,hits[k].Y,0,hits[k+1].X,hits[k+1].Y,0);
                }
            }
            }
            if(generated.Count==0)throw new InvalidOperationException(I18n.T("Nessuna curva trovata nell'intervallo indicato."));
            if(required>Dim)
            {
                _loadingProject=true;
                try{DimensionBox.SelectedIndex=required-1;_previousDim=required;}
                finally{_loadingProject=false;}
                foreach(var v in _vectors)
                {
                    var components=v.Components.Split(';',StringSplitOptions.TrimEntries).ToList();
                    while(components.Count<required)components.Add("0");
                    v.Components=string.Join("; ",components);
                }
            }
            var definition=new CurveDefinition(input,min,max,color,generated);
            if(_editingCurve is not null)
            {
                var index=_curveDefinitions.IndexOf(_editingCurve);
                if(index>=0)_curveDefinitions[index]=definition;
                else _curveDefinitions.Add(definition);
                _editingCurve=null;
                SaveCurveButton.Content="+ Aggiungi";
            }
            else _curveDefinitions.Add(definition);
            CurveGrid.SelectedItem=definition;
            RebuildCurves();
            MainTabs.SelectedIndex=0;
            Dispatcher.BeginInvoke(new Action(FitVectorsToCanvas),System.Windows.Threading.DispatcherPriority.Loaded);
            Render();
            EquationStatus.Text=string.Format(I18n.T("Equazione disegnata: {0} segmenti."),generated.Count);
        }
        catch(Exception ex)
        {
            EquationStatus.Text=I18n.T("Errore")+": "+ex.Message;
            MessageBox.Show(this,ex.Message,I18n.T("Equazione non valida"),MessageBoxButton.OK,MessageBoxImage.Warning);
        }
    }
    private void AddClicked(object sender,RoutedEventArgs e)
    {
        var dialog=new VectorDialog(Dim){Owner=this};
        if(dialog.ShowDialog()!=true)return;
        var v=new VectorItem{Name=dialog.VectorName,Components=dialog.Components,Color=dialog.VectorColor,ComponentXColor=dialog.ComponentXColor,ComponentYColor=dialog.ComponentYColor,ComponentZColor=dialog.ComponentZColor};
        v.PropertyChanged+=VectorChanged;_vectors.Add(v);Render();
    }
    private void EditClicked(object sender,RoutedEventArgs e)=>EditSelected();
    private void VectorDoubleClicked(object sender,MouseButtonEventArgs e)=>EditSelected();
    private void EditSelected()
    {
        if(VectorGrid.SelectedItem is not VectorItem v)return;
        var dialog=new VectorDialog(Dim,v){Owner=this};
        if(dialog.ShowDialog()!=true)return;
        v.Name=dialog.VectorName;v.Components=dialog.Components;v.Color=dialog.VectorColor;v.ComponentXColor=dialog.ComponentXColor;v.ComponentYColor=dialog.ComponentYColor;v.ComponentZColor=dialog.ComponentZColor;Render();
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
    // Camera ortografica: base ottenuta da rotazioni rigide 3D.
    // I vettori di base restano ortogonali in R3 prima della proiezione.
    private Point Project(double x,double y,double z,double cx,double cy)
    {
        if(Dim==1)return new(cx+x*Scale,cy);
        if(Dim==2)return new(cx+x*Scale,cy-y*Scale);
        var cosA=Math.Cos(_yaw);var sinA=Math.Sin(_yaw);
        var cosE=Math.Cos(_pitch);var sinE=Math.Sin(_pitch);
        // Rotazione attorno a z seguita da elevazione della camera.
        // Orizzontale camera = (cosA,-sinA,0)
        // Verticale camera   = (sinA*sinE,cosA*sinE,cosE)
        var sx=x*cosA-y*sinA;
        var sy=x*sinA*sinE+y*cosA*sinE+z*cosE;
        return new(cx+sx*Scale,cy-sy*Scale);
    }
    private void AddLine(Point a,Point b,string color,double width=1,bool arrow=false,bool dashed=false,bool dotted=false)
        =>_segments.Add(new(a.X,a.Y,b.X,b.Y,color,width,arrow,dashed,dotted));
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
                // Componenti cartesiane: frecce vere e proprie sugli assi.
                // Le linee ausiliarie seguono spigoli paralleli agli assi,
                // non collegamenti diagonali tra punta e proiezioni.
                var origin=new Point(cx,cy);
                var axisEnds=new Point[Dim];
                for(int axis=0;axis<Dim;axis++)
                {
                    axisEnds[axis]=Project(axis==0?c[0]:0,axis==1?c[1]:0,axis==2?c[2]:0,cx,cy);
                }
                // Disegnare prima le costruzioni tratteggiate.
                if(GuidesCheck.IsChecked==true && Dim==2)
                {
                    var xy=p;
                    AddLine(axisEnds[0],xy,v.GetComponentColor(1),1.1,false,true);
                    AddLine(axisEnds[1],xy,v.GetComponentColor(0),1.1,false,true);
                }
                else if(GuidesCheck.IsChecked==true && Dim==3)
                {
                    // Piano xy: O, X, Y, XY. Poi elevazione da XY a XYZ.
                    var xy=Project(c[0],c[1],0,cx,cy);
                    var xz=Project(c[0],0,c[2],cx,cy);
                    var yz=Project(0,c[1],c[2],cx,cy);
                    AddLine(axisEnds[0],xy,color,1.1,false,true);
                    AddLine(axisEnds[1],xy,color,1.1,false,true);
                    AddLine(xy,p,v.GetComponentColor(2),1.1,false,true);
                    // Altri due percorsi paralleli agli assi, utili a
                    // rendere leggibile il parallelepipedo in prospettiva.
                    AddLine(axisEnds[0],xz,v.GetComponentColor(2),0.9,false,true);
                    AddLine(axisEnds[2],xz,v.GetComponentColor(0),0.9,false,true);
                    AddLine(xz,p,v.GetComponentColor(1),0.9,false,true);
                    AddLine(axisEnds[1],yz,v.GetComponentColor(2),0.9,false,true);
                    AddLine(axisEnds[2],yz,v.GetComponentColor(1),0.9,false,true);
                    AddLine(yz,p,v.GetComponentColor(0),0.9,false,true);
                }
                for(int axis=0;axis<Dim;axis++)
                {
                    var end=axisEnds[axis];
                    if(Point.Subtract(end,origin).Length<0.01)continue;
                    // Frecce componenti continue, spesse e visibili sugli assi.
                    AddLine(origin,end,v.GetComponentColor(axis),3.2,true);
                    if(NamesCheck.IsChecked==true)
                    {
                        var dx=end.X-cx;var dy=end.Y-cy;
                        var length=Math.Sqrt(dx*dx+dy*dy);
                        var nx=length>0.01?-dy/length:0;
                        var ny=length>0.01?dx/length:0;
                        var middle=new Point(cx+dx*0.65+nx*13,cy+dy*0.65+ny*13);
                        _labels.Add(new Caption(middle.X,middle.Y,v.Name,v.GetComponentColor(axis),13,"xyz"[axis].ToString()));
                    }
                }
            }
            AddLine(new(cx,cy),p,color,2.6,true);
            if(NamesCheck.IsChecked==true)_labels.Add(new Caption(p.X+8,p.Y-8,v.Name,color,15));
        }
        foreach(var curve in _curves)
        {
            var a=Project(curve.X1,curve.Y1,curve.Z1,cx,cy);
            var b=Project(curve.X2,curve.Y2,curve.Z2,cx,cy);
            if(double.IsFinite(a.X)&&double.IsFinite(a.Y)&&double.IsFinite(b.X)&&double.IsFinite(b.Y))
                AddLine(a,b,curve.Color,2,dotted:Dim==3);
        }
        Status.Text=invalid>0?string.Format(I18n.T("{0} vettore/i con componenti non valide per ℝ{1}."),invalid,Dim):string.Format(I18n.T("ℝ{0} · {1} vettori"),Dim,_vectors.Count);
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
            if(Math.Abs(s.X1-s.X2)<1e-8&&Math.Abs(s.Y1-s.Y2)<1e-8&&!s.Arrow)
            {
                var dot=new Ellipse{Width=8,Height=8,Fill=brush};
                Canvas.SetLeft(dot,s.X1-4);Canvas.SetTop(dot,s.Y1-4);
                DrawingCanvas.Children.Add(dot);
            }
            else if(s.Dotted)
            {
                // Un solo punto ogni 12 px, condividendo la griglia globale per
                // non sovraccaricare le superfici composte da molti segmenti corti.
                const double spacing=12;
                var dx=s.X2-s.X1;var dy=s.Y2-s.Y1;
                var length=Math.Sqrt(dx*dx+dy*dy);
                if(length>=spacing)
                {
                    int count=(int)Math.Floor(length/spacing);
                    for(int k=0;k<=count;k++)
                    {
                        var t=(k*spacing)/length;
                        var dot=new Ellipse{Width=2.5,Height=2.5,Fill=brush};
                        Canvas.SetLeft(dot,s.X1+t*dx-1.25);
                        Canvas.SetTop(dot,s.Y1+t*dy-1.25);
                        DrawingCanvas.Children.Add(dot);
                    }
                }
            }
            else DrawingCanvas.Children.Add(new Line{X1=s.X1,Y1=s.Y1,X2=s.X2,Y2=s.Y2,Stroke=brush,StrokeThickness=s.Width,StrokeDashArray=s.Dashed?new DoubleCollection{5,4}:null});
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
        _pitch=Math.Clamp(_pitch+(now.Y-old.Y)*.009,0.12,1.45);_drag=now;Render();
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
            if(Math.Abs(s.X1-s.X2)<1e-8&&Math.Abs(s.Y1-s.Y2)<1e-8&&!s.Arrow)
                sb.AppendLine($"  <circle cx=\"{F(s.X1)}\" cy=\"{F(s.Y1)}\" r=\"4\" fill=\"{Xml(s.Color)}\" />");
            else if(s.Dotted)
            {
                var dx=s.X2-s.X1;var dy=s.Y2-s.Y1;
                var length=Math.Sqrt(dx*dx+dy*dy);
                if(length>=12)
                {
                    int count=(int)Math.Floor(length/12);
                    for(int k=0;k<=count;k++)
                    {
                        var t=k*12/length;
                        sb.AppendLine($"  <circle cx=\"{F(s.X1+t*dx)}\" cy=\"{F(s.Y1+t*dy)}\" r=\"1.25\" fill=\"{Xml(s.Color)}\" />");
                    }
                }
            }
            else sb.AppendLine($"  <line x1=\"{F(s.X1)}\" y1=\"{F(s.Y1)}\" x2=\"{F(s.X2)}\" y2=\"{F(s.Y2)}\" stroke=\"{Xml(s.Color)}\" stroke-width=\"{F(s.Width)}\"{dashAttribute} />");
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
        if(dlg.ShowDialog()==true){File.WriteAllText(dlg.FileName,MakeSvg(Math.Max(600,DrawingCanvas.ActualWidth),Math.Max(450,DrawingCanvas.ActualHeight)));Status.Text=I18n.T("SVG esportato: ")+dlg.FileName;}
    }
    private void SaveClicked(object sender,RoutedEventArgs e)
    {
        VectorGrid.CommitEdit(DataGridEditingUnit.Row,true);
        var dlg=new SaveFileDialog{Filter="VectorDraw JSON (*.json)|*.json",FileName="progetto.json"};
        if(dlg.ShowDialog()==true){var p=new ProjectData(Dim,Scale,_yaw,_pitch,GridCheck.IsChecked==true,NamesCheck.IsChecked==true,_vectors.ToList(),ComponentsCheck.IsChecked==true,GuidesCheck.IsChecked==true,_curves.ToList(),_curveDefinitions.ToList());
            File.WriteAllText(dlg.FileName,JsonSerializer.Serialize(p,new JsonSerializerOptions{WriteIndented=true}));RememberProject(dlg.FileName);Status.Text=I18n.T("Progetto salvato.");}
    }
    private void OpenClicked(object sender,RoutedEventArgs e)
    {
        var dlg=new OpenFileDialog{Filter="VectorDraw JSON (*.json)|*.json"};if(dlg.ShowDialog()!=true)return;
        LoadProject(dlg.FileName);
    }
    private static void RememberProject(string filename)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LastProjectPointer)!);
        File.WriteAllText(LastProjectPointer,Path.GetFullPath(filename));
    }
    private void LoadLastProject()
    {
        try
        {
            if(!File.Exists(LastProjectPointer))return;
            var filename=File.ReadAllText(LastProjectPointer).Trim();
            if(File.Exists(filename))LoadProject(filename);
        }
        catch(Exception ex){Status.Text=I18n.T("Ultimo progetto non caricato: ")+ex.Message;}
    }
    private void LoadProject(string filename)
    {
        try{
            var p=JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(filename))??throw new InvalidDataException(I18n.T("Progetto vuoto"));
            if(p.Dimension is <1 or >3)throw new InvalidDataException(I18n.T("Dimensione non valida"));
            foreach(var v in _vectors)v.PropertyChanged-=VectorChanged;
            _vectors.Clear();_curves.Clear();_curveDefinitions.Clear();_editingCurve=null;
            if(p.CurveDefinitions is {Count:>0})
            {
                foreach(var definition in p.CurveDefinitions)_curveDefinitions.Add(definition);
                foreach(var definition in _curveDefinitions)_curves.AddRange(definition.Segments);
            }
            else if(p.Curves is {Count:>0})
            {
                // I progetti precedenti non contenevano l'equazione originale.
                _curveDefinitions.Add(new CurveDefinition("Curva importata (equazione non disponibile)",-10,10,p.Curves[0].Color,p.Curves));
                _curves.AddRange(p.Curves);
            }
            SaveCurveButton.Content="+ Aggiungi";foreach(var v in p.Vectors){v.PropertyChanged+=VectorChanged;_vectors.Add(v);}
            _loadingProject=true;
            try{DimensionBox.SelectedIndex=p.Dimension-1;_previousDim=p.Dimension;}finally{_loadingProject=false;}
            ScaleSlider.Value=p.Scale;_yaw=p.Yaw;_pitch=p.Pitch;
            GridCheck.IsChecked=p.Grid;NamesCheck.IsChecked=p.Names;ComponentsCheck.IsChecked=p.ComponentsVisible;GuidesCheck.IsChecked=p.GuidesVisible;RememberProject(filename);Render();Status.Text=I18n.T("Progetto caricato.");
        }catch(Exception ex){MessageBox.Show(I18n.T("Impossibile aprire il progetto: ")+ex.Message,"VectorDraw",MessageBoxButton.OK,MessageBoxImage.Error);}
    }
}