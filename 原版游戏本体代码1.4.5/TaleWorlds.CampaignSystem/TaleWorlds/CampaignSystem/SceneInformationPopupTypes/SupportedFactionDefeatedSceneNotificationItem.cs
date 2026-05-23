using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class SupportedFactionDefeatedSceneNotificationItem : SceneNotificationData
{
	private readonly CampaignTime _creationCampaignTime;

	public Kingdom Faction { get; }

	public bool PlayerWantsRestore { get; }

	public override string SceneID => "scn_supported_faction_defeated_notification";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("FORMAL_NAME", CampaignSceneNotificationHelper.GetFormalNameForKingdom(Faction));
			GameTexts.SetVariable("PLAYER_WANTS_RESTORE", PlayerWantsRestore ? 1 : 0);
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			return GameTexts.FindText("str_supported_faction_defeated");
		}
	}

	public override Banner[] GetBanners()
	{
		return new Banner[2] { Faction.Banner, Faction.Banner };
	}

	public SupportedFactionDefeatedSceneNotificationItem(Kingdom faction, bool playerWantsRestore)
	{
		Faction = faction;
		PlayerWantsRestore = playerWantsRestore;
		_creationCampaignTime = CampaignTime.Now;
	}
}
