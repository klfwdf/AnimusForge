using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Conversation.Tags
{
	// Token: 0x02000299 RID: 665
	public static class ConversationTagHelper
	{
		// Token: 0x060023B1 RID: 9137 RVA: 0x00099636 File Offset: 0x00097836
		public static bool UsesHighRegister(CharacterObject character)
		{
			return ConversationTagHelper.EducatedClass(character) && !ConversationTagHelper.TribalVoiceGroup(character);
		}

		// Token: 0x060023B2 RID: 9138 RVA: 0x0009964B File Offset: 0x0009784B
		public static bool UsesLowRegister(CharacterObject character)
		{
			return !ConversationTagHelper.EducatedClass(character) && !ConversationTagHelper.TribalVoiceGroup(character);
		}

		// Token: 0x060023B3 RID: 9139 RVA: 0x00099660 File Offset: 0x00097860
		public static bool TribalVoiceGroup(CharacterObject character)
		{
			return character.Culture.StringId == "sturgia" || character.Culture.StringId == "aserai" || character.Culture.StringId == "khuzait" || character.Culture.StringId == "battania" || character.Culture.StringId == "vlandia" || character.Culture.StringId == "nord" || character.Culture.StringId == "vakken";
		}

		// Token: 0x060023B4 RID: 9140 RVA: 0x00099714 File Offset: 0x00097914
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

		// Token: 0x060023B5 RID: 9141 RVA: 0x00099790 File Offset: 0x00097990
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
}
