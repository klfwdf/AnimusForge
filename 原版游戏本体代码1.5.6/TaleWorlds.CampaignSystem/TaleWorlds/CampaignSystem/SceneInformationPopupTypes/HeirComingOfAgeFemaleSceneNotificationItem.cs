using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class HeirComingOfAgeFemaleSceneNotificationItem : SceneNotificationData
{
	private readonly CampaignTime _creationCampaignTime;

	public Hero MentorHero { get; }

	public Hero HeroCameOfAge { get; }

	public override string SceneID => "scn_hero_come_of_age_female";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("HERO_NAME", HeroCameOfAge.Name);
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			return GameTexts.FindText("str_hero_came_of_age");
		}
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		Equipment equipment = MentorHero.CivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(MentorHero, equipment));
		string childStageEquipmentIDFromCulture = CampaignSceneNotificationHelper.GetChildStageEquipmentIDFromCulture(HeroCameOfAge.Culture);
		Equipment overridenEquipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(childStageEquipmentIDFromCulture).DefaultEquipment.Clone();
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(overriddenBodyProperties: new BodyProperties(new DynamicBodyProperties(6f, HeroCameOfAge.Weight, HeroCameOfAge.Build), HeroCameOfAge.StaticBodyProperties), hero: HeroCameOfAge, overridenEquipment: overridenEquipment));
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(overriddenBodyProperties: new BodyProperties(new DynamicBodyProperties(14f, HeroCameOfAge.Weight, HeroCameOfAge.Build), HeroCameOfAge.StaticBodyProperties), hero: HeroCameOfAge, overridenEquipment: overridenEquipment));
		Equipment equipment2 = HeroCameOfAge.BattleEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(HeroCameOfAge, equipment2));
		return list.ToArray();
	}

	public HeirComingOfAgeFemaleSceneNotificationItem(Hero mentorHero, Hero heroCameOfAge, CampaignTime creationTime)
	{
		MentorHero = mentorHero;
		HeroCameOfAge = heroCameOfAge;
		_creationCampaignTime = creationTime;
	}
}
