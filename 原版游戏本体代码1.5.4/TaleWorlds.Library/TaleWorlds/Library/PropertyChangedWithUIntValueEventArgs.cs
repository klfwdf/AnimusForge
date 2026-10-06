namespace TaleWorlds.Library;

public struct PropertyChangedWithUIntValueEventArgs
{
	public string PropertyName { get; }

	public uint Value { get; }

	public PropertyChangedWithUIntValueEventArgs(string propertyName, uint value)
	{
		PropertyName = propertyName;
		Value = value;
	}
}
