using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class KingdomCreatedSceneNotificationItem : SceneNotificationData
{
	private const int NumberOfKingdomMemberAudience = 5;

	private readonly CampaignTime _creationCampaignTime;

	public Kingdom NewKingdom { get; }

	public override string SceneID => "scn_kingdom_made";

	public override bool PauseActiveState => false;

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("KINGDOM_NAME", NewKingdom.Name);
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			GameTexts.SetVariable("LEADER_NAME", NewKingdom.Leader.Name);
			return GameTexts.FindText("str_kingdom_created");
		}
	}

	public override TextObject AffirmativeText => GameTexts.FindText("str_ok");

	public override Banner[] GetBanners()
	{
		return new Banner[2] { NewKingdom.Banner, NewKingdom.Banner };
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		Hero leader = NewKingdom.Leader;
		Equipment equipment = leader.BattleEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(leader, equipment));
		foreach (Hero item in CampaignSceneNotificationHelper.GetMilitaryAudienceForKingdom(NewKingdom, includeKingdomLeader: false).Take(5))
		{
			Equipment equipment2 = item.CivilianEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2, removeHelmet: true);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(item, equipment2));
		}
		return list.ToArray();
	}

	public KingdomCreatedSceneNotificationItem(Kingdom newKingdom)
	{
		NewKingdom = newKingdom;
		_creationCampaignTime = CampaignTime.Now;
	}
}
