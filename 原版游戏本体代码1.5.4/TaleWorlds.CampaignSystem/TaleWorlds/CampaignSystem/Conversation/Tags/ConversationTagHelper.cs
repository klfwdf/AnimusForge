using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Conversation.Tags;

public static class ConversationTagHelper
{
	public static bool UsesHighRegister(CharacterObject character)
	{
		if (EducatedClass(character))
		{
			return !TribalVoiceGroup(character);
		}
		return false;
	}

	public static bool UsesLowRegister(CharacterObject character)
	{
		if (!EducatedClass(character))
		{
			return !TribalVoiceGroup(character);
		}
		return false;
	}

	public static bool TribalVoiceGroup(CharacterObject character)
	{
		if (!(character.Culture.StringId == "sturgia") && !(character.Culture.StringId == "aserai") && !(character.Culture.StringId == "khuzait") && !(character.Culture.StringId == "battania") && !(character.Culture.StringId == "vlandia") && !(character.Culture.StringId == "nord"))
		{
			return character.Culture.StringId == "vakken";
		}
		return true;
	}

	public static bool EducatedClass(CharacterObject character)
	{
		bool result = false;
		if (character.HeroObject != null)
		{
			Clan clan = character.HeroObject.Clan;
			if (clan != null && clan.IsNoble)
			{
				result = true;
			}
			if (character.HeroObject.IsMerchant)
			{
				result = true;
			}
			if (character.HeroObject.GetTraitLevel(DefaultTraits.Siegecraft) >= 5 || character.HeroObject.GetTraitLevel(DefaultTraits.Surgery) >= 5)
			{
				result = true;
			}
			if (character.HeroObject.IsGangLeader)
			{
				result = false;
			}
		}
		return result;
	}

	public static int TraitCompatibility(Hero hero1, Hero hero2, TraitObject trait)
	{
		int traitLevel = hero1.GetTraitLevel(trait);
		int traitLevel2 = hero2.GetTraitLevel(trait);
		if (traitLevel > 0 && traitLevel2 > 0)
		{
			return 1;
		}
		if (traitLevel < 0 || traitLevel2 < 0)
		{
			return MathF.Abs(traitLevel - traitLevel2) * -1;
		}
		return 0;
	}
}
