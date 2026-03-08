using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C7 RID: 199
	public class NewBornFemaleHeroSceneAlternateNotificationItem : SceneNotificationData
	{
		// Token: 0x1700058E RID: 1422
		// (get) Token: 0x0600142A RID: 5162 RVA: 0x0005D97A File Offset: 0x0005BB7A
		public Hero MaleHero { get; }

		// Token: 0x1700058F RID: 1423
		// (get) Token: 0x0600142B RID: 5163 RVA: 0x0005D982 File Offset: 0x0005BB82
		public Hero FemaleHero { get; }

		// Token: 0x17000590 RID: 1424
		// (get) Token: 0x0600142C RID: 5164 RVA: 0x0005D98A File Offset: 0x0005BB8A
		public override string SceneID
		{
			get
			{
				return "scn_born_baby_female_hero2";
			}
		}

		// Token: 0x17000591 RID: 1425
		// (get) Token: 0x0600142D RID: 5165 RVA: 0x0005D994 File Offset: 0x0005BB94
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("MOTHER_NAME", this.FemaleHero.Name);
				return GameTexts.FindText("str_baby_born_only_mother", null);
			}
		}

		// Token: 0x0600142E RID: 5166 RVA: 0x0005D9F0 File Offset: 0x0005BBF0
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Equipment overridenEquipment = this.FemaleHero.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, true, true);
			CharacterObject characterObject = CharacterObject.All.First((CharacterObject h) => h.StringId == "cutscene_midwife");
			Equipment overriddenEquipment = characterObject.FirstCivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overriddenEquipment, false, false);
			list.Add(new SceneNotificationData.SceneNotificationCharacter(null, null, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(this.FemaleHero, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			list.Add(new SceneNotificationData.SceneNotificationCharacter(characterObject, overriddenEquipment, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false));
			return list.ToArray();
		}

		// Token: 0x0600142F RID: 5167 RVA: 0x0005DAB5 File Offset: 0x0005BCB5
		public NewBornFemaleHeroSceneAlternateNotificationItem(Hero maleHero, Hero femaleHero, CampaignTime creationTime)
		{
			this.MaleHero = maleHero;
			this.FemaleHero = femaleHero;
			this._creationCampaignTime = creationTime;
		}

		// Token: 0x040006B6 RID: 1718
		private readonly CampaignTime _creationCampaignTime;
	}
}
