using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyNavigationModel : MBGameModel<PartyNavigationModel>
{
	public abstract bool CanPlayerNavigateToPosition(CampaignVec2 vec2, out MobileParty.NavigationType navigationType);

	public abstract float GetEmbarkDisembarkThresholdDistance();

	public abstract bool IsTerrainTypeValidForNavigationType(TerrainType terrainType, MobileParty.NavigationType navigationType);

	public abstract int[] GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType navigationType);

	public abstract bool HasNavalNavigationCapability(MobileParty mobileParty);
}
