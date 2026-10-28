using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class ProEmpireConspiracyBeginsSceneNotificationItem : EmpireConspiracySupportsSceneNotificationItemBase
{
	public override TextObject TitleText
	{
		get
		{
			TextObject textObject = GameTexts.FindText("str_empire_conspiracy_supports_proempire");
			textObject.SetTextVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
			textObject.SetTextVariable("YEAR", CampaignTime.Now.GetYear);
			return textObject;
		}
	}

	public ProEmpireConspiracyBeginsSceneNotificationItem(Hero kingHero)
		: base(kingHero)
	{
	}
}
