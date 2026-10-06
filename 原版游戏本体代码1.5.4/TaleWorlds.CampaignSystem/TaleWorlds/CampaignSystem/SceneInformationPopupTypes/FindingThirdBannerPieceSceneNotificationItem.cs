using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class FindingThirdBannerPieceSceneNotificationItem : SceneNotificationData
{
	private readonly CampaignTime _creationCampaignTime;

	public override string SceneID => "scn_third_banner_piece_notification";

	public override bool IsAffirmativeOptionShown { get; }

	public override TextObject TitleText
	{
		get
		{
			GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(_creationCampaignTime));
			GameTexts.SetVariable("YEAR", _creationCampaignTime.GetYear);
			return GameTexts.FindText("str_third_banner_piece_found");
		}
	}

	public override TextObject AffirmativeTitleText => GameTexts.FindText("str_third_banner_piece_found_assembled");

	public override TextObject AffirmativeText => new TextObject("{=6mgapvxb}Assemble");

	public override TextObject AffirmativeDescriptionText => new TextObject("{=IRLB42FY}Assemble the dragon banner!");

	public override Banner[] GetBanners()
	{
		return new Banner[1] { Hero.MainHero.ClanBanner };
	}

	public FindingThirdBannerPieceSceneNotificationItem()
	{
		IsAffirmativeOptionShown = true;
		_creationCampaignTime = CampaignTime.Now;
	}
}
