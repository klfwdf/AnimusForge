using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000424 RID: 1060
	public class PartiesSellPrisonerCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060042F4 RID: 17140 RVA: 0x001435A6 File Offset: 0x001417A6
		public override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
		}

		// Token: 0x060042F5 RID: 17141 RVA: 0x001435D6 File Offset: 0x001417D6
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x060042F6 RID: 17142 RVA: 0x001435D8 File Offset: 0x001417D8
		private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (mobileParty != null && !mobileParty.IsMainParty && settlement.IsFortification && mobileParty.MapFaction != null && !mobileParty.IsDisbanding && !mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction) && (mobileParty.PrisonRoster.TotalRegulars > 0 || (mobileParty.PrisonRoster.TotalHeroes > 0 && mobileParty.PrisonRoster.GetTroopRoster().Exists((TroopRosterElement x) => x.Character != CharacterObject.PlayerCharacter && x.Character.HeroObject.MapFaction.IsAtWarWith(settlement.MapFaction)))))
			{
				TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
				foreach (TroopRosterElement troopRosterElement in mobileParty.PrisonRoster.GetTroopRoster())
				{
					if (!troopRosterElement.Character.IsHero || (!troopRosterElement.Character.IsPlayerCharacter && troopRosterElement.Character.HeroObject.MapFaction.IsAtWarWith(settlement.MapFaction)))
					{
						troopRoster.Add(troopRosterElement);
					}
				}
				SellPrisonersAction.ApplyForSelectedPrisoners(mobileParty.Party, settlement.Party, troopRoster);
			}
		}

		// Token: 0x060042F7 RID: 17143 RVA: 0x00143728 File Offset: 0x00141928
		private void DailyTickSettlement(Settlement settlement)
		{
			if (settlement.IsFortification)
			{
				TroopRoster prisonRoster = settlement.Party.PrisonRoster;
				if (prisonRoster.TotalRegulars > 0)
				{
					int num = ((settlement.Owner == Hero.MainHero) ? (prisonRoster.TotalManCount - settlement.Party.PrisonerSizeLimit) : MBRandom.RoundRandomized((float)prisonRoster.TotalRegulars * 0.1f));
					if (num > 0)
					{
						TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
						IEnumerable<TroopRosterElement> enumerable;
						if (settlement.Owner != Hero.MainHero)
						{
							enumerable = prisonRoster.GetTroopRoster().AsEnumerable<TroopRosterElement>();
						}
						else
						{
							IEnumerable<TroopRosterElement> enumerable2 = from t in prisonRoster.GetTroopRoster()
								orderby t.Character.Tier
								select t;
							enumerable = enumerable2;
						}
						foreach (TroopRosterElement troopRosterElement in enumerable)
						{
							if (!troopRosterElement.Character.IsHero)
							{
								int num2 = Math.Min(num, troopRosterElement.Number);
								num -= num2;
								troopRoster.AddToCounts(troopRosterElement.Character, num2, false, 0, 0, true, -1);
								if (num <= 0)
								{
									break;
								}
							}
						}
						if (troopRoster.TotalManCount > 0)
						{
							SellPrisonersAction.ApplyForSelectedPrisoners(settlement.Party, null, troopRoster);
						}
					}
				}
			}
		}
	}
}
