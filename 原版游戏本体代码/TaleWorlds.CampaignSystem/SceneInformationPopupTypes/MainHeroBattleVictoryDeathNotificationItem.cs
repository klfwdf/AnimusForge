using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C4 RID: 196
	public class MainHeroBattleVictoryDeathNotificationItem : SceneNotificationData
	{
		// Token: 0x17000580 RID: 1408
		// (get) Token: 0x06001412 RID: 5138 RVA: 0x0005CE64 File Offset: 0x0005B064
		public Hero DeadHero { get; }

		// Token: 0x17000581 RID: 1409
		// (get) Token: 0x06001413 RID: 5139 RVA: 0x0005CE6C File Offset: 0x0005B06C
		public List<CharacterObject> EncounterAllyCharacters { get; }

		// Token: 0x17000582 RID: 1410
		// (get) Token: 0x06001414 RID: 5140 RVA: 0x0005CE74 File Offset: 0x0005B074
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_main_hero_battle_victory_death";
			}
		}

		// Token: 0x17000583 RID: 1411
		// (get) Token: 0x06001415 RID: 5141 RVA: 0x0005CE7C File Offset: 0x0005B07C
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

		// Token: 0x06001416 RID: 5142 RVA: 0x0005CED8 File Offset: 0x0005B0D8
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment overridenEquipment = this.DeadHero.BattleEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, true, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.DeadHero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			for (int i = 0; i < 2; i++)
			{
				CharacterObject randomTroopForCulture = CampaignSceneNotificationHelper.GetRandomTroopForCulture(this.DeadHero.MapFaction.Culture);
				Equipment equipment = randomTroopForCulture.FirstBattleEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref equipment, false, false);
				BodyProperties bodyProperties = randomTroopForCulture.GetBodyProperties(equipment, MBRandom.RandomInt(100));
				list.Add(new SceneNotificationData.SceneNotificationCharacter(randomTroopForCulture, equipment, bodyProperties, false, uint.MaxValue, uint.MaxValue, false));
			}
			List<CharacterObject> encounterAllyCharacters = this.EncounterAllyCharacters;
			foreach (CharacterObject characterObject in ((encounterAllyCharacters != null) ? encounterAllyCharacters.Take(3) : null))
			{
				if (characterObject.IsHero)
				{
					Equipment overridenEquipment2 = characterObject.HeroObject.BattleEquipment.Clone(false);
					CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment2, false, false);
					list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(characterObject.HeroObject, overridenEquipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
				}
				else
				{
					Equipment overriddenEquipment = characterObject.FirstBattleEquipment.Clone(false);
					CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overriddenEquipment, false, false);
					list.Add(new SceneNotificationData.SceneNotificationCharacter(characterObject, overriddenEquipment, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));
				}
			}
			return list.ToArray();
		}

		// Token: 0x06001417 RID: 5143 RVA: 0x0005D05C File Offset: 0x0005B25C
		public MainHeroBattleVictoryDeathNotificationItem(Hero deadHero, List<CharacterObject> encounterAllyCharacters)
		{
			this.DeadHero = deadHero;
			this.EncounterAllyCharacters = encounterAllyCharacters;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x040006A7 RID: 1703
		private const int NumberOfCorpses = 2;

		// Token: 0x040006A8 RID: 1704
		private const int NumberOfCompanions = 3;

		// Token: 0x040006AB RID: 1707
		private readonly CampaignTime _creationCampaignTime;
	}
}
