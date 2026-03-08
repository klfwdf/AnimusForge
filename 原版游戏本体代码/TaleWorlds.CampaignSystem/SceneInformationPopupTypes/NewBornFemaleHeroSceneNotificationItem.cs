using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C8 RID: 200
	public class NewBornFemaleHeroSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000592 RID: 1426
		// (get) Token: 0x06001430 RID: 5168 RVA: 0x0005DAD2 File Offset: 0x0005BCD2
		public Hero MaleHero { get; }

		// Token: 0x17000593 RID: 1427
		// (get) Token: 0x06001431 RID: 5169 RVA: 0x0005DADA File Offset: 0x0005BCDA
		public Hero FemaleHero { get; }

		// Token: 0x17000594 RID: 1428
		// (get) Token: 0x06001432 RID: 5170 RVA: 0x0005DAE2 File Offset: 0x0005BCE2
		public override string SceneID
		{
			get
			{
				return "scn_born_baby_female_hero";
			}
		}

		// Token: 0x17000595 RID: 1429
		// (get) Token: 0x06001433 RID: 5171 RVA: 0x0005DAEC File Offset: 0x0005BCEC
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("MOTHER_NAME", this.FemaleHero.Name);
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				return GameTexts.FindText("str_baby_born_only_mother", null);
			}
		}

		// Token: 0x06001434 RID: 5172 RVA: 0x0005DB48 File Offset: 0x0005BD48
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			CharacterObject characterObject = CharacterObject.All.First((CharacterObject h) => h.StringId == "cutscene_midwife");
			Equipment overridenEquipment = this.MaleHero.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, true, false);
			Equipment overridenEquipment2 = this.FemaleHero.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment2, true, true);
			Equipment overriddenEquipment = characterObject.FirstCivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overriddenEquipment, false, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.MaleHero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.FemaleHero, overridenEquipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			list.Add(new SceneNotificationData.SceneNotificationCharacter(characterObject, overriddenEquipment, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));
			return list.ToArray();
		}

		// Token: 0x06001435 RID: 5173 RVA: 0x0005DC30 File Offset: 0x0005BE30
		public NewBornFemaleHeroSceneNotificationItem(Hero maleHero, Hero femaleHero, CampaignTime creationTime)
		{
			this.MaleHero = maleHero;
			this.FemaleHero = femaleHero;
			this._creationCampaignTime = creationTime;
		}

		// Token: 0x040006B9 RID: 1721
		private readonly CampaignTime _creationCampaignTime;
	}
}
