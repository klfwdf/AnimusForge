using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B4 RID: 180
	public class DeathOldAgeSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x1700053E RID: 1342
		// (get) Token: 0x060013A2 RID: 5026 RVA: 0x0005B36E File Offset: 0x0005956E
		public Hero DeadHero { get; }

		// Token: 0x1700053F RID: 1343
		// (get) Token: 0x060013A3 RID: 5027 RVA: 0x0005B376 File Offset: 0x00059576
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_death_old_age";
			}
		}

		// Token: 0x17000540 RID: 1344
		// (get) Token: 0x060013A4 RID: 5028 RVA: 0x0005B380 File Offset: 0x00059580
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("NAME", this.DeadHero.Name);
				return GameTexts.FindText("str_died_of_old_age", null);
			}
		}

		// Token: 0x060013A5 RID: 5029 RVA: 0x0005B3DA File Offset: 0x000595DA
		public override Banner[] GetBanners()
		{
			return new Banner[] { this.DeadHero.ClanBanner };
		}

		// Token: 0x060013A6 RID: 5030 RVA: 0x0005B3F0 File Offset: 0x000595F0
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

		// Token: 0x060013A7 RID: 5031 RVA: 0x0005B4C0 File Offset: 0x000596C0
		public DeathOldAgeSceneNotificationItem(Hero deadHero)
		{
			this.DeadHero = deadHero;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x04000671 RID: 1649
		private const int NumberOfAudienceHeroes = 5;

		// Token: 0x04000673 RID: 1651
		private readonly CampaignTime _creationCampaignTime;
	}
}
