using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000BE RID: 190
	public class HeirComingOfAgeSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x1700055E RID: 1374
		// (get) Token: 0x060013DA RID: 5082 RVA: 0x0005BFBB File Offset: 0x0005A1BB
		public Hero MentorHero { get; }

		// Token: 0x1700055F RID: 1375
		// (get) Token: 0x060013DB RID: 5083 RVA: 0x0005BFC3 File Offset: 0x0005A1C3
		public Hero HeroCameOfAge { get; }

		// Token: 0x17000560 RID: 1376
		// (get) Token: 0x060013DC RID: 5084 RVA: 0x0005BFCB File Offset: 0x0005A1CB
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_heir_coming_of_age";
			}
		}

		// Token: 0x17000561 RID: 1377
		// (get) Token: 0x060013DD RID: 5085 RVA: 0x0005BFD4 File Offset: 0x0005A1D4
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("HERO_NAME", this.HeroCameOfAge.Name);
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				return GameTexts.FindText("str_hero_came_of_age", null);
			}
		}

		// Token: 0x060013DE RID: 5086 RVA: 0x0005C030 File Offset: 0x0005A230
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment overridenEquipment = this.MentorHero.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, true, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.MentorHero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			string childStageEquipmentIDFromCulture = CampaignSceneNotificationHelper.GetChildStageEquipmentIDFromCulture(this.HeroCameOfAge.Culture);
			Equipment overridenEquipment2 = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(childStageEquipmentIDFromCulture).DefaultEquipment.Clone(false);
			BodyProperties overriddenBodyProperties = new BodyProperties(new DynamicBodyProperties(6f, this.HeroCameOfAge.Weight, this.HeroCameOfAge.Build), this.HeroCameOfAge.StaticBodyProperties);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.HeroCameOfAge, overridenEquipment2, false, overriddenBodyProperties, uint.MaxValue, uint.MaxValue, false));
			BodyProperties overriddenBodyProperties2 = new BodyProperties(new DynamicBodyProperties(14f, this.HeroCameOfAge.Weight, this.HeroCameOfAge.Build), this.HeroCameOfAge.StaticBodyProperties);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.HeroCameOfAge, overridenEquipment2, false, overriddenBodyProperties2, uint.MaxValue, uint.MaxValue, false));
			Equipment overridenEquipment3 = this.HeroCameOfAge.BattleEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment3, true, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.HeroCameOfAge, overridenEquipment3, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			return list.ToArray();
		}

		// Token: 0x060013DF RID: 5087 RVA: 0x0005C17A File Offset: 0x0005A37A
		public HeirComingOfAgeSceneNotificationItem(Hero mentorHero, Hero heroCameOfAge, CampaignTime creationTime)
		{
			this.MentorHero = mentorHero;
			this.HeroCameOfAge = heroCameOfAge;
			this._creationCampaignTime = creationTime;
		}

		// Token: 0x0400068B RID: 1675
		private readonly CampaignTime _creationCampaignTime;
	}
}
