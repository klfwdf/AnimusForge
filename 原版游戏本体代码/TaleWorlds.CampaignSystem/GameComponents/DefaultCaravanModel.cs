using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000FF RID: 255
	public class DefaultCaravanModel : CaravanModel
	{
		// Token: 0x1700062F RID: 1583
		// (get) Token: 0x060016A3 RID: 5795 RVA: 0x00067537 File Offset: 0x00065737
		public override int MaxNumberOfItemsToBuyFromSingleCategory
		{
			get
			{
				return 300;
			}
		}

		// Token: 0x060016A4 RID: 5796 RVA: 0x00067540 File Offset: 0x00065740
		public override float GetEliteCaravanSpawnChance(Hero hero)
		{
			float result = 0f;
			if (hero.Power >= 112f)
			{
				result = hero.Power * 0.0045f - 0.5f;
			}
			return result;
		}

		// Token: 0x060016A5 RID: 5797 RVA: 0x00067574 File Offset: 0x00065774
		public override int GetPowerChangeAfterCaravanCreation(Hero hero, MobileParty caravanParty)
		{
			if (hero.Power >= 50f)
			{
				return -30;
			}
			return 0;
		}

		// Token: 0x060016A6 RID: 5798 RVA: 0x00067588 File Offset: 0x00065788
		public override bool CanHeroCreateCaravan(Hero hero)
		{
			if (hero.IsMerchant && hero.PartyBelongedTo == null)
			{
				if (hero.OwnedCaravans.Count((CaravanPartyComponent x) => !x.MobileParty.Ai.IsDisabled) == 0 && hero.IsActive && !hero.IsTemplate)
				{
					return hero.CanLeadParty();
				}
			}
			return false;
		}

		// Token: 0x060016A7 RID: 5799 RVA: 0x000675EC File Offset: 0x000657EC
		public override int GetCaravanFormingCost(bool largerCaravan, bool navalCaravan)
		{
			int num = (largerCaravan ? 22500 : 15000);
			if (CharacterObject.PlayerCharacter.Culture.HasFeat(DefaultCulturalFeats.AseraiTraderFeat))
			{
				return MathF.Round((float)num * DefaultCulturalFeats.AseraiTraderFeat.EffectBonus);
			}
			return num;
		}

		// Token: 0x060016A8 RID: 5800 RVA: 0x00067634 File Offset: 0x00065834
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

		// Token: 0x060016A9 RID: 5801 RVA: 0x00067664 File Offset: 0x00065864
		public override int GetMaxGoldToSpendOnOneItemCategory(MobileParty caravan, ItemCategory itemCategory)
		{
			return 1500;
		}
	}
}
