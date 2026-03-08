using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C9 RID: 201
	public class NewBornSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000596 RID: 1430
		// (get) Token: 0x06001436 RID: 5174 RVA: 0x0005DC4D File Offset: 0x0005BE4D
		public Hero MaleHero { get; }

		// Token: 0x17000597 RID: 1431
		// (get) Token: 0x06001437 RID: 5175 RVA: 0x0005DC55 File Offset: 0x0005BE55
		public Hero FemaleHero { get; }

		// Token: 0x17000598 RID: 1432
		// (get) Token: 0x06001438 RID: 5176 RVA: 0x0005DC5D File Offset: 0x0005BE5D
		public override string SceneID
		{
			get
			{
				return "scn_born_baby";
			}
		}

		// Token: 0x17000599 RID: 1433
		// (get) Token: 0x06001439 RID: 5177 RVA: 0x0005DC64 File Offset: 0x0005BE64
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("FATHER_NAME", this.MaleHero.Name);
				GameTexts.SetVariable("MOTHER_NAME", this.FemaleHero.Name);
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				return GameTexts.FindText("str_baby_born", null);
			}
		}

		// Token: 0x0600143A RID: 5178 RVA: 0x0005DCD4 File Offset: 0x0005BED4
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

		// Token: 0x0600143B RID: 5179 RVA: 0x0005DDBC File Offset: 0x0005BFBC
		public NewBornSceneNotificationItem(Hero maleHero, Hero femaleHero, CampaignTime creationTime)
		{
			this.MaleHero = maleHero;
			this.FemaleHero = femaleHero;
			this._creationCampaignTime = creationTime;
		}

		// Token: 0x040006BC RID: 1724
		private readonly CampaignTime _creationCampaignTime;
	}
}
