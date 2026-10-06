namespace TaleWorlds.Library;

public struct PropertyChangedWithBoolValueEventArgs
{
	public string PropertyName { get; }

	public bool Value { get; }

	public PropertyChangedWithBoolValueEventArgs(string propertyName, bool value)
	{
		PropertyName = propertyName;
		Value = value;
	}
}
