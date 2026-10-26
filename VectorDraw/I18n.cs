using System.Windows;
using System.Windows.Controls;
namespace VectorDraw;
public static class I18n
{
    private static ILocalizationService Service => App.Localization;
    public static string Language => Service.Language;
    public static event Action? Changed
    {
        add => Service.Changed += value;
        remove => Service.Changed -= value;
    }
    public static string T(string key)=>Service.Translate(key);
    public static void Set(string code)=>Service.SetLanguage(code);
    public static void TranslateTree(DependencyObject root)
    {
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if(child is TextBlock text)TranslateProperty(text,text.Text,v=>text.Text=v);
            if(child is ContentControl content && content.Content is string value)TranslateProperty(content,value,v=>content.Content=v);
            if(child is HeaderedContentControl header && header.Header is string h)TranslateProperty(header,h,v=>header.Header=v);
            if(child is FrameworkElement element && element.ToolTip is string tip)TranslateProperty(new TooltipTarget(element),tip,v=>element.ToolTip=v);
            TranslateTree(child);
        }
    }
    public static void TranslateColumns(DataGrid grid)
    {
        foreach(var column in grid.Columns)
            if(column.Header is string header)TranslateProperty(column,header,v=>column.Header=v);
    }
    private sealed record TooltipTarget(FrameworkElement Element);
    private static readonly Dictionary<object,string> Original=new();
    private static void TranslateProperty(object element,string current,Action<string> setter)
    {
        if(!Original.TryGetValue(element,out var original)){original=current;Original[element]=original;}
        setter(T(original));
    }
}