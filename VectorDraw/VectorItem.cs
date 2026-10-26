using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace VectorDraw;
public sealed class VectorItem : INotifyPropertyChanged
{
    private string _name="v", _components="2; 1", _color="#D94848";
    public string Name { get=>_name; set { _name=value; Changed(); } }
    public string Components { get=>_components; set { _components=value; Changed(); } }
    public string Color { get=>_color; set { _color=value; Changed(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? property=null)=>PropertyChanged?.Invoke(this,new(property));
}