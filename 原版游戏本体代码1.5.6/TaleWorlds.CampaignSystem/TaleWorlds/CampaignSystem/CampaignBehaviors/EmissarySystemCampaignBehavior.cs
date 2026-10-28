using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class EmissarySystemCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void DailyTick()
	{
		EmissaryModel emissaryModel = Campaign.Current.Models.EmissaryModel;
		foreach (Hero hero in Clan.PlayerClan.Heroes)
		{
			if (!emissaryModel.IsEmissary(hero))
			{
				continue;
			}
			float num = MBMath.ClampFloat(0.05f + 0.05f * ((float)hero.GetSkillValue(DefaultSkills.Charm) / 300f), 0f, 1f);
			if (!(MBRandom.RandomFloat <= num))
			{
				continue;
			}
			bool flag = MBRandom.RandomFloat <= 0.5f;
			if ((!flag || !hero.CurrentSettlement.HeroesWithoutParty.Any((Hero h) => h.Occupation == Occupation.Lord)) && (flag || hero.CurrentSettlement.Notables.Count != 0 || !hero.CurrentSettlement.HeroesWithoutParty.Any((Hero h) => h.Occupation == Occupation.Lord)))
			{
				Hero randomElement = hero.CurrentSettlement.Notables.GetRandomElement();
				if (randomElement != null)
				{
					ChangeRelationAction.ApplyEmissaryRelation(hero, randomElement, emissaryModel.EmissaryRelationBonusForMainClan);
				}
				continue;
			}
			Hero randomElementWithPredicate = hero.CurrentSettlement.HeroesWithoutParty.GetRandomElementWithPredicate((Hero n) => !n.IsPrisoner && n.CharacterObject.Occupation == Occupation.Lord && n.Clan != Clan.PlayerClan);
			if (randomElementWithPredicate != null)
			{
				ChangeRelationAction.ApplyEmissaryRelation(hero, randomElementWithPredicate, emissaryModel.EmissaryRelationBonusForMainClan);
			}
		}
	}
}
