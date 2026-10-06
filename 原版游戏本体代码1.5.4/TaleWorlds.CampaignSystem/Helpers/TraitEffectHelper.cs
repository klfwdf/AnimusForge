using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class TraitEffectHelper
{
	public static void ApplyTraitEffect(Hero hero, TraitEffectObject effect, ref ExplainedNumber result)
	{
		float traitEffectBonus = GetTraitEffectBonus(hero, effect);
		if (traitEffectBonus != 0f)
		{
			if (effect.IncrementType == EffectIncrementType.Add)
			{
				result.Add(traitEffectBonus, new TextObject("{=ENta0wCu}Personality"));
			}
			else if (effect.IncrementType == EffectIncrementType.AddFactor)
			{
				result.AddFactor(traitEffectBonus, new TextObject("{=ENta0wCu}Personality"));
			}
			else
			{
				Debug.FailedAssert("effect.IncrementType is out of range!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "ApplyTraitEffect", 3530);
			}
		}
	}

	public static float GetTraitEffectBonus(Hero hero, TraitEffectObject effect)
	{
		int traitLevel = hero.GetTraitLevel(effect.Trait);
		if (traitLevel == 0)
		{
			return 0f;
		}
		float bonus = effect.GetBonus(traitLevel);
		if (bonus == 0f)
		{
			return 0f;
		}
		return bonus;
	}
}
