namespace TaleWorlds.Library;

public struct PropertyChangedWithIntValueEventArgs
{
	public string PropertyName { get; }

	public int Value { get; }

	public PropertyChangedWithIntValueEventArgs(string propertyName, int value)
	{
		PropertyName = propertyName;
		Value = value;
	}
}
