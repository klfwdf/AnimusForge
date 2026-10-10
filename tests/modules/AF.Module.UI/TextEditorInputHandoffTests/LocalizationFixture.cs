namespace TaleWorlds.Localization;

// This input/lifecycle fixture models English fallback only. Native resource resolution is tested separately.
public sealed class TextObject
{
    private readonly string _text;
    public TextObject(string text)
    {
        _text = text ?? "";
        if (_text.StartsWith("{=", System.StringComparison.Ordinal))
        {
            int end = _text.IndexOf('}');
            if (end >= 0) _text = _text.Substring(end + 1);
        }
    }
    public override string ToString() => _text;
}
