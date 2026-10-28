namespace TaleWorlds.Library;

public struct PropertyChangedWithValueEventArgs(string propertyName, object value)
{
	public string PropertyName { get; } = propertyName;

	public object Value { get; } = value;
}
