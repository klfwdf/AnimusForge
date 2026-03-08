using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000160 RID: 352
	public class DefaultValuationModel : ValuationModel
	{
		// Token: 0x06001AC1 RID: 6849 RVA: 0x000896DB File Offset: 0x000878DB
		public override float GetMilitaryValueOfParty(MobileParty party)
		{
			return party.Party.CalculateCurrentStrength() * 15f;
		}

		// Token: 0x06001AC2 RID: 6850 RVA: 0x000896EE File Offset: 0x000878EE
		public override float GetValueOfTroop(CharacterObject troop)
		{
			return troop.GetPower() * 15f;
		}

		// Token: 0x06001AC3 RID: 6851 RVA: 0x000896FC File Offset: 0x000878FC
		public override float GetValueOfHero(Hero hero)
		{
			if (hero.Clan != null)
			{
				return ((float)hero.Clan.Gold * 0.15f + (float)((1 + hero.Clan.Tier * hero.Clan.Tier) * 500)) * ((hero.Clan.Leader == hero) ? 4f : 1f);
			}
			return 500f;
		}
	}
}
