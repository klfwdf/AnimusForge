using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MapVisibilityModel : MBGameModel<MapVisibilityModel>
{
	public abstract float MaximumSeeingRange();

	public abstract float GetPartySeeingRangeBase(MobileParty party);

	public abstract ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false);

	public abstract float GetPartySpottingRatioForMainPartySeeingRange(MobileParty party);

	public abstract float GetHideoutSpottingDistance();

	public abstract void GetMobilePartyVisibilityAndInspectedState(MobileParty mobileParty, Vec2[] points, float seeingRange, out bool isVisible, out bool isInspected);

	public abstract void GetSettlementInspectedState(Settlement settlement, Vec2[] points, float seeingRange, out bool isInspected, out bool isDistanceDependent);

	public abstract bool IsVisibilityPersistent(MobileParty mobileParty);
}
