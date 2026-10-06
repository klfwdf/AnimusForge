using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

public class AntiEmpireConspiracyBeginsSceneNotificationItem : EmpireConspiracySupportsSceneNotificationItemBase
{
	private readonly List<Kingdom> _antiEmpireFactions;

	public override TextObject TitleText
	{
		get
		{
			List<TextObject> list = new List<TextObject>();
			foreach (Kingdom antiEmpireFaction in _antiEmpireFactions)
			{
				list.Add(antiEmpireFaction.InformalName);
			}
			TextObject textObject = GameTexts.FindText("str_empire_conspiracy_supports_antiempire");
			textObject.SetTextVariable("FACTION_NAMES", GameTexts.GameTextHelper.MergeTextObjectsWithComma(list, includeAnd: true));
			textObject.SetTextVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
			textObject.SetTextVariable("YEAR", CampaignTime.Now.GetYear);
			return textObject;
		}
	}

	public AntiEmpireConspiracyBeginsSceneNotificationItem(Hero kingHero, List<Kingdom> antiEmpireFactions)
		: base(kingHero)
	{
		_antiEmpireFactions = antiEmpireFactions;
	}
}
