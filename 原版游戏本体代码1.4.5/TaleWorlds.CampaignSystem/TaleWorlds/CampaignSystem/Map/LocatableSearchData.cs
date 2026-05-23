using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map;

public struct LocatableSearchData<T>
{
	public readonly Vec2 Position;

	public readonly float RadiusSquared;

	public readonly int MinY;

	public readonly int MaxXInclusive;

	public readonly int MaxYInclusive;

	public int CurrentX;

	public int CurrentY;

	internal ILocatable<T> CurrentLocatable;

	public LocatableSearchData(Vec2 position, float radius, int minX, int minY, int maxX, int maxY)
	{
		Position = position;
		RadiusSquared = radius * radius;
		MinY = minY;
		MaxXInclusive = maxX;
		MaxYInclusive = maxY;
		CurrentX = minX;
		CurrentY = minY - 1;
		CurrentLocatable = null;
	}
}
