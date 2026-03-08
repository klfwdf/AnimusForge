using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C3 RID: 195
	public class MainHeroBattleDeathNotificationItem : SceneNotificationData
	{
		// Token: 0x1700057C RID: 1404
		// (get) Token: 0x0600140C RID: 5132 RVA: 0x0005CD01 File Offset: 0x0005AF01
		public Hero DeadHero { get; }

		// Token: 0x1700057D RID: 1405
		// (get) Token: 0x0600140D RID: 5133 RVA: 0x0005CD09 File Offset: 0x0005AF09
		public CultureObject KillerCulture { get; }

		// Token: 0x1700057E RID: 1406
		// (get) Token: 0x0600140E RID: 5134 RVA: 0x0005CD11 File Offset: 0x0005AF11
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_main_hero_battle_death";
			}
		}

		// Token: 0x1700057F RID: 1407
		// (get) Token: 0x0600140F RID: 5135 RVA: 0x0005CD18 File Offset: 0x0005AF18
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("NAME", this.DeadHero.Name);
				return GameTexts.FindText("str_main_hero_battle_death", null);
			}
		}

		// Token: 0x06001410 RID: 5136 RVA: 0x0005CD74 File Offset: 0x0005AF74
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment overridenEquipment = this.DeadHero.BattleEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, true, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.DeadHero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			for (int i = 0; i < 23; i++)
			{
				CharacterObject randomTroopForCulture = CampaignSceneNotificationHelper.GetRandomTroopForCulture((this.KillerCulture != null && (float)i > 11.5f) ? this.KillerCulture : this.DeadHero.MapFaction.Culture);
				Equipment equipment = randomTroopForCulture.FirstBattleEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, false, false);
				BodyProperties bodyProperties = randomTroopForCulture.GetBodyProperties(equipment, MBRandom.RandomInt(100));
				list.Add(new SceneNotificationData.SceneNotificationCharacter(randomTroopForCulture, equipment, bodyProperties, false, uint.MaxValue, uint.MaxValue, false));
			}
			return list.ToArray();
		}

		// Token: 0x06001411 RID: 5137 RVA: 0x0005CE43 File Offset: 0x0005B043
		public MainHeroBattleDeathNotificationItem(Hero deadHero, CultureObject killerCulture = null)
		{
			this.DeadHero = deadHero;
			this.KillerCulture = killerCulture;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x040006A3 RID: 1699
		private const int NumberOfCorpses = 23;

		// Token: 0x040006A6 RID: 1702
		private readonly CampaignTime _creationCampaignTime;
	}
}
