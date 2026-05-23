using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B9 RID: 185
	public class ProEmpireConspiracyBeginsSceneNotificationItem : EmpireConspiracySupportsSceneNotificationItemBase
	{
		// Token: 0x1700054D RID: 1357
		// (get) Token: 0x060013C0 RID: 5056 RVA: 0x0005BBD0 File Offset: 0x00059DD0
		public override TextObject TitleText
		{
			get
			{
				TextObject textObject = GameTexts.FindText("str_empire_conspiracy_supports_proempire", null);
				textObject.SetTextVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
				textObject.SetTextVariable("YEAR", CampaignTime.Now.GetYear);
				return textObject;
			}
		}

		// Token: 0x060013C1 RID: 5057 RVA: 0x0005BC17 File Offset: 0x00059E17
		public ProEmpireConspiracyBeginsSceneNotificationItem(Hero kingHero)
			: base(kingHero)
		{
		}
	}
}
