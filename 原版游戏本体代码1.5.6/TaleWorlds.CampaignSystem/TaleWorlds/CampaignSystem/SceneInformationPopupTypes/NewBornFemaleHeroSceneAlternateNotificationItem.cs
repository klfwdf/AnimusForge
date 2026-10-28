using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class NewBornFemaleHeroSceneAlternateNotificationItem : SceneNotificationData
{
	private readonly CampaignTime _creationCampaignTime;

	public Hero MaleHero { get; }

	public Hero FemaleHero { get; }

	public override string SceneID => "scn_born_baby_female_hero2";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			GameTexts.SetVariable("MOTHER_NAME", FemaleHero.Name);
			return GameTexts.FindText("str_baby_born_only_mother");
		}
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		Equipment equipment = FemaleHero.CivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true, removeShoulder: true);
		CharacterObject characterObject = CharacterObject.All.First((CharacterObject h) => h.StringId == "cutscene_midwife");
		Equipment equipment2 = characterObject.FirstCivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2);
		list.Add(new SceneNotificationCharacter(null));
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(FemaleHero, equipment));
		list.Add(new SceneNotificationCharacter(characterObject, equipment2));
		return list.ToArray();
	}

	public NewBornFemaleHeroSceneAlternateNotificationItem(Hero maleHero, Hero femaleHero, CampaignTime creationTime)
	{
		MaleHero = maleHero;
		FemaleHero = femaleHero;
		_creationCampaignTime = creationTime;
	}
}
