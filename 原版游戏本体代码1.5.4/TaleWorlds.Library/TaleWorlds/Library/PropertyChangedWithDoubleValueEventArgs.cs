namespace TaleWorlds.Library;

public struct PropertyChangedWithDoubleValueEventArgs
{
	public string PropertyName { get; }

	public double Value { get; }

	public PropertyChangedWithDoubleValueEventArgs(string propertyName, double value)
	{
		PropertyName = propertyName;
		Value = value;
	}
}
