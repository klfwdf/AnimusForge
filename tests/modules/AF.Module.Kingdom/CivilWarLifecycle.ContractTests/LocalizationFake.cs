namespace TaleWorlds.Localization;

public sealed class TextObject
{
    private readonly string _value;
    public TextObject(string value) { _value = value; }
    public override string ToString() => _value;
}
