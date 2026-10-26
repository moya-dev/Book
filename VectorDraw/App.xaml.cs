using System.Windows;
namespace VectorDraw;
public partial class App : Application
{
    public static ILocalizationService Localization {get;}=new JsonLocalizationService();
}
