using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCVoiceOverModel : VoiceOverModel
{
	private const string NordClass = "nord";

	private const string CultureSouthernPirates = "southern_pirates";

	private const string SouthernPiratesClass = "southern_pirates";

	public override string GetSoundPathForCharacter(CharacterObject character, VoiceObject voiceObject)
	{
		return base.BaseModel.GetSoundPathForCharacter(character, voiceObject);
	}

	public override string GetAccentClass(CultureObject culture, bool isHighClass)
	{
		if (culture.StringId == "nord")
		{
			return "nord";
		}
		if (culture.StringId == "southern_pirates")
		{
			return "southern_pirates";
		}
		return base.BaseModel.GetAccentClass(culture, isHighClass);
	}
}
