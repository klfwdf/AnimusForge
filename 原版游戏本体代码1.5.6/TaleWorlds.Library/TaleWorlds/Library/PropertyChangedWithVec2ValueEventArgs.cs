namespace TaleWorlds.Library;

public struct PropertyChangedWithVec2ValueEventArgs(string propertyName, Vec2 value)
{
	public string PropertyName { get; } = propertyName;

	public Vec2 Value { get; } = value;
}
