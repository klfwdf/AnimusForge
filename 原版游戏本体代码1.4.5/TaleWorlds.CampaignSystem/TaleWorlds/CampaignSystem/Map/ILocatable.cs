using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Map;

internal interface ILocatable<T>
{
	[CachedData]
	int LocatorNodeIndex { get; set; }

	[CachedData]
	T NextLocatable { get; set; }

	[CachedData]
	Vec2 GetPosition2D { get; }
}
