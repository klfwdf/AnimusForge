using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B7 RID: 183
	public abstract class EmpireConspiracySupportsSceneNotificationItemBase : SceneNotificationData
	{
		// Token: 0x17000549 RID: 1353
		// (get) Token: 0x060013B8 RID: 5048 RVA: 0x0005B999 File Offset: 0x00059B99
		public Hero King { get; }

		// Token: 0x1700054A RID: 1354
		// (get) Token: 0x060013B9 RID: 5049 RVA: 0x0005B9A1 File Offset: 0x00059BA1
		public override string SceneID
		{
			get
			{
				return "scn_empire_conspiracy_supports_notification";
			}
		}

		// Token: 0x1700054B RID: 1355
		// (get) Token: 0x060013BA RID: 5050 RVA: 0x0005B9A8 File Offset: 0x00059BA8
		public override TextObject AffirmativeText
		{
			get
			{
				return GameTexts.FindText("str_ok", null);
			}
		}

		// Token: 0x060013BB RID: 5051 RVA: 0x0005B9B5 File Offset: 0x00059BB5
		public override Banner[] GetBanners()
		{
			return new Banner[]
			{
				this.King.MapFaction.Banner,
				this.King.MapFaction.Banner
			};
		}

		// Token: 0x060013BC RID: 5052 RVA: 0x0005B9E4 File Offset: 0x00059BE4
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment overridenEquipment = this.King.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, false, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.King, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("villager_battania");
			Equipment equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("conspirator_cutscene_template").DefaultEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, false, false);
			BodyProperties bodyProperties = @object.GetBodyProperties(equipment, MBRandom.RandomInt(100));
			list.Add(new SceneNotificationData.SceneNotificationCharacter(@object, equipment, bodyProperties, false, 0U, 0U, false));
			bodyProperties = @object.GetBodyProperties(equipment, MBRandom.RandomInt(100));
			list.Add(new SceneNotificationData.SceneNotificationCharacter(@object, equipment, bodyProperties, false, 0U, 0U, false));
			bodyProperties = @object.GetBodyProperties(equipment, MBRandom.RandomInt(100));
			list.Add(new SceneNotificationData.SceneNotificationCharacter(@object, equipment, bodyProperties, false, 0U, 0U, false));
			list.Add(CampaignSceneNotificationHelper.GetBodyguardOfCulture(this.King.MapFaction.Culture));
			list.Add(CampaignSceneNotificationHelper.GetBodyguardOfCulture(this.King.MapFaction.Culture));
			return list.ToArray();
		}

		// Token: 0x060013BD RID: 5053 RVA: 0x0005BB03 File Offset: 0x00059D03
		protected EmpireConspiracySupportsSceneNotificationItemBase(Hero kingHero)
		{
			this.King = kingHero;
		}
	}
}
