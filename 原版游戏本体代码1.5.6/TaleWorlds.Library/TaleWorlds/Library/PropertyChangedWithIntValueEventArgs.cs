namespace TaleWorlds.Library;

public struct PropertyChangedWithIntValueEventArgs(string propertyName, int value)
{
	public string PropertyName { get; } = propertyName;

	public int Value { get; } = value;
}
