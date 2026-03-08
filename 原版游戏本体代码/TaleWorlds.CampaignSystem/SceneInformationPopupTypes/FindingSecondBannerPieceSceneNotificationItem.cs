using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000BB RID: 187
	public class FindingSecondBannerPieceSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000551 RID: 1361
		// (get) Token: 0x060013C7 RID: 5063 RVA: 0x0005BCAE File Offset: 0x00059EAE
		public Hero PlayerHero { get; }

		// Token: 0x17000552 RID: 1362
		// (get) Token: 0x060013C8 RID: 5064 RVA: 0x0005BCB6 File Offset: 0x00059EB6
		public override string SceneID
		{
			get
			{
				return "scn_second_banner_piece_notification";
			}
		}

		// Token: 0x17000553 RID: 1363
		// (get) Token: 0x060013C9 RID: 5065 RVA: 0x0005BCC0 File Offset: 0x00059EC0
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				return GameTexts.FindText("str_second_banner_piece_found", null);
			}
		}

		// Token: 0x060013CA RID: 5066 RVA: 0x0005BD05 File Offset: 0x00059F05
		public override Banner[] GetBanners()
		{
			return new Banner[] { this.PlayerHero.ClanBanner };
		}

		// Token: 0x060013CB RID: 5067 RVA: 0x0005BD1B File Offset: 0x00059F1B
		public FindingSecondBannerPieceSceneNotificationItem(Hero playerHero)
		{
			this.PlayerHero = playerHero;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x04000683 RID: 1667
		private readonly CampaignTime _creationCampaignTime;
	}
}
