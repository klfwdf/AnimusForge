using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IMapTracksCampaignBehavior : ICampaignBehavior
{
	MBReadOnlyList<Track> DetectedTracks { get; }

	void AddTrack(MobileParty target, CampaignVec2 trackPosition, Vec2 trackDirection);

	void AddMapArrow(TextObject pointerName, CampaignVec2 trackPosition, Vec2 trackDirection, float life);
}
