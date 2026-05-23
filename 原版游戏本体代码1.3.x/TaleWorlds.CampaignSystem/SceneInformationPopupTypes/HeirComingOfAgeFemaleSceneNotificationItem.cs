using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000BD RID: 189
	public class HeirComingOfAgeFemaleSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x1700055A RID: 1370
		// (get) Token: 0x060013D4 RID: 5076 RVA: 0x0005BDDF File Offset: 0x00059FDF
		public Hero MentorHero { get; }

		// Token: 0x1700055B RID: 1371
		// (get) Token: 0x060013D5 RID: 5077 RVA: 0x0005BDE7 File Offset: 0x00059FE7
		public Hero HeroCameOfAge { get; }

		// Token: 0x1700055C RID: 1372
		// (get) Token: 0x060013D6 RID: 5078 RVA: 0x0005BDEF File Offset: 0x00059FEF
		public override string SceneID
		{
			get
			{
				return "scn_hero_come_of_age_female";
			}
		}

		// Token: 0x1700055D RID: 1373
		// (get) Token: 0x060013D7 RID: 5079 RVA: 0x0005BDF8 File Offset: 0x00059FF8
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

		// Token: 0x060013D8 RID: 5080 RVA: 0x0005BE54 File Offset: 0x0005A054
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment overridenEquipment = this.MentorHero.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, false, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.MentorHero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			string childStageEquipmentIDFromCulture = CampaignSceneNotificationHelper.GetChildStageEquipmentIDFromCulture(this.HeroCameOfAge.Culture);
			Equipment overridenEquipment2 = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(childStageEquipmentIDFromCulture).DefaultEquipment.Clone(false);
			BodyProperties overriddenBodyProperties = new BodyProperties(new DynamicBodyProperties(6f, this.HeroCameOfAge.Weight, this.HeroCameOfAge.Build), this.HeroCameOfAge.StaticBodyProperties);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.HeroCameOfAge, overridenEquipment2, false, overriddenBodyProperties, uint.MaxValue, uint.MaxValue, false));
			BodyProperties overriddenBodyProperties2 = new BodyProperties(new DynamicBodyProperties(14f, this.HeroCameOfAge.Weight, this.HeroCameOfAge.Build), this.HeroCameOfAge.StaticBodyProperties);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.HeroCameOfAge, overridenEquipment2, false, overriddenBodyProperties2, uint.MaxValue, uint.MaxValue, false));
			Equipment overridenEquipment3 = this.HeroCameOfAge.BattleEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment3, false, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.HeroCameOfAge, overridenEquipment3, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			return list.ToArray();
		}

		// Token: 0x060013D9 RID: 5081 RVA: 0x0005BF9E File Offset: 0x0005A19E
		public HeirComingOfAgeFemaleSceneNotificationItem(Hero mentorHero, Hero heroCameOfAge, CampaignTime creationTime)
		{
			this.MentorHero = mentorHero;
			this.HeroCameOfAge = heroCameOfAge;
			this._creationCampaignTime = creationTime;
		}

		// Token: 0x04000688 RID: 1672
		private readonly CampaignTime _creationCampaignTime;
	}
}
