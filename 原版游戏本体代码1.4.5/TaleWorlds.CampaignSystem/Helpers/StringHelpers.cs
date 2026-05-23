using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class StringHelpers
{
	public static string SplitCamelCase(string text)
	{
		return Regex.Replace(text, "((?<=\\p{Ll})\\p{Lu})|((?!\\A)\\p{Lu}(?>\\p{Ll}))", " $0");
	}

	public static string CamelCaseToSnakeCase(string camelCaseString)
	{
		string replacement = "_$1$2";
		return new Regex("((?<=.)[A-Z][a-zA-Z]*)|((?<=[a-zA-Z])\\d+)").Replace(camelCaseString, replacement).ToLower();
	}

	public static void SetSettlementProperties(string tag, Settlement settlement, TextObject parent = null, bool isRepeatable = false)
	{
		TextObject empty = TextObject.GetEmpty();
		empty.SetTextVariable("NAME", settlement.Name);
		empty.SetTextVariable("LINK", settlement.EncyclopediaLinkWithName);
		if (!isRepeatable)
		{
			if (parent != null)
			{
				parent.SetTextVariable(tag, empty);
			}
			else
			{
				MBTextManager.SetTextVariable(tag, empty);
			}
		}
		else
		{
			ConversationSentence.SelectedRepeatLine.SetTextVariable(tag, empty);
		}
	}

	public static void SetRepeatableCharacterProperties(string tag, CharacterObject character, bool includeDetails = false)
	{
		TextObject characterProperties = GetCharacterProperties(character, includeDetails);
		ConversationSentence.SelectedRepeatLine.SetTextVariable(tag, characterProperties);
	}

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

	public static TextObject SetCharacterProperties(string tag, CharacterObject character, TextObject parent = null, bool includeDetails = false)
	{
		TextObject characterProperties = GetCharacterProperties(character, includeDetails);
		if (parent != null)
		{
			parent.SetTextVariable(tag, characterProperties);
		}
		else
		{
			MBTextManager.SetTextVariable(tag, characterProperties);
		}
		return characterProperties;
	}

	public static void SetEffectIncrementTypeTextVariable(string tag, TextObject description, float bonus, EffectIncrementType effectIncrementType)
	{
		float num = ((effectIncrementType == EffectIncrementType.AddFactor) ? (bonus * 100f) : bonus);
		string text = $"{num:0.#}";
		if (bonus > 0f)
		{
			description.SetTextVariable(tag, "+" + text);
		}
		else
		{
			description.SetTextVariable(tag, text ?? "");
		}
	}

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
