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
		relationChange = Campaign.Current.Models.DiplomacyModel.GetEffectiveRelationChange(originalHero, originalGainedRelationWith, relationChange);
		if (relationChange != 0)
		{
			Campaign.Current.Models.DiplomacyModel.GetHeroesForEffectiveRelation(originalHero, originalGainedRelationWith, out var effectiveHero, out var effectiveHero2);
			int value = CharacterRelationManager.GetHeroRelation(effectiveHero, effectiveHero2) + relationChange;
			value = MBMath.ClampInt(value, -100, 100);
			effectiveHero.SetPersonalRelation(effectiveHero2, value);
			CampaignEventDispatcher.Instance.OnHeroRelationChanged(effectiveHero, effectiveHero2, relationChange, showQuickNotification, detail, originalHero, originalGainedRelationWith);
		}
	}

	private static void ApplyInternalBySet(Hero originalHero, Hero originalGainedRelationWith, int relationAmount, bool showQuickNotification, ChangeRelationDetail detail)
	{
		Campaign.Current.Models.DiplomacyModel.GetHeroesForEffectiveRelation(originalHero, originalGainedRelationWith, out var effectiveHero, out var effectiveHero2);
		int heroRelation = CharacterRelationManager.GetHeroRelation(effectiveHero, effectiveHero2);
		int num = relationAmount - heroRelation;
		if (num != 0)
		{
			effectiveHero.SetPersonalRelation(effectiveHero2, relationAmount);
			CampaignEventDispatcher.Instance.OnHeroRelationChanged(effectiveHero, effectiveHero2, num, showQuickNotification, detail, originalHero, originalGainedRelationWith);
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

	public static void SetRelationBetweenHeroes(Hero hero, Hero gainedRelationWith, int newRelation, bool showQuickNotification = true)
	{
		ApplyInternalBySet(hero, gainedRelationWith, newRelation, showQuickNotification, ChangeRelationDetail.Default);
	}
}
