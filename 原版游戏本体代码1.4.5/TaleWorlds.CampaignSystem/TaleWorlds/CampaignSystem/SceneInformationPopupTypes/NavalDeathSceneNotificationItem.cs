using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class NavalDeathSceneNotificationItem : SceneNotificationData
{
	private readonly CampaignTime _creationCampaignTime;

	public Hero DeadHero { get; }

	public override string SceneID => "scn_cutscene_main_hero_naval_battle_death";

	public KillCharacterAction.KillCharacterActionDetail KillDetail { get; private set; }

	public override NotificationSceneProperties SceneProperties => new NotificationSceneProperties
	{
		InitializePhysics = true,
		DisableStaticShadows = true,
		OverriddenWaterStrength = null
	};

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			GameTexts.SetVariable("NAME", DeadHero.Name);
			if (KillDetail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
			{
				return GameTexts.FindText("str_main_hero_battle_death");
			}
			if (KillDetail == KillCharacterAction.KillCharacterActionDetail.DiedInLabor)
			{
				return GameTexts.FindText("str_main_hero_battle_death_in_labor");
			}
			if (KillDetail == KillCharacterAction.KillCharacterActionDetail.Executed || KillDetail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
			{
				return GameTexts.FindText("str_main_hero_battle_executed");
			}
			if (KillDetail == KillCharacterAction.KillCharacterActionDetail.Murdered)
			{
				return GameTexts.FindText("str_main_hero_battle_murdered");
			}
			return GameTexts.FindText("str_family_member_death");
		}
	}

	public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
	{
		return Array.Empty<SceneNotificationCharacter>();
	}

	public override SceneNotificationShip[] GetShips()
	{
		return Array.Empty<SceneNotificationShip>();
	}

	public NavalDeathSceneNotificationItem(Hero deadHero, CampaignTime creationTime, KillCharacterAction.KillCharacterActionDetail killDetail)
	{
		DeadHero = deadHero;
		_creationCampaignTime = creationTime;
		KillDetail = killDetail;
	}
}
