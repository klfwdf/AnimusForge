using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeRelationAction
{
	public enum ChangeRelationDetail
	{
		Default,
		Emissary
	}

	private static void ApplyInternal(Hero originalHero, Hero originalGainedRelationWith, int relationChange, bool showQuickNotification, ChangeRelationDetail detail)
	{
		if (relationChange > 0)
		{
			relationChange = MBRandom.RoundRandomized(Campaign.Current.Models.DiplomacyModel.GetRelationIncreaseFactor(originalHero, originalGainedRelationWith, relationChange));
		}
		if (relationChange != 0)
		{
			Campaign.Current.Models.DiplomacyModel.GetHeroesForEffectiveRelation(originalHero, originalGainedRelationWith, out var effectiveHero, out var effectiveHero2);
			int value = CharacterRelationManager.GetHeroRelation(effectiveHero, effectiveHero2) + relationChange;
			value = MBMath.ClampInt(value, -100, 100);
			effectiveHero.SetPersonalRelation(effectiveHero2, value);
			CampaignEventDispatcher.Instance.OnHeroRelationChanged(effectiveHero, effectiveHero2, relationChange, showQuickNotification, detail, originalHero, originalGainedRelationWith);
		}
	}

	public static void ApplyPlayerRelation(Hero gainedRelationWith, int relation, bool affectRelatives = true, bool showQuickNotification = true)
	{
		ApplyInternal(Hero.MainHero, gainedRelationWith, relation, showQuickNotification, ChangeRelationDetail.Default);
	}

	public static void ApplyRelationChangeBetweenHeroes(Hero hero, Hero gainedRelationWith, int relationChange, bool showQuickNotification = true)
	{
		ApplyInternal(hero, gainedRelationWith, relationChange, showQuickNotification, ChangeRelationDetail.Default);
	}

	public static void ApplyEmissaryRelation(Hero emissary, Hero gainedRelationWith, int relationChange, bool showQuickNotification = true)
	{
		ApplyInternal(emissary, gainedRelationWith, relationChange, showQuickNotification, ChangeRelationDetail.Emissary);
	}
}
