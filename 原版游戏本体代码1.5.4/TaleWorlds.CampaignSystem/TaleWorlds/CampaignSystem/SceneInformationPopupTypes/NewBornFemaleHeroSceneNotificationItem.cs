using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class NewBornFemaleHeroSceneNotificationItem : SceneNotificationData
{
	private readonly CampaignTime _creationCampaignTime;

	public Hero MaleHero { get; }

	public Hero FemaleHero { get; }

	public override string SceneID => "scn_born_baby_female_hero";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("MOTHER_NAME", FemaleHero.Name);
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			return GameTexts.FindText("str_baby_born_only_mother");
		}
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		CharacterObject characterObject = CharacterObject.All.First((CharacterObject h) => h.StringId == "cutscene_midwife");
		Equipment equipment = MaleHero.CivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true);
		Equipment equipment2 = FemaleHero.CivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2, removeHelmet: true, removeShoulder: true);
		Equipment equipment3 = characterObject.FirstCivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment3);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(MaleHero, equipment));
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(FemaleHero, equipment2));
		list.Add(new SceneNotificationCharacter(characterObject, equipment3));
		return list.ToArray();
	}

	public NewBornFemaleHeroSceneNotificationItem(Hero maleHero, Hero femaleHero, CampaignTime creationTime)
	{
		MaleHero = maleHero;
		FemaleHero = femaleHero;
		_creationCampaignTime = creationTime;
	}
}
