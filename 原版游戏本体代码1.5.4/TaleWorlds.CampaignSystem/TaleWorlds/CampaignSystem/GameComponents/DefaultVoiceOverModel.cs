using System.Collections.Generic;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultVoiceOverModel : VoiceOverModel
{
	private const string ImperialHighClass = "imperial_high";

	private const string ImperialLowClass = "imperial_low";

	private const string VlandianClass = "vlandian";

	private const string SturgianClass = "sturgian";

	private const string KhuzaitClass = "khuzait";

	private const string AseraiClass = "aserai";

	private const string BattanianClass = "battanian";

	private const string ForestBanditClass = "forest_bandits";

	private const string SeaBanditClass = "sea_raiders";

	private const string MountainBanditClass = "mountain_bandits";

	private const string DesertBanditClass = "desert_bandits";

	private const string SteppeBanditClass = "steppe_bandits";

	private const string LootersClass = "looters";

	private const string Male = "male";

	private const string Female = "female";

	private const string GenericPersonaId = "generic";

	public override string GetSoundPathForCharacter(CharacterObject character, VoiceObject voiceObject)
	{
		if (voiceObject == null)
		{
			return "";
		}
		string text = "";
		string value = character.StringId + "_" + (CharacterObject.PlayerCharacter.IsFemale ? "female" : "male");
		foreach (string voicePath in voiceObject.VoicePaths)
		{
			if (voicePath.Contains(value))
			{
				text = voicePath;
				break;
			}
			if (voicePath.Contains(character.StringId + "_"))
			{
				text = voicePath;
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			string accentClass = Campaign.Current.Models.VoiceOverModel.GetAccentClass(character.Culture, ConversationTagHelper.UsesHighRegister(character));
			Debug.Print("accentClass: " + accentClass);
			string text2 = (character.IsFemale ? "female" : "male");
			string stringId = character.GetPersona().StringId;
			List<string> possibleVoicePaths = new List<string>();
			List<string> list = new List<string>();
			list.Add(".+\\\\" + accentClass + "_" + text2 + "_" + stringId + "_.+");
			list.Add(".+\\\\" + accentClass + "_" + text2 + "_generic_.+");
			CheckPossibleMatches(voiceObject, list, ref possibleVoicePaths);
			if (possibleVoicePaths.IsEmpty())
			{
				list.Clear();
				list.Add(".+\\\\" + accentClass + "_" + stringId + "_.+");
				list.Add(".+\\\\" + accentClass + "_generic_.+");
				list.Add(".+\\\\" + text2 + "_" + stringId + "_.+");
				list.Add(".+\\\\" + text2 + "_generic_.+");
				CheckPossibleMatches(voiceObject, list, ref possibleVoicePaths);
				if (possibleVoicePaths.IsEmpty())
				{
					list.Clear();
					list.Add(".+\\\\" + stringId + "_.+");
					list.Add(".+\\\\generic_.+");
					list.Add(".+" + accentClass + "_.+");
					CheckPossibleMatches(voiceObject, list, ref possibleVoicePaths, doubleCheckForGender: true, character.IsFemale);
				}
			}
			if (!possibleVoicePaths.IsEmpty())
			{
				text = (character.IsHero ? possibleVoicePaths[character.HeroObject.RandomInt(possibleVoicePaths.Count)] : ((MobileParty.ConversationParty == null) ? possibleVoicePaths.GetRandomElement() : possibleVoicePaths[MobileParty.ConversationParty.RandomInt(possibleVoicePaths.Count)]));
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		Debug.Print("[VOICEOVER]Sound path found: " + BasePath.Name + text);
		text = text.Replace("$PLATFORM", "PC");
		return text + ".ogg";
	}

	private void CheckPossibleMatches(VoiceObject voiceObject, List<string> possibleMatches, ref List<string> possibleVoicePaths, bool doubleCheckForGender = false, bool isFemale = false)
	{
		foreach (string possibleMatch in possibleMatches)
		{
			Regex regex = new Regex(possibleMatch, RegexOptions.IgnoreCase);
			foreach (string voicePath in voiceObject.VoicePaths)
			{
				if (!regex.Match(voicePath).Success || possibleVoicePaths.Contains(voicePath))
				{
					continue;
				}
				if (doubleCheckForGender)
				{
					if (voicePath.Contains("_male") || voicePath.Contains("_female"))
					{
						string value = (isFemale ? "_female" : "_male");
						if (voicePath.Contains(value))
						{
							possibleVoicePaths.Add(voicePath);
						}
					}
				}
				else
				{
					possibleVoicePaths.Add(voicePath);
				}
			}
		}
	}

	public override string GetAccentClass(CultureObject culture, bool isHighClass)
	{
		if (culture.StringId == "empire")
		{
			if (isHighClass)
			{
				return "imperial_high";
			}
			return "imperial_low";
		}
		if (culture.StringId == "vlandia")
		{
			return "vlandian";
		}
		if (culture.StringId == "sturgia")
		{
			return "sturgian";
		}
		if (culture.StringId == "khuzait")
		{
			return "khuzait";
		}
		if (culture.StringId == "aserai")
		{
			return "aserai";
		}
		if (culture.StringId == "battania")
		{
			return "battanian";
		}
		if (culture.StringId == "forest_bandits")
		{
			return "forest_bandits";
		}
		if (culture.StringId == "sea_raiders")
		{
			return "sea_raiders";
		}
		if (culture.StringId == "mountain_bandits")
		{
			return "mountain_bandits";
		}
		if (culture.StringId == "desert_bandits")
		{
			return "desert_bandits";
		}
		if (culture.StringId == "steppe_bandits")
		{
			return "steppe_bandits";
		}
		if (culture.StringId == "looters")
		{
			return "looters";
		}
		return "";
	}
}
