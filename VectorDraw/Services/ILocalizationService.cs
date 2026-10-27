namespace VectorDraw;
public interface ILocalizationService
{
    string Language { get; }
    void SetLanguage(string language);
    string Translate(string key);
    event Action? Changed;
}