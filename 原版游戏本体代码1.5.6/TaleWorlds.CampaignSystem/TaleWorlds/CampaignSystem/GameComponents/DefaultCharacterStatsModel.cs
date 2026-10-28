using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCharacterStatsModel : CharacterStatsModel
{
	public override int MaxCharacterTier => 6;

	public override int WoundedHitPointLimit(Hero hero)
	{
		return 20;
	}

	public override int GetTier(CharacterObject character)
	{
		if (character.IsHero)
		{
			return 0;
		}
		return MathF.Min(MathF.Max(MathF.Ceiling(((float)character.Level - 5f) / 5f), 0), Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier);
	}

	public override ExplainedNumber MaxHitpoints(CharacterObject character, bool includeDescriptions = false)
	{
		ExplainedNumber bonuses = new ExplainedNumber(100f, includeDescriptions);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.Trainer, character, isPrimaryBonus: true, ref bonuses);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.ThickHides, character, isPrimaryBonus: true, ref bonuses);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Medicine.DoctorsOath, character, isPrimaryBonus: false, ref bonuses);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Medicine.FortitudeTonic, character, isPrimaryBonus: false, ref bonuses);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.WellBuilt, character, isPrimaryBonus: true, ref bonuses);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.UnwaveringDefense, character, isPrimaryBonus: true, ref bonuses);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Medicine.PreventiveMedicine, character, isPrimaryBonus: true, ref bonuses);
		if (character.IsHero && character.HeroObject.PartyBelongedTo != null && character.HeroObject.PartyBelongedTo.LeaderHero != character.HeroObject)
		{
			PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.FortitudeTonic, character.HeroObject.PartyBelongedTo, isPrimaryBonus: true, ref bonuses);
		}
		PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Athletics.MightyBlow, character, DefaultSkills.Athletics, isPrimaryBonus: false, ref bonuses, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
		return bonuses;
	}
}
