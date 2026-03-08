using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C2 RID: 194
	public class KingdomDestroyedSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000579 RID: 1401
		// (get) Token: 0x06001406 RID: 5126 RVA: 0x0005CBFA File Offset: 0x0005ADFA
		public Kingdom DestroyedKingdom { get; }

		// Token: 0x1700057A RID: 1402
		// (get) Token: 0x06001407 RID: 5127 RVA: 0x0005CC02 File Offset: 0x0005AE02
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_enemykingdom_destroyed";
			}
		}

		// Token: 0x1700057B RID: 1403
		// (get) Token: 0x06001408 RID: 5128 RVA: 0x0005CC0C File Offset: 0x0005AE0C
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("FORMAL_NAME", CampaignSceneNotificationHelper.GetFormalNameForKingdom(this.DestroyedKingdom));
				return GameTexts.FindText("str_kingdom_destroyed_scene_notification", null);
			}
		}

		// Token: 0x06001409 RID: 5129 RVA: 0x0005CC66 File Offset: 0x0005AE66
		public override Banner[] GetBanners()
		{
			return new Banner[] { this.DestroyedKingdom.Banner };
		}

		// Token: 0x0600140A RID: 5130 RVA: 0x0005CC7C File Offset: 0x0005AE7C
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			for (int i = 0; i < 2; i++)
			{
				CharacterObject randomTroopForCulture = CampaignSceneNotificationHelper.GetRandomTroopForCulture(this.DestroyedKingdom.Culture);
				Equipment equipment = randomTroopForCulture.FirstBattleEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, false, false);
				BodyProperties bodyProperties = randomTroopForCulture.GetBodyProperties(equipment, MBRandom.RandomInt(100));
				list.Add(new SceneNotificationData.SceneNotificationCharacter(randomTroopForCulture, equipment, bodyProperties, false, uint.MaxValue, uint.MaxValue, false));
			}
			return list.ToArray();
		}

		// Token: 0x0600140B RID: 5131 RVA: 0x0005CCEB File Offset: 0x0005AEEB
		public KingdomDestroyedSceneNotificationItem(Kingdom destroyedKingdom, CampaignTime creationTime)
		{
			this.DestroyedKingdom = destroyedKingdom;
			this._creationCampaignTime = creationTime;
		}

		// Token: 0x040006A0 RID: 1696
		private const int NumberOfDeadTroops = 2;

		// Token: 0x040006A2 RID: 1698
		private readonly CampaignTime _creationCampaignTime;
	}
}
