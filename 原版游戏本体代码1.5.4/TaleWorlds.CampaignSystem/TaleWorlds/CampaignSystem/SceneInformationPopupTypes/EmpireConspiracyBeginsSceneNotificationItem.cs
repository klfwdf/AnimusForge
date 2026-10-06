using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class EmpireConspiracyBeginsSceneNotificationItem : SceneNotificationData
{
	private const int AudienceNumber = 8;

	private readonly uint[] _audienceColors = new uint[4] { 4278914065u, 4284308292u, 4281543757u, 4282199842u };

	private readonly CampaignTime _creationCampaignTime;

	public Hero PlayerHero { get; }

	public Kingdom Empire { get; }

	public bool IsConspiracyAgainstEmpire { get; }

	public override string SceneID => "scn_empire_conspiracy_start_notification";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			if (IsConspiracyAgainstEmpire)
			{
				return GameTexts.FindText("str_empire_conspiracy_begins_antiempire");
			}
			return GameTexts.FindText("str_empire_conspiracy_begins_proempire");
		}
	}

	public override Banner[] GetBanners()
	{
		return new Banner[1] { Empire.Banner };
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		for (int i = 0; i < 8; i++)
		{
			Equipment equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("conspirator_cutscene_template").DefaultEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment);
			CharacterObject facePropertiesFromAudienceIndex = GetFacePropertiesFromAudienceIndex(playerWantsRestore: false, i);
			BodyProperties bodyProperties = facePropertiesFromAudienceIndex.GetBodyProperties(equipment, MBRandom.RandomInt(100));
			uint customColor = _audienceColors[MBRandom.RandomInt(_audienceColors.Length)];
			uint customColor2 = _audienceColors[MBRandom.RandomInt(_audienceColors.Length)];
			list.Add(new SceneNotificationCharacter(facePropertiesFromAudienceIndex, equipment, bodyProperties, useCivilianEquipment: false, customColor, customColor2));
		}
		return list.ToArray();
	}

	public EmpireConspiracyBeginsSceneNotificationItem(Hero playerHero, Kingdom empire, bool isConspiracyAgainstEmpire)
	{
		PlayerHero = playerHero;
		Empire = empire;
		IsConspiracyAgainstEmpire = isConspiracyAgainstEmpire;
		_creationCampaignTime = CampaignTime.Now;
	}

	private CharacterObject GetFacePropertiesFromAudienceIndex(bool playerWantsRestore, int audienceMemberIndex)
	{
		if (!playerWantsRestore)
		{
			return MBObjectManager.Instance.GetObject<CharacterObject>("villager_empire");
		}
		string objectName = (audienceMemberIndex % 8) switch
		{
			0 => "villager_battania", 
			1 => "villager_khuzait", 
			2 => "villager_vlandia", 
			3 => "villager_aserai", 
			4 => "villager_battania", 
			5 => "villager_sturgia", 
			_ => "villager_battania", 
		};
		return MBObjectManager.Instance.GetObject<CharacterObject>(objectName);
	}
}
