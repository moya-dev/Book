using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace VectorDraw;
public partial class MatrixWorkspace : UserControl
{
    private readonly List<TextBox[,]> _matrices=new();
    private readonly List<Grid> _hosts=new();
    private readonly List<TabItem> _operandTabs=new();
    private bool _initialized;
    private ComboBox _count=null!,_operation=null!;
    private TextBox _scalar=null!;
    private StackPanel _scalarPanel=null!;
    private Grid _result=null!;
    private TextBlock _status=null!;
    private TabItem _headerTab=null!,_resultTab=null!;
    private Button _execute=null!, _draw=null!;
    private CheckBox _showOnCanvas=null!;
    private RadioButton _byColumns=null!, _byRows=null!;
    private StackPanel _drawOptions=null!;
    private double[,]? _lastResult;
    public event Action<double[,],bool>? DrawResultRequested;
    public MatrixWorkspace()
    {
        InitializeComponent();
        _headerTab=new TabItem();_resultTab=new TabItem();
        MatrixTabs.Items.Add(_headerTab);
        var panel=new StackPanel{Margin=new Thickness(15)};
        _headerTab.Content=panel;
        panel.Children.Add(new TextBlock{Text="Numero di matrici (1–10)",Margin=new Thickness(0,0,0,6)});
        _count=new ComboBox{Width=110,HorizontalAlignment=HorizontalAlignment.Left,ItemsSource=Enumerable.Range(1,10).ToArray()};
        panel.Children.Add(_count);
        panel.Children.Add(new TextBlock{Text="Operazione",Margin=new Thickness(0,18,0,6)});
        _operation=new ComboBox{Width=330,HorizontalAlignment=HorizontalAlignment.Left};
        panel.Children.Add(_operation);
        _scalarPanel=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,14,0,0)};
        _scalarPanel.Children.Add(new TextBlock{Text="Scalare k",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,0)});
        _scalar=new TextBox{Text="1",Width=100,Padding=new Thickness(6)};
        _scalarPanel.Children.Add(_scalar);panel.Children.Add(_scalarPanel);
        panel.Children.Add(new TextBlock{Text="Le operazioni tra matrici sono applicate in ordine A, B, C…",Margin=new Thickness(0,20,0,0),TextWrapping=TextWrapping.Wrap});
        _resultTab.Content=MakeResultContent();
        _count.SelectionChanged+=(_,_)=>{if(_initialized)RebuildTabs();};
        _operation.SelectionChanged+=(_,_)=>{if(_initialized){_scalarPanel.Visibility=_operation.SelectedIndex>=2?Visibility.Visible:Visibility.Collapsed;ClearResult();}};
        _initialized=true;_count.SelectedItem=2;SetLanguage();
    }
    private object MakeResultContent()
    {
        var dock=new DockPanel{Margin=new Thickness(12)};
        var top=new StackPanel();
        DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);
        _execute=new Button{Padding=new Thickness(18,8),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};
        _execute.Click+=ExecuteClicked;top.Children.Add(_execute);
        _showOnCanvas=new CheckBox{Margin=new Thickness(0,0,0,8)};
        _showOnCanvas.Checked+=(_,_)=>UpdateDrawOptions();
        _showOnCanvas.Unchecked+=(_,_)=>UpdateDrawOptions();
        top.Children.Add(_showOnCanvas);
        _drawOptions=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,10)};
        _byColumns=new RadioButton{GroupName="MatrixVectorOrientation",IsChecked=true,Margin=new Thickness(0,0,18,0)};
        _byRows=new RadioButton{GroupName="MatrixVectorOrientation"};
        _byColumns.Checked+=(_,_)=>UpdateDrawOptions();
        _byRows.Checked+=(_,_)=>UpdateDrawOptions();
        _drawOptions.Children.Add(_byColumns);_drawOptions.Children.Add(_byRows);top.Children.Add(_drawOptions);
        _draw=new Button{Padding=new Thickness(12,7),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};
        _draw.Click+=DrawClicked;top.Children.Add(_draw);
        UpdateDrawOptions();
        _status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)};
        top.Children.Add(_status);
        _result=new Grid{HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
        dock.Children.Add(new ScrollViewer{Content=_result,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        return dock;
    }
    private void UpdateDrawOptions()
    {
        if(_drawOptions is null)return;
        _drawOptions.Visibility=_showOnCanvas.IsChecked==true?Visibility.Visible:Visibility.Collapsed;
        int components=_lastResult is null?0:(_byColumns.IsChecked==true?_lastResult.GetLength(0):_lastResult.GetLength(1));
        _draw.IsEnabled=_showOnCanvas.IsChecked==true&&components is >=1 and <=3;
    }
    private void DrawClicked(object sender,RoutedEventArgs e)
    {
        if(_lastResult is null||_showOnCanvas.IsChecked!=true)return;
        bool columns=_byColumns.IsChecked==true;
        int components=columns?_lastResult.GetLength(0):_lastResult.GetLength(1);
        if(components is <1 or >3)
        {
            _status.Text=I18n.T("Il vettore deve avere da 1 a 3 componenti.");
            return;
        }
        DrawResultRequested?.Invoke((double[,])_lastResult.Clone(),columns);
    }
    private void RebuildTabs()
    {
        int count=(int)_count.SelectedItem;
        while(_matrices.Count<count)
        {
            _matrices.Add(new TextBox[0,0]);
            var host=new Grid{HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
            _hosts.Add(host);
            var rows=new ComboBox{Width=65,ItemsSource=Enumerable.Range(1,10).ToArray(),Margin=new Thickness(0,0,15,0)};
            var cols=new ComboBox{Width=65,ItemsSource=Enumerable.Range(1,10).ToArray()};
            var controls=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,12)};
            controls.Children.Add(new TextBlock{Text=I18n.T("Righe"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,0)});
            controls.Children.Add(rows);
            controls.Children.Add(new TextBlock{Text=I18n.T("Colonne"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,0)});
            controls.Children.Add(cols);
            var dock=new DockPanel{Margin=new Thickness(12)};
            DockPanel.SetDock(controls,Dock.Top);dock.Children.Add(controls);
            dock.Children.Add(new ScrollViewer{Content=host,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
            var tab=new TabItem{Content=dock};_operandTabs.Add(tab);
            int index=_matrices.Count-1;
            rows.SelectionChanged+=(_,_)=>{if(rows.SelectedItem is int r&&cols.SelectedItem is int c)Resize(index,r,c);};
            cols.SelectionChanged+=(_,_)=>{if(rows.SelectedItem is int r&&cols.SelectedItem is int c)Resize(index,r,c);};
            rows.SelectedItem=2;cols.SelectedItem=2;
        }
        MatrixTabs.Items.Clear();MatrixTabs.Items.Add(_headerTab);
        for(int i=0;i<count;i++){_operandTabs[i].Header=$"{I18n.T("Matrice")} {(char)('A'+i)}";MatrixTabs.Items.Add(_operandTabs[i]);}
        MatrixTabs.Items.Add(_resultTab);
        _operation.SelectedIndex=0;
        RefreshOperations();ClearResult();
    }
    private void Resize(int index,int rows,int cols)
    {
        var previous=_matrices[index];var host=_hosts[index];
        host.Children.Clear();host.RowDefinitions.Clear();host.ColumnDefinitions.Clear();
        for(int i=0;i<rows;i++)host.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        for(int j=0;j<cols;j++)host.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var cells=new TextBox[rows,cols];
        for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)
        {
            var value=i<previous.GetLength(0)&&j<previous.GetLength(1)?previous[i,j].Text:"0";
            var box=new TextBox{Text=value,Width=82,MinHeight=32,Padding=new Thickness(6),Margin=new Thickness(3),HorizontalContentAlignment=HorizontalAlignment.Right};
            Grid.SetRow(box,i);Grid.SetColumn(box,j);host.Children.Add(box);cells[i,j]=box;
        }
        _matrices[index]=cells;if(_initialized)ClearResult();
    }
    private void RefreshOperations()
    {
        var index=Math.Max(0,_operation.SelectedIndex);
        _operation.Items.Clear();
        foreach(var name in new[]{"Somma matriciale","Moltiplicazione matriciale","Somma scalare","Moltiplicazione scalare"})
            _operation.Items.Add(I18n.T(name));
        _operation.SelectedIndex=index;
        _scalarPanel.Visibility=index>=2?Visibility.Visible:Visibility.Collapsed;
    }
    public void SetLanguage()
    {
        _headerTab.Header=I18n.T("Impostazioni");_resultTab.Header=I18n.T("Risultato");
        foreach(var tab in _operandTabs.Select((tab,i)=>(tab,i)))
        {
            tab.tab.Header=$"{I18n.T("Matrice")} {(char)('A'+tab.i)}";
            if(tab.tab.Content is DockPanel dock&&dock.Children[0] is StackPanel p)
            {
                ((TextBlock)p.Children[0]).Text=I18n.T("Righe");
                ((TextBlock)p.Children[2]).Text=I18n.T("Colonne");
            }
        }
        if(_headerTab.Content is StackPanel panel)
        {
            ((TextBlock)panel.Children[0]).Text=I18n.T("Numero di matrici (1–10)");
            ((TextBlock)panel.Children[2]).Text=I18n.T("Operazione");
            ((TextBlock)_scalarPanel.Children[0]).Text=I18n.T("Scalare k");
            ((TextBlock)panel.Children[5]).Text=I18n.T("Le operazioni tra matrici sono applicate in ordine A, B, C…");
        }
        _execute.Content=I18n.T("Esegui operazione");
        _showOnCanvas.Content=I18n.T("Disegna il risultato sul canvas");
        _byColumns.Content=I18n.T("Vettori nelle colonne");
        _byRows.Content=I18n.T("Vettori nelle righe");
        _draw.Content=I18n.T("Trasferisci al canvas");
        RefreshOperations();ClearResult();
    }
    private void ClearResult()
    {
        _lastResult=null;UpdateDrawOptions();
        _result.Children.Clear();_result.RowDefinitions.Clear();_result.ColumnDefinitions.Clear();
        _status.Text=I18n.T("Premi Esegui operazione per aggiornare il risultato.");
    }
    private static double[,] Read(TextBox[,] cells,string name)
    {
        int rows=cells.GetLength(0),cols=cells.GetLength(1);var a=new double[rows,cols];
        for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)
            if(!(double.TryParse(cells[i,j].Text,NumberStyles.Float,CultureInfo.CurrentCulture,out a[i,j])
                ||double.TryParse(cells[i,j].Text,NumberStyles.Float,CultureInfo.InvariantCulture,out a[i,j]))
                ||!double.IsFinite(a[i,j]))
                throw new ArgumentException($"{I18n.T("Valore non valido")} {name} [{i+1},{j+1}].");
        return a;
    }
    private void ExecuteClicked(object sender,RoutedEventArgs e)
    {
        try
        {
            int op=_operation.SelectedIndex;
            var inputs=Enumerable.Range(0,(int)_count.SelectedItem).Select(i=>Read(_matrices[i],((char)('A'+i)).ToString())).ToList();
            var result=inputs[0];
            if(op<2)
            {
                if(inputs.Count<2)throw new ArgumentException(I18n.T("Servono almeno due matrici."));
                foreach(var b in inputs.Skip(1))
                {
                    int rows=result.GetLength(0),cols=result.GetLength(1),br=b.GetLength(0),bc=b.GetLength(1);
                    if(op==0)
                    {
                        if(rows!=br||cols!=bc)throw new ArgumentException(I18n.T("Per la somma le matrici devono avere le stesse dimensioni."));
                        var next=new double[rows,cols];
                        for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)next[i,j]=result[i,j]+b[i,j];
                        result=next;
                    }
                    else
                    {
                        if(cols!=br)throw new ArgumentException(I18n.T("Colonne della matrice precedente e righe della successiva non coincidono."));
                        var next=new double[rows,bc];
                        for(int i=0;i<rows;i++)for(int j=0;j<bc;j++)for(int k=0;k<cols;k++)next[i,j]+=result[i,k]*b[k,j];
                        result=next;
                    }
                }
            }
            else
            {
                if(!(double.TryParse(_scalar.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out var k)
                    ||double.TryParse(_scalar.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out k))||!double.IsFinite(k))
                    throw new ArgumentException(I18n.T("Scalare non valido."));
                var next=new double[result.GetLength(0),result.GetLength(1)];
                for(int i=0;i<next.GetLength(0);i++)for(int j=0;j<next.GetLength(1);j++)
                    next[i,j]=op==2?result[i,j]+k:result[i,j]*k;
                result=next;
            }
            ShowResult(result);_status.Text=$"{I18n.T("Risultato")}: {result.GetLength(0)} × {result.GetLength(1)}";
            MatrixTabs.SelectedItem=_resultTab;
        }
        catch(Exception ex){ClearResult();_status.Text=I18n.T("Errore")+": "+ex.Message;}
    }
    private void ShowResult(double[,] matrix)
    {
        _lastResult=(double[,])matrix.Clone();UpdateDrawOptions();
        _result.Children.Clear();_result.RowDefinitions.Clear();_result.ColumnDefinitions.Clear();
        for(int i=0;i<matrix.GetLength(0);i++)_result.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        for(int j=0;j<matrix.GetLength(1);j++)_result.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        for(int i=0;i<matrix.GetLength(0);i++)for(int j=0;j<matrix.GetLength(1);j++)
        {
            var cell=new TextBox{Text=matrix[i,j].ToString("G10",CultureInfo.CurrentCulture),IsReadOnly=true,
                Width=100,MinHeight=34,Padding=new Thickness(7),Margin=new Thickness(3),
                Background=new SolidColorBrush(Color.FromRgb(241,245,249)),HorizontalContentAlignment=HorizontalAlignment.Right};
            Grid.SetRow(cell,i);Grid.SetColumn(cell,j);_result.Children.Add(cell);
        }
    }
}