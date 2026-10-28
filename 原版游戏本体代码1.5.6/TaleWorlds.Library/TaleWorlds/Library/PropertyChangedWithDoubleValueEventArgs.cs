namespace TaleWorlds.Library;

public struct PropertyChangedWithDoubleValueEventArgs(string propertyName, double value)
{
	public string PropertyName { get; } = propertyName;

	public double Value { get; } = value;
}
