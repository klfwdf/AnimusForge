using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class FindingFirstBannerPieceSceneNotificationItem : SceneNotificationData
{
	private readonly Action _onCloseAction;

	private readonly CampaignTime _creationCampaignTime;

	public Hero PlayerHero { get; }

	public override string SceneID => "scn_first_banner_piece_notification";

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			return GameTexts.FindText("str_first_banner_piece_found");
		}
	}

	public override void OnCloseAction()
	{
		base.OnCloseAction();
		_onCloseAction?.Invoke();
	}

	public FindingFirstBannerPieceSceneNotificationItem(Hero playerHero, Action onCloseAction = null)
	{
		PlayerHero = playerHero;
		_creationCampaignTime = CampaignTime.Now;
		_onCloseAction = onCloseAction;
	}
}
