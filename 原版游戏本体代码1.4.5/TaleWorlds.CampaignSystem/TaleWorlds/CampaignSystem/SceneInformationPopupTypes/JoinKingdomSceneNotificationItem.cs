using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class JoinKingdomSceneNotificationItem : SceneNotificationData
{
	private const int NumberOfKingdomMembers = 5;

	private readonly CampaignTime _creationCampaignTime;

	public Clan NewMemberClan { get; }

	public Kingdom KingdomToUse { get; }

	public override string SceneID => "scn_cutscene_factionjoin";

	public override RelevantContextType RelevantContext => RelevantContextType.Any;

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("CLAN_NAME", NewMemberClan.Name);
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			GameTexts.SetVariable("KINGDOM_FORMALNAME", CampaignSceneNotificationHelper.GetFormalNameForKingdom(KingdomToUse));
			return GameTexts.FindText("str_new_faction_member");
		}
	}

	public override Banner[] GetBanners()
	{
		return new Banner[2] { KingdomToUse.Banner, KingdomToUse.Banner };
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
		Hero leader = NewMemberClan.Leader;
		Equipment equipment = leader.CivilianEquipment.Clone();
		CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, removeHelmet: true);
		list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(leader, equipment));
		foreach (Hero item in CampaignSceneNotificationHelper.GetMilitaryAudienceForKingdom(KingdomToUse).Take(5))
		{
			Equipment equipment2 = item.CivilianEquipment.Clone();
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment2, removeHelmet: true);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(item, equipment2));
		}
		return list.ToArray();
	}

	public JoinKingdomSceneNotificationItem(Clan newMember, Kingdom kingdom)
	{
		NewMemberClan = newMember;
		KingdomToUse = kingdom;
		_creationCampaignTime = CampaignTime.Now;
	}
}
