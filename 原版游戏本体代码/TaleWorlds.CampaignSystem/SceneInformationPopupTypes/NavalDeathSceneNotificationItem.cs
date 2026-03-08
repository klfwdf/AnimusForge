using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C6 RID: 198
	public class NavalDeathSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000589 RID: 1417
		// (get) Token: 0x06001421 RID: 5153 RVA: 0x0005D844 File Offset: 0x0005BA44
		public Hero DeadHero { get; }

		// Token: 0x1700058A RID: 1418
		// (get) Token: 0x06001422 RID: 5154 RVA: 0x0005D84C File Offset: 0x0005BA4C
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_main_hero_naval_battle_death";
			}
		}

		// Token: 0x1700058B RID: 1419
		// (get) Token: 0x06001423 RID: 5155 RVA: 0x0005D853 File Offset: 0x0005BA53
		// (set) Token: 0x06001424 RID: 5156 RVA: 0x0005D85B File Offset: 0x0005BA5B
		public KillCharacterAction.KillCharacterActionDetail KillDetail { get; private set; }

		// Token: 0x1700058C RID: 1420
		// (get) Token: 0x06001425 RID: 5157 RVA: 0x0005D864 File Offset: 0x0005BA64
		public override SceneNotificationData.NotificationSceneProperties SceneProperties
		{
			get
			{
				return new SceneNotificationData.NotificationSceneProperties
				{
					InitializePhysics = true,
					DisableStaticShadows = true,
					OverriddenWaterStrength = null
				};
			}
		}

		// Token: 0x1700058D RID: 1421
		// (get) Token: 0x06001426 RID: 5158 RVA: 0x0005D898 File Offset: 0x0005BA98
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("NAME", this.DeadHero.Name);
				if (this.KillDetail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
				{
					return GameTexts.FindText("str_main_hero_battle_death", null);
				}
				if (this.KillDetail == KillCharacterAction.KillCharacterActionDetail.DiedInLabor)
				{
					return GameTexts.FindText("str_main_hero_battle_death_in_labor", null);
				}
				if (this.KillDetail == KillCharacterAction.KillCharacterActionDetail.Executed || this.KillDetail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
				{
					return GameTexts.FindText("str_main_hero_battle_executed", null);
				}
				if (this.KillDetail == KillCharacterAction.KillCharacterActionDetail.Murdered)
				{
					return GameTexts.FindText("str_main_hero_battle_murdered", null);
				}
				return GameTexts.FindText("str_family_member_death", null);
			}
		}

		// Token: 0x06001427 RID: 5159 RVA: 0x0005D94F File Offset: 0x0005BB4F
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			return Array.Empty<SceneNotificationData.SceneNotificationCharacter>();
		}

		// Token: 0x06001428 RID: 5160 RVA: 0x0005D956 File Offset: 0x0005BB56
		public override SceneNotificationData.SceneNotificationShip[] GetShips()
		{
			return Array.Empty<SceneNotificationData.SceneNotificationShip>();
		}

		// Token: 0x06001429 RID: 5161 RVA: 0x0005D95D File Offset: 0x0005BB5D
		public NavalDeathSceneNotificationItem(Hero deadHero, CampaignTime creationTime, KillCharacterAction.KillCharacterActionDetail killDetail)
		{
			this.DeadHero = deadHero;
			this._creationCampaignTime = creationTime;
			this.KillDetail = killDetail;
		}

		// Token: 0x040006B3 RID: 1715
		private readonly CampaignTime _creationCampaignTime;
	}
}
