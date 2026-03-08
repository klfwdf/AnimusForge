using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B8 RID: 184
	public class AntiEmpireConspiracyBeginsSceneNotificationItem : EmpireConspiracySupportsSceneNotificationItemBase
	{
		// Token: 0x1700054C RID: 1356
		// (get) Token: 0x060013BE RID: 5054 RVA: 0x0005BB14 File Offset: 0x00059D14
		public override TextObject TitleText
		{
			get
			{
				List<TextObject> list = new List<TextObject>();
				foreach (Kingdom kingdom in this._antiEmpireFactions)
				{
					list.Add(kingdom.InformalName);
				}
				TextObject textObject = GameTexts.FindText("str_empire_conspiracy_supports_antiempire", null);
				textObject.SetTextVariable("FACTION_NAMES", GameTexts.GameTextHelper.MergeTextObjectsWithComma(list, true));
				textObject.SetTextVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(CampaignTime.Now));
				textObject.SetTextVariable("YEAR", CampaignTime.Now.GetYear);
				return textObject;
			}
		}

		// Token: 0x060013BF RID: 5055 RVA: 0x0005BBC0 File Offset: 0x00059DC0
		public AntiEmpireConspiracyBeginsSceneNotificationItem(Hero kingHero, List<Kingdom> antiEmpireFactions)
			: base(kingHero)
		{
			this._antiEmpireFactions = antiEmpireFactions;
		}

		// Token: 0x0400067E RID: 1662
		private readonly List<Kingdom> _antiEmpireFactions;
	}
}
