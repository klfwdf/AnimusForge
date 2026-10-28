namespace TaleWorlds.Library;

public struct PropertyChangedWithFloatValueEventArgs(string propertyName, float value)
{
	public string PropertyName { get; } = propertyName;

	public float Value { get; } = value;
}
