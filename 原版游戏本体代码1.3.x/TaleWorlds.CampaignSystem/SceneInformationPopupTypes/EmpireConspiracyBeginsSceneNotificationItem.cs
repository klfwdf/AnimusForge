using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B6 RID: 182
	public class EmpireConspiracyBeginsSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000544 RID: 1348
		// (get) Token: 0x060013AF RID: 5039 RVA: 0x0005B79D File Offset: 0x0005999D
		public Hero PlayerHero { get; }

		// Token: 0x17000545 RID: 1349
		// (get) Token: 0x060013B0 RID: 5040 RVA: 0x0005B7A5 File Offset: 0x000599A5
		public Kingdom Empire { get; }

		// Token: 0x17000546 RID: 1350
		// (get) Token: 0x060013B1 RID: 5041 RVA: 0x0005B7AD File Offset: 0x000599AD
		public bool IsConspiracyAgainstEmpire { get; }

		// Token: 0x17000547 RID: 1351
		// (get) Token: 0x060013B2 RID: 5042 RVA: 0x0005B7B5 File Offset: 0x000599B5
		public override string SceneID
		{
			get
			{
				return "scn_empire_conspiracy_start_notification";
			}
		}

		// Token: 0x17000548 RID: 1352
		// (get) Token: 0x060013B3 RID: 5043 RVA: 0x0005B7BC File Offset: 0x000599BC
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				if (this.IsConspiracyAgainstEmpire)
				{
					return GameTexts.FindText("str_empire_conspiracy_begins_antiempire", null);
				}
				return GameTexts.FindText("str_empire_conspiracy_begins_proempire", null);
			}
		}

		// Token: 0x060013B4 RID: 5044 RVA: 0x0005B815 File Offset: 0x00059A15
		public override Banner[] GetBanners()
		{
			return new Banner[] { this.Empire.Banner };
		}

		// Token: 0x060013B5 RID: 5045 RVA: 0x0005B82C File Offset: 0x00059A2C
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			for (int i = 0; i < 8; i++)
			{
				Equipment equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("conspirator_cutscene_template").DefaultEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, false, false);
				CharacterObject facePropertiesFromAudienceIndex = this.GetFacePropertiesFromAudienceIndex(false, i);
				BodyProperties bodyProperties = facePropertiesFromAudienceIndex.GetBodyProperties(equipment, MBRandom.RandomInt(100));
				uint customColor = this._audienceColors[MBRandom.RandomInt(this._audienceColors.Length)];
				uint customColor2 = this._audienceColors[MBRandom.RandomInt(this._audienceColors.Length)];
				list.Add(new SceneNotificationData.SceneNotificationCharacter(facePropertiesFromAudienceIndex, equipment, bodyProperties, false, customColor, customColor2, false));
			}
			return list.ToArray();
		}

		// Token: 0x060013B6 RID: 5046 RVA: 0x0005B8D5 File Offset: 0x00059AD5
		public EmpireConspiracyBeginsSceneNotificationItem(Hero playerHero, Kingdom empire, bool isConspiracyAgainstEmpire)
		{
			this.PlayerHero = playerHero;
			this.Empire = empire;
			this.IsConspiracyAgainstEmpire = isConspiracyAgainstEmpire;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x060013B7 RID: 5047 RVA: 0x0005B914 File Offset: 0x00059B14
		private CharacterObject GetFacePropertiesFromAudienceIndex(bool playerWantsRestore, int audienceMemberIndex)
		{
			if (!playerWantsRestore)
			{
				return MBObjectManager.Instance.GetObject<CharacterObject>("villager_empire");
			}
			string objectName;
			switch (audienceMemberIndex % 8)
			{
			case 0:
				objectName = "villager_battania";
				break;
			case 1:
				objectName = "villager_khuzait";
				break;
			case 2:
				objectName = "villager_vlandia";
				break;
			case 3:
				objectName = "villager_aserai";
				break;
			case 4:
				objectName = "villager_battania";
				break;
			case 5:
				objectName = "villager_sturgia";
				break;
			default:
				objectName = "villager_battania";
				break;
			}
			return MBObjectManager.Instance.GetObject<CharacterObject>(objectName);
		}

		// Token: 0x04000677 RID: 1655
		private const int AudienceNumber = 8;

		// Token: 0x04000678 RID: 1656
		private readonly uint[] _audienceColors = new uint[] { 4278914065U, 4284308292U, 4281543757U, 4282199842U };

		// Token: 0x0400067C RID: 1660
		private readonly CampaignTime _creationCampaignTime;
	}
}
