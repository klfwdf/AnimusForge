using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map;

public struct LocatableSearchData<T>(Vec2 position, float radius, int minX, int minY, int maxX, int maxY)
{
	public readonly Vec2 Position = position;

	public readonly float RadiusSquared = radius * radius;

	public readonly int MinY = minY;

	public readonly int MaxXInclusive = maxX;

	public readonly int MaxYInclusive = maxY;

	public int CurrentX = minX;

	public int CurrentY = minY - 1;

	internal ILocatable<T> CurrentLocatable = null;
}
