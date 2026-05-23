using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MapVisibilityModel : MBGameModel<MapVisibilityModel>
{
	public abstract float MaximumSeeingRange();

	public abstract float GetPartySeeingRangeBase(MobileParty party);

	public abstract ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false);

	public abstract float GetPartySpottingRatioForMainPartySeeingRange(MobileParty party);

	public abstract float GetHideoutSpottingDistance();
}
