using System.Linq;
using StoryMode.StoryModeObjects;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace StoryMode.GameComponents;

public class StoryModeVoiceOverModel : VoiceOverModel
{
	private const string Male = "male";

	private const string Female = "female";

	public override string GetSoundPathForCharacter(CharacterObject character, VoiceObject voiceObject)
	{
		if (voiceObject == null)
		{
			return "";
		}
		if (!TutorialPhase.Instance.IsCompleted && TutorialPhase.Instance.TutorialVillageHeadman.CharacterObject == character)
		{
			string text = voiceObject.VoicePaths.First();
			Debug.Print("[VOICEOVER]Sound path found: " + BasePath.Name + text);
			text = text.Replace("$PLATFORM", "PC");
			return text + ".ogg";
		}
		if (StoryModeHeroes.ElderBrother.CharacterObject == character)
		{
			string text2 = "";
			string value = character.StringId + "_" + (CharacterObject.PlayerCharacter.IsFemale ? "female" : "male");
			foreach (string voicePath in voiceObject.VoicePaths)
			{
				if (voicePath.Contains(value))
				{
					text2 = voicePath;
					break;
				}
				if (voicePath.Contains(character.StringId + "_"))
				{
					text2 = voicePath;
				}
			}
			if (string.IsNullOrEmpty(text2))
			{
				return text2;
			}
			Debug.Print("[VOICEOVER]Sound path found: " + BasePath.Name + text2);
			text2 = text2.Replace("$PLATFORM", "PC");
			return text2 + ".ogg";
		}
		return base.BaseModel.GetSoundPathForCharacter(character, voiceObject);
	}

	public override string GetAccentClass(CultureObject culture, bool isHighClass)
	{
		return base.BaseModel.GetAccentClass(culture, isHighClass);
	}
}
