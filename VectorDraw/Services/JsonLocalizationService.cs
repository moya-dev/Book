using System.Globalization;
using System.IO;
using System.Text.Json;
namespace VectorDraw;
public sealed class JsonLocalizationService : ILocalizationService
{
    private static readonly string[] Supported={"it","en","fr","de","es"};
    private readonly Dictionary<string,Dictionary<string,string>> _translations=new();
    public event Action? Changed;
    public string Language {get;private set;}
    public JsonLocalizationService()
    {
        foreach(var language in Supported)
        {
            var path=Path.Combine(AppContext.BaseDirectory,"Resources","Languages",language+".json");
            _translations[language]=File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(path))??new()
                : new();
        }
        var systemLanguage=CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        Language=Supported.Contains(systemLanguage)?systemLanguage:"it";
    }
    public void SetLanguage(string language)
    {
        if(!Supported.Contains(language))return;
        Language=language;
        Changed?.Invoke();
    }
    public string Translate(string key)
    {
        if(_translations.TryGetValue(Language,out var selected)&&selected.TryGetValue(key,out var value))return value;
        if(_translations["it"].TryGetValue(key,out var italian))return italian;
        return key;
    }
}