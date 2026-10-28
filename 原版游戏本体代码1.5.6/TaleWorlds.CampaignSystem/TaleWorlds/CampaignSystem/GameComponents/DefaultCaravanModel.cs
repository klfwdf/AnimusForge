using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCaravanModel : CaravanModel
{
	public override int MaxNumberOfItemsToBuyFromSingleCategory => 300;

	public override float GetEliteCaravanSpawnChance(Hero hero)
	{
		float result = 0f;
		if (hero.Power >= 112f)
		{
			result = hero.Power * 0.0045f - 0.5f;
		}
		return result;
	}

	public override int GetPowerChangeAfterCaravanCreation(Hero hero, MobileParty caravanParty)
	{
		if (hero.Power >= 50f)
		{
			return -30;
		}
		return 0;
	}

	public override bool CanHeroCreateCaravan(Hero hero)
	{
		if (hero.IsMerchant && hero.PartyBelongedTo == null && hero.OwnedCaravans.Count((CaravanPartyComponent x) => !x.MobileParty.Ai.IsDisabled) == 0 && hero.IsActive && !hero.IsTemplate)
		{
			return hero.CanLeadParty();
		}
		return false;
	}

	public override int GetCaravanFormingCost(bool largerCaravan, bool navalCaravan)
	{
		int num = (largerCaravan ? 22500 : 15000);
		if (CharacterObject.PlayerCharacter.Culture.HasFeat(DefaultCulturalFeats.AseraiTraderFeat))
		{
			return MathF.Round((float)num * DefaultCulturalFeats.AseraiTraderFeat.EffectBonus);
		}
		return num;
	}

	public override int GetInitialTradeGold(Hero owner, bool navalCaravan, bool largeCaravan)
	{
		int num = 10000;
		int num2 = ((owner == Hero.MainHero) ? 5000 : 0);
		if (largeCaravan)
		{
			num = 17500;
		}
		return num + num2;
	}

	public override int GetMaxGoldToSpendOnOneItemCategory(MobileParty caravan, ItemCategory itemCategory)
	{
		return 1500;
	}
}
