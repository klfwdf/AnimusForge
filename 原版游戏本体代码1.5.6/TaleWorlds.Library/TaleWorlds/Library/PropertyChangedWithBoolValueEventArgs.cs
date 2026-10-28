namespace TaleWorlds.Library;

public struct PropertyChangedWithBoolValueEventArgs(string propertyName, bool value)
{
	public string PropertyName { get; } = propertyName;

	public bool Value { get; } = value;
}
