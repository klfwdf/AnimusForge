namespace TaleWorlds.Library;

public struct PropertyChangedWithColorValueEventArgs(string propertyName, Color value)
{
	public string PropertyName { get; } = propertyName;

	public Color Value { get; } = value;
}
