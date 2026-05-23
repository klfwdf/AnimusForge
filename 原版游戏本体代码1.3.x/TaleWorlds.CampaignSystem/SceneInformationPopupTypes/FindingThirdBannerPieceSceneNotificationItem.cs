using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000BC RID: 188
	public class FindingThirdBannerPieceSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000554 RID: 1364
		// (get) Token: 0x060013CC RID: 5068 RVA: 0x0005BD35 File Offset: 0x00059F35
		public override string SceneID
		{
			get
			{
				return "scn_third_banner_piece_notification";
			}
		}

		// Token: 0x17000555 RID: 1365
		// (get) Token: 0x060013CD RID: 5069 RVA: 0x0005BD3C File Offset: 0x00059F3C
		public override bool IsAffirmativeOptionShown { get; }

		// Token: 0x17000556 RID: 1366
		// (get) Token: 0x060013CE RID: 5070 RVA: 0x0005BD44 File Offset: 0x00059F44
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				return GameTexts.FindText("str_third_banner_piece_found", null);
			}
		}

		// Token: 0x17000557 RID: 1367
		// (get) Token: 0x060013CF RID: 5071 RVA: 0x0005BD89 File Offset: 0x00059F89
		public override TextObject AffirmativeTitleText
		{
			get
			{
				return GameTexts.FindText("str_third_banner_piece_found_assembled", null);
			}
		}

		// Token: 0x17000558 RID: 1368
		// (get) Token: 0x060013D0 RID: 5072 RVA: 0x0005BD96 File Offset: 0x00059F96
		public override TextObject AffirmativeText
		{
			get
			{
				return new TextObject("{=6mgapvxb}Assemble", null);
			}
		}

		// Token: 0x17000559 RID: 1369
		// (get) Token: 0x060013D1 RID: 5073 RVA: 0x0005BDA3 File Offset: 0x00059FA3
		public override TextObject AffirmativeDescriptionText
		{
			get
			{
				return new TextObject("{=IRLB42FY}Assemble the dragon banner!", null);
			}
		}

		// Token: 0x060013D2 RID: 5074 RVA: 0x0005BDB0 File Offset: 0x00059FB0
		public override Banner[] GetBanners()
		{
			return new Banner[] { Hero.MainHero.ClanBanner };
		}

		// Token: 0x060013D3 RID: 5075 RVA: 0x0005BDC5 File Offset: 0x00059FC5
		public FindingThirdBannerPieceSceneNotificationItem()
		{
			this.IsAffirmativeOptionShown = 1;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x04000685 RID: 1669
		private readonly CampaignTime _creationCampaignTime;
	}
}
