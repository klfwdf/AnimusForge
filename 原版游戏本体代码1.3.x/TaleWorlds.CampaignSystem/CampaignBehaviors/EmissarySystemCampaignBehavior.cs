using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003EB RID: 1003
	public class EmissarySystemCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003DBC RID: 15804 RVA: 0x00112765 File Offset: 0x00110965
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
		}

		// Token: 0x06003DBD RID: 15805 RVA: 0x0011277E File Offset: 0x0011097E
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003DBE RID: 15806 RVA: 0x00112780 File Offset: 0x00110980
		private void DailyTick()
		{
			EmissaryModel emissaryModel = Campaign.Current.Models.EmissaryModel;
			foreach (Hero hero in Clan.PlayerClan.Heroes)
			{
				if (emissaryModel.IsEmissary(hero))
				{
					float num = MBMath.ClampFloat(0.05f + 0.05f * ((float)hero.GetSkillValue(DefaultSkills.Charm) / 300f), 0f, 1f);
					if (MBRandom.RandomFloat <= num)
					{
						bool flag = MBRandom.RandomFloat <= 0.5f;
						if (!flag)
						{
							goto IL_B8;
						}
						if (!hero.CurrentSettlement.HeroesWithoutParty.Any((Hero h) => h.Occupation == Occupation.Lord))
						{
							goto IL_B8;
						}
						bool flag2 = true;
						IL_103:
						if (!flag2)
						{
							Hero randomElement = hero.CurrentSettlement.Notables.GetRandomElement<Hero>();
							if (randomElement != null)
							{
								ChangeRelationAction.ApplyEmissaryRelation(hero, randomElement, emissaryModel.EmissaryRelationBonusForMainClan, true);
								continue;
							}
							continue;
						}
						else
						{
							Hero randomElementWithPredicate = hero.CurrentSettlement.HeroesWithoutParty.GetRandomElementWithPredicate((Hero n) => !n.IsPrisoner && n.CharacterObject.Occupation == Occupation.Lord && n.Clan != Clan.PlayerClan);
							if (randomElementWithPredicate != null)
							{
								ChangeRelationAction.ApplyEmissaryRelation(hero, randomElementWithPredicate, emissaryModel.EmissaryRelationBonusForMainClan, true);
								continue;
							}
							continue;
						}
						IL_B8:
						if (!flag && hero.CurrentSettlement.Notables.Count == 0)
						{
							flag2 = hero.CurrentSettlement.HeroesWithoutParty.Any((Hero h) => h.Occupation == Occupation.Lord);
							goto IL_103;
						}
						flag2 = false;
						goto IL_103;
					}
				}
			}
		}
	}
}
