using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace VectorDraw;
public sealed class VectorItem : INotifyPropertyChanged
{
    private string _name="v", _components="2; 1", _color="#D94848";
    public string Name { get=>_name; set { _name=value; Changed(); } }
    public string Components { get=>_components; set { _components=value; Changed(); } }
    public string Color { get=>_color; set { _color=value; Changed(); } }
    // Empty value means inherit the main vector color.
    private string _componentXColor="", _componentYColor="", _componentZColor="";
    public string ComponentXColor {get=>_componentXColor;set{_componentXColor=value;Changed();}}
    public string ComponentYColor {get=>_componentYColor;set{_componentYColor=value;Changed();}}
    public string ComponentZColor {get=>_componentZColor;set{_componentZColor=value;Changed();}}
    public string GetComponentColor(int axis)=>axis switch {0=>string.IsNullOrWhiteSpace(ComponentXColor)?Color:ComponentXColor,1=>string.IsNullOrWhiteSpace(ComponentYColor)?Color:ComponentYColor,_=>string.IsNullOrWhiteSpace(ComponentZColor)?Color:ComponentZColor};
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? property=null)=>PropertyChanged?.Invoke(this,new(property));
}