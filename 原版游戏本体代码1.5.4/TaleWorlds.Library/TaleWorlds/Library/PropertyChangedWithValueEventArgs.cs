namespace TaleWorlds.Library;

public struct PropertyChangedWithValueEventArgs
{
	public string PropertyName { get; }

	public object Value { get; }

	public PropertyChangedWithValueEventArgs(string propertyName, object value)
	{
		PropertyName = propertyName;
		Value = value;
	}
}
