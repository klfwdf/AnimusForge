using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B3 RID: 179
	public class ClanMemberWarDeathSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x1700053B RID: 1339
		// (get) Token: 0x0600139C RID: 5020 RVA: 0x0005B209 File Offset: 0x00059409
		public Hero DeadHero { get; }

		// Token: 0x1700053C RID: 1340
		// (get) Token: 0x0600139D RID: 5021 RVA: 0x0005B211 File Offset: 0x00059411
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_family_member_death_war";
			}
		}

		// Token: 0x1700053D RID: 1341
		// (get) Token: 0x0600139E RID: 5022 RVA: 0x0005B218 File Offset: 0x00059418
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("NAME", this.DeadHero.Name);
				return GameTexts.FindText("str_family_member_death_war", null);
			}
		}

		// Token: 0x0600139F RID: 5023 RVA: 0x0005B272 File Offset: 0x00059472
		public override Banner[] GetBanners()
		{
			return new Banner[] { this.DeadHero.ClanBanner };
		}

		// Token: 0x060013A0 RID: 5024 RVA: 0x0005B288 File Offset: 0x00059488
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment overridenEquipment = this.DeadHero.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, false, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.DeadHero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			foreach (Hero hero in CampaignSceneNotificationHelper.GetMilitaryAudienceForHero(this.DeadHero, true, false).Take(5))
			{
				Equipment overridenEquipment2 = hero.CivilianEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment2, false, false);
				list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(hero, overridenEquipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			}
			return list.ToArray();
		}

		// Token: 0x060013A1 RID: 5025 RVA: 0x0005B358 File Offset: 0x00059558
		public ClanMemberWarDeathSceneNotificationItem(Hero deadHero, CampaignTime creationTime)
		{
			this.DeadHero = deadHero;
			this._creationCampaignTime = creationTime;
		}

		// Token: 0x0400066E RID: 1646
		private const int NumberOfAudienceHeroes = 5;

		// Token: 0x04000670 RID: 1648
		private readonly CampaignTime _creationCampaignTime;
	}
}
