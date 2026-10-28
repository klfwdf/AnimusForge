namespace TaleWorlds.Library;

public struct PropertyChangedWithUIntValueEventArgs(string propertyName, uint value)
{
	public string PropertyName { get; } = propertyName;

	public uint Value { get; } = value;
}
