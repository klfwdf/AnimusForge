using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class HeroDailyXpCampaignBehavior : CampaignBehaviorBase
{
	private const float XpPerFocusPointForLords = 200f;

	private const float XpPerFocusPointForWanderers = 100f;

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private static void DailyTickHero(Hero hero)
	{
		if (!IsEligibleForDailyXp(hero))
		{
			return;
		}
		float rawXp = (hero.IsWanderer ? 100f : 200f);
		foreach (SkillObject item in Skills.All)
		{
			if (hero.HeroDeveloper.GetFocus(item) > 0)
			{
				hero.HeroDeveloper.AddSkillXp(item, rawXp, isAffectedByFocusFactor: true, shouldNotify: false);
			}
		}
	}

	private static bool IsEligibleForDailyXp(Hero hero)
	{
		if (hero == Hero.MainHero || !hero.IsActive || hero.IsChild || hero.IsTemplate || hero.PartyBelongedTo?.MapEvent != null)
		{
			return false;
		}
		Settlement currentSettlement = hero.CurrentSettlement;
		if (currentSettlement != null && currentSettlement.IsFortification && hero.GovernorOf != hero.CurrentSettlement.Town)
		{
			return false;
		}
		if (!hero.IsLord && !hero.IsWanderer)
		{
			return hero.IsPlayerCompanion;
		}
		return true;
	}
}
