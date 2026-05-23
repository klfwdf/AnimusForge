using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000CB RID: 203
	public class SupportedFactionDefeatedSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x1700059E RID: 1438
		// (get) Token: 0x06001443 RID: 5187 RVA: 0x0005E0BF File Offset: 0x0005C2BF
		public Kingdom Faction { get; }

		// Token: 0x1700059F RID: 1439
		// (get) Token: 0x06001444 RID: 5188 RVA: 0x0005E0C7 File Offset: 0x0005C2C7
		public bool PlayerWantsRestore { get; }

		// Token: 0x170005A0 RID: 1440
		// (get) Token: 0x06001445 RID: 5189 RVA: 0x0005E0CF File Offset: 0x0005C2CF
		public override string SceneID
		{
			get
			{
				return "scn_supported_faction_defeated_notification";
			}
		}

		// Token: 0x170005A1 RID: 1441
		// (get) Token: 0x06001446 RID: 5190 RVA: 0x0005E0D8 File Offset: 0x0005C2D8
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("FORMAL_NAME", CampaignSceneNotificationHelper.GetFormalNameForKingdom(this.Faction));
				GameTexts.SetVariable("PLAYER_WANTS_RESTORE", this.PlayerWantsRestore ? 1 : 0);
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				return GameTexts.FindText("str_supported_faction_defeated", null);
			}
		}

		// Token: 0x06001447 RID: 5191 RVA: 0x0005E148 File Offset: 0x0005C348
		public override Banner[] GetBanners()
		{
			return new Banner[]
			{
				this.Faction.Banner,
				this.Faction.Banner
			};
		}

		// Token: 0x06001448 RID: 5192 RVA: 0x0005E16C File Offset: 0x0005C36C
		public SupportedFactionDefeatedSceneNotificationItem(Kingdom faction, bool playerWantsRestore)
		{
			this.Faction = faction;
			this.PlayerWantsRestore = playerWantsRestore;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x040006C3 RID: 1731
		private readonly CampaignTime _creationCampaignTime;
	}
}
