using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200007A RID: 122
	public class CampaignTutorial
	{
		// Token: 0x170003EA RID: 1002
		// (get) Token: 0x06001061 RID: 4193 RVA: 0x0004DACB File Offset: 0x0004BCCB
		public TextObject Description
		{
			get
			{
				return GameTexts.FindText("str_campaign_tutorial_description", this.TutorialTypeId);
			}
		}

		// Token: 0x170003EB RID: 1003
		// (get) Token: 0x06001062 RID: 4194 RVA: 0x0004DADD File Offset: 0x0004BCDD
		public TextObject Title
		{
			get
			{
				return GameTexts.FindText("str_campaign_tutorial_title", this.TutorialTypeId);
			}
		}

		// Token: 0x06001063 RID: 4195 RVA: 0x0004DAEF File Offset: 0x0004BCEF
		public CampaignTutorial(string tutorialType, int priority)
		{
			this.TutorialTypeId = tutorialType;
			this.Priority = priority;
		}

		// Token: 0x04000498 RID: 1176
		public readonly string TutorialTypeId;

		// Token: 0x04000499 RID: 1177
		public readonly int Priority;
	}
}
