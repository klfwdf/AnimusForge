using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class MobilePartyMoraleModel : MBGameModel<MobilePartyMoraleModel>
{
	public abstract float CalculateMoraleChange(MobileParty party);

	public abstract TextObject GetMoraleTooltipText(MobileParty party);
}
