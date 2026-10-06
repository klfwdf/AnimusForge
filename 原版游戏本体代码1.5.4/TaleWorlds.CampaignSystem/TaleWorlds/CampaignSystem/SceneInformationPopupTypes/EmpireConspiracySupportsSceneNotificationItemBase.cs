using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public abstract class EmpireConspiracySupportsSceneNotificationItemBase : SceneNotificationData
{
	public Hero King { get; }

	public override string SceneID => "scn_empire_conspiracy_supports_notification";

	public override TextObject AffirmativeText => GameTexts.FindText("str_ok");

	public override Banner[] GetBanners()
	{
		return new Banner[2]
		{
			King.MapFaction.Banner,
			King.MapFaction.Banner
		};
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		Equipment equipment = King.CivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(King, equipment));
		CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("villager_battania");
		Equipment equipment2 = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("conspirator_cutscene_template").DefaultEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2);
		list.Add(new SceneNotificationCharacter(overriddenBodyProperties: characterObject.GetBodyProperties(equipment2, MBRandom.RandomInt(100)), character: characterObject, overriddenEquipment: equipment2, useCivilianEquipment: false, customColor1: 0u, customColor2: 0u));
		list.Add(new SceneNotificationCharacter(overriddenBodyProperties: characterObject.GetBodyProperties(equipment2, MBRandom.RandomInt(100)), character: characterObject, overriddenEquipment: equipment2, useCivilianEquipment: false, customColor1: 0u, customColor2: 0u));
		list.Add(new SceneNotificationCharacter(overriddenBodyProperties: characterObject.GetBodyProperties(equipment2, MBRandom.RandomInt(100)), character: characterObject, overriddenEquipment: equipment2, useCivilianEquipment: false, customColor1: 0u, customColor2: 0u));
		list.Add(CampaignSceneNotificationHelper.GetBodyguardOfCulture(King.MapFaction.Culture));
		list.Add(CampaignSceneNotificationHelper.GetBodyguardOfCulture(King.MapFaction.Culture));
		return list.ToArray();
	}

	protected EmpireConspiracySupportsSceneNotificationItemBase(Hero kingHero)
	{
		King = kingHero;
	}
}
