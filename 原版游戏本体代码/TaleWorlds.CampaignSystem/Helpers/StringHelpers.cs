using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x02000006 RID: 6
	public static class StringHelpers
	{
		// Token: 0x06000019 RID: 25 RVA: 0x000034A0 File Offset: 0x000016A0
		public static string SplitCamelCase(string text)
		{
			return Regex.Replace(text, "((?<=\\p{Ll})\\p{Lu})|((?!\\A)\\p{Lu}(?>\\p{Ll}))", " $0");
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000034B4 File Offset: 0x000016B4
		public static string CamelCaseToSnakeCase(string camelCaseString)
		{
			string pattern = "((?<=.)[A-Z][a-zA-Z]*)|((?<=[a-zA-Z])\\d+)";
			string replacement = "_$1$2";
			return new Regex(pattern).Replace(camelCaseString, replacement).ToLower();
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000034E0 File Offset: 0x000016E0
		public static void SetSettlementProperties(string tag, Settlement settlement, TextObject parent = null, bool isRepeatable = false)
		{
			TextObject empty = TextObject.GetEmpty();
			empty.SetTextVariable("NAME", settlement.Name);
			empty.SetTextVariable("LINK", settlement.EncyclopediaLinkWithName);
			if (isRepeatable)
			{
				ConversationSentence.SelectedRepeatLine.SetTextVariable(tag, empty);
				return;
			}
			if (parent != null)
			{
				parent.SetTextVariable(tag, empty);
				return;
			}
			MBTextManager.SetTextVariable(tag, empty, false);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00003544 File Offset: 0x00001744
		public static void SetRepeatableCharacterProperties(string tag, CharacterObject character, bool includeDetails = false)
		{
			TextObject characterProperties = StringHelpers.GetCharacterProperties(character, includeDetails);
			ConversationSentence.SelectedRepeatLine.SetTextVariable(tag, characterProperties);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00003568 File Offset: 0x00001768
		private static TextObject GetCharacterProperties(CharacterObject character, bool includeDetails)
		{
			TextObject empty = TextObject.GetEmpty();
			empty.SetTextVariable("NAME", character.Name);
			empty.SetTextVariable("GENDER", character.IsFemale ? 1 : 0);
			empty.SetTextVariable("LINK", character.EncyclopediaLinkWithName);
			if (character.IsHero)
			{
				if (character.HeroObject.FirstName != null)
				{
					empty.SetTextVariable("FIRSTNAME", character.HeroObject.FirstName);
				}
				else
				{
					empty.SetTextVariable("FIRSTNAME", character.Name);
				}
				if (includeDetails)
				{
					empty.SetTextVariable("AGE", (int)MathF.Round(character.Age, 2));
					if (character.HeroObject.MapFaction != null)
					{
						empty.SetTextVariable("FACTION", character.HeroObject.MapFaction.Name);
					}
					else
					{
						empty.SetTextVariable("FACTION", character.Culture.Name);
					}
					if (character.HeroObject.Clan != null)
					{
						empty.SetTextVariable("CLAN", character.HeroObject.Clan.Name);
					}
					else
					{
						empty.SetTextVariable("CLAN", character.Culture.Name);
					}
				}
			}
			return empty;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000036A4 File Offset: 0x000018A4
		public static TextObject SetCharacterProperties(string tag, CharacterObject character, TextObject parent = null, bool includeDetails = false)
		{
			TextObject characterProperties = StringHelpers.GetCharacterProperties(character, includeDetails);
			if (parent != null)
			{
				parent.SetTextVariable(tag, characterProperties);
			}
			else
			{
				MBTextManager.SetTextVariable(tag, characterProperties, false);
			}
			return characterProperties;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000036D8 File Offset: 0x000018D8
		public static void SetEffectIncrementTypeTextVariable(string tag, TextObject description, float bonus, EffectIncrementType effectIncrementType)
		{
			float num = ((effectIncrementType == EffectIncrementType.AddFactor) ? (bonus * 100f) : bonus);
			string text = string.Format("{0:0.#}", num);
			if (bonus > 0f)
			{
				description.SetTextVariable(tag, "+" + text);
				return;
			}
			description.SetTextVariable(tag, text ?? "");
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00003734 File Offset: 0x00001934
		public static string RemoveDiacritics(string originalText)
		{
			originalText = originalText.Normalize(NormalizationForm.FormD);
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < originalText.Length; i++)
			{
				if (CharUnicodeInfo.GetUnicodeCategory(originalText[i]) != UnicodeCategory.NonSpacingMark)
				{
					stringBuilder.Append(originalText[i]);
				}
			}
			return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
		}
	}
}
