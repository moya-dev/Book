using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace VectorDraw;
public partial class MatrixWorkspace : UserControl
{
    private TextBox[,] _cellsA=new TextBox[0,0];
    private TextBox[,] _cellsB=new TextBox[0,0];
    private bool _initialized;
    public MatrixWorkspace()
    {
        InitializeComponent();
        var sizes=Enumerable.Range(1,10).ToArray();
        RowsA.ItemsSource=sizes;ColsA.ItemsSource=sizes;
        RowsB.ItemsSource=sizes;ColsB.ItemsSource=sizes;
        RowsA.SelectedItem=2;ColsA.SelectedItem=2;
        RowsB.SelectedItem=2;ColsB.SelectedItem=2;
        OperationBox.SelectedIndex=0;
        _initialized=true;
        _cellsA=BuildEditor(GridA,2,2,_cellsA);
        _cellsB=BuildEditor(GridB,2,2,_cellsB);
        ResultStatus.Text="Inserisci i valori nelle matrici A e B e premi Esegui operazione.";
    }
    private void SizeChangedA(object sender,SelectionChangedEventArgs e)
    {
        if(!_initialized||RowsA.SelectedItem is not int rows||ColsA.SelectedItem is not int cols)return;
        _cellsA=BuildEditor(GridA,rows,cols,_cellsA);
        ClearResult();
    }
    private void SizeChangedB(object sender,SelectionChangedEventArgs e)
    {
        if(!_initialized||RowsB.SelectedItem is not int rows||ColsB.SelectedItem is not int cols)return;
        _cellsB=BuildEditor(GridB,rows,cols,_cellsB);
        ClearResult();
    }
    private void OperationChanged(object sender,SelectionChangedEventArgs e)
    {
        if(ScalarPanel is null)return;
        ScalarPanel.Visibility=OperationBox.SelectedIndex>=2?Visibility.Visible:Visibility.Collapsed;
        if(_initialized)ClearResult();
    }
    private void ClearResult()
    {
        ResultGrid.Children.Clear();ResultGrid.RowDefinitions.Clear();ResultGrid.ColumnDefinitions.Clear();
        ResultStatus.Text="Premi Esegui operazione per aggiornare il risultato.";
    }
    private static TextBox[,] BuildEditor(Grid host,int rows,int cols,TextBox[,] previous)
    {
        host.Children.Clear();host.RowDefinitions.Clear();host.ColumnDefinitions.Clear();
        for(int i=0;i<rows;i++)host.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        for(int j=0;j<cols;j++)host.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var cells=new TextBox[rows,cols];
        for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)
        {
            var value=i<previous.GetLength(0)&&j<previous.GetLength(1)?previous[i,j].Text:"0";
            var box=new TextBox{Text=value,Width=84,MinHeight=32,Padding=new Thickness(6),
                Margin=new Thickness(3),HorizontalContentAlignment=HorizontalAlignment.Right,
                VerticalContentAlignment=VerticalAlignment.Center,
                ToolTip=$"Riga {i+1}, colonna {j+1}"};
            Grid.SetRow(box,i);Grid.SetColumn(box,j);host.Children.Add(box);cells[i,j]=box;
        }
        return cells;
    }
    private static bool TryNumber(string value,out double number)
    {
        // Prima il formato locale, poi quello internazionale.
        return (double.TryParse(value,NumberStyles.Float,CultureInfo.CurrentCulture,out number)
                ||double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out number))
                &&double.IsFinite(number);
    }
    private static double[,] Read(TextBox[,] cells,string name)
    {
        var rows=cells.GetLength(0);var cols=cells.GetLength(1);
        var matrix=new double[rows,cols];
        for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)
        {
            if(!TryNumber(cells[i,j].Text,out matrix[i,j]))
                throw new ArgumentException($"Valore non valido nella matrice {name}, riga {i+1}, colonna {j+1}.");
        }
        return matrix;
    }
    private void ExecuteClicked(object sender,RoutedEventArgs e)
    {
        try
        {
            var a=Read(_cellsA,"A");
            var ar=a.GetLength(0);var ac=a.GetLength(1);
            var operation=OperationBox.SelectedIndex;
            double[,] result;
            if(operation==0)
            {
                var b=Read(_cellsB,"B");
                if(ar!=b.GetLength(0)||ac!=b.GetLength(1))
                    throw new ArgumentException("Per la somma A + B, le due matrici devono avere lo stesso numero di righe e colonne.");
                result=new double[ar,ac];
                for(int i=0;i<ar;i++)for(int j=0;j<ac;j++)result[i,j]=a[i,j]+b[i,j];
            }
            else if(operation==1)
            {
                var b=Read(_cellsB,"B");
                if(ac!=b.GetLength(0))
                    throw new ArgumentException("Per A × B, il numero di colonne di A deve coincidere con il numero di righe di B.");
                var bc=b.GetLength(1);
                result=new double[ar,bc];
                for(int i=0;i<ar;i++)for(int j=0;j<bc;j++)
                    for(int k=0;k<ac;k++)result[i,j]+=a[i,k]*b[k,j];
            }
            else if(operation==2||operation==3)
            {
                if(!TryNumber(ScalarBox.Text,out var scalar))
                    throw new ArgumentException("Inserisci un numero valido per lo scalare k.");
                result=new double[ar,ac];
                for(int i=0;i<ar;i++)for(int j=0;j<ac;j++)
                    result[i,j]=operation==2?a[i,j]+scalar:a[i,j]*scalar;
            }
            else throw new ArgumentException("Seleziona un'operazione.");
            ShowResult(result);
            ResultStatus.Text=$"Operazione eseguita. Matrice risultato: {result.GetLength(0)} × {result.GetLength(1)}.";
        }
        catch(Exception ex){ClearResult();ResultStatus.Text="Errore: "+ex.Message;}
    }
    private void ShowResult(double[,] matrix)
    {
        ResultGrid.Children.Clear();ResultGrid.RowDefinitions.Clear();ResultGrid.ColumnDefinitions.Clear();
        int rows=matrix.GetLength(0),cols=matrix.GetLength(1);
        for(int i=0;i<rows;i++)ResultGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        for(int j=0;j<cols;j++)ResultGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)
        {
            var value=matrix[i,j];
            var label=new TextBox{Text=value.ToString("G10",CultureInfo.CurrentCulture),
                IsReadOnly=true,Width=100,MinHeight=34,Padding=new Thickness(7),Margin=new Thickness(3),
                Background=new SolidColorBrush(Color.FromRgb(241,245,249)),
                HorizontalContentAlignment=HorizontalAlignment.Right,VerticalContentAlignment=VerticalAlignment.Center};
            Grid.SetRow(label,i);Grid.SetColumn(label,j);ResultGrid.Children.Add(label);
        }
    }
}