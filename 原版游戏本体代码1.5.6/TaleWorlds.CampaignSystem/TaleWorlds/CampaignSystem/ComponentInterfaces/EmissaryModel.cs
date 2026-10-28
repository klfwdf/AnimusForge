using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class EmissaryModel : MBGameModel<EmissaryModel>
{
	public abstract int EmissaryRelationBonusForMainClan { get; }

	public abstract bool IsEmissary(Hero hero);
}
