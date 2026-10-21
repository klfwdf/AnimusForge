namespace TaleWorlds.Localization;

public sealed class TextObject
{
    private readonly string _value;
    private readonly Dictionary<string, object> _variables = new();
    public TextObject(string value) { _value = value; }
    public void SetTextVariable(string name, object value) => _variables[name] = value;
    public override string ToString()
    {
        string value = _value;
        foreach (var pair in _variables) value = value.Replace("{" + pair.Key + "}", pair.Value?.ToString() ?? "");
        return value;
    }
}
