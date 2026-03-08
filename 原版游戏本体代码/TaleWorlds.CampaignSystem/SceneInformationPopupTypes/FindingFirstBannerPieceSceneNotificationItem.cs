using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000BA RID: 186
	public class FindingFirstBannerPieceSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x1700054E RID: 1358
		// (get) Token: 0x060013C2 RID: 5058 RVA: 0x0005BC20 File Offset: 0x00059E20
		public Hero PlayerHero { get; }

		// Token: 0x1700054F RID: 1359
		// (get) Token: 0x060013C3 RID: 5059 RVA: 0x0005BC28 File Offset: 0x00059E28
		public override string SceneID
		{
			get
			{
				return "scn_first_banner_piece_notification";
			}
		}

		// Token: 0x17000550 RID: 1360
		// (get) Token: 0x060013C4 RID: 5060 RVA: 0x0005BC30 File Offset: 0x00059E30
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				return GameTexts.FindText("str_first_banner_piece_found", null);
			}
		}

		// Token: 0x060013C5 RID: 5061 RVA: 0x0005BC75 File Offset: 0x00059E75
		public override void OnCloseAction()
		{
			base.OnCloseAction();
			Action onCloseAction = this._onCloseAction;
			if (onCloseAction == null)
			{
				return;
			}
			onCloseAction();
		}

		// Token: 0x060013C6 RID: 5062 RVA: 0x0005BC8D File Offset: 0x00059E8D
		public FindingFirstBannerPieceSceneNotificationItem(Hero playerHero, Action onCloseAction = null)
		{
			this.PlayerHero = playerHero;
			this._creationCampaignTime = CampaignTime.Now;
			this._onCloseAction = onCloseAction;
		}

		// Token: 0x04000680 RID: 1664
		private readonly Action _onCloseAction;

		// Token: 0x04000681 RID: 1665
		private readonly CampaignTime _creationCampaignTime;
	}
}
