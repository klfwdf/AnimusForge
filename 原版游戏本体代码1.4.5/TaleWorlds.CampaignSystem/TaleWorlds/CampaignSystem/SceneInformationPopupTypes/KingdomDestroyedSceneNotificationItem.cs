using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class KingdomDestroyedSceneNotificationItem : SceneNotificationData
{
	private const int NumberOfDeadTroops = 2;

	private readonly CampaignTime _creationCampaignTime;

	public Kingdom DestroyedKingdom { get; }

	public override string SceneID => "scn_cutscene_enemykingdom_destroyed";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			GameTexts.SetVariable("FORMAL_NAME", CampaignSceneNotificationHelper.GetFormalNameForKingdom(DestroyedKingdom));
			return GameTexts.FindText("str_kingdom_destroyed_scene_notification");
		}
	}

	public override Banner[] GetBanners()
	{
		return new Banner[1] { DestroyedKingdom.Banner };
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		for (int i = 0; i < 2; i++)
		{
			CharacterObject randomTroopForCulture = CampaignSceneNotificationHelper.GetRandomTroopForCulture(DestroyedKingdom.Culture);
			Equipment equipment = randomTroopForCulture.FirstBattleEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment);
			BodyProperties bodyProperties = randomTroopForCulture.GetBodyProperties(equipment, MBRandom.RandomInt(100));
			list.Add(new SceneNotificationCharacter(randomTroopForCulture, equipment, bodyProperties));
		}
		return list.ToArray();
	}

	public KingdomDestroyedSceneNotificationItem(Kingdom destroyedKingdom, CampaignTime creationTime)
	{
		DestroyedKingdom = destroyedKingdom;
		_creationCampaignTime = creationTime;
	}
}
