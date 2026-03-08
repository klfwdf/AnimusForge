using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B2 RID: 178
	public class ClanMemberPeaceDeathSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000537 RID: 1335
		// (get) Token: 0x06001394 RID: 5012 RVA: 0x0005B043 File Offset: 0x00059243
		public Hero DeadHero { get; }

		// Token: 0x17000538 RID: 1336
		// (get) Token: 0x06001395 RID: 5013 RVA: 0x0005B04B File Offset: 0x0005924B
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_family_member_death";
			}
		}

		// Token: 0x17000539 RID: 1337
		// (get) Token: 0x06001396 RID: 5014 RVA: 0x0005B052 File Offset: 0x00059252
		// (set) Token: 0x06001397 RID: 5015 RVA: 0x0005B05A File Offset: 0x0005925A
		public KillCharacterAction.KillCharacterActionDetail KillDetail { get; private set; }

		// Token: 0x1700053A RID: 1338
		// (get) Token: 0x06001398 RID: 5016 RVA: 0x0005B064 File Offset: 0x00059264
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("NAME", this.DeadHero.Name);
				if (this.KillDetail == KillCharacterAction.KillCharacterActionDetail.DiedInLabor)
				{
					return GameTexts.FindText("str_main_hero_battle_death_in_labor", null);
				}
				if (this.KillDetail == KillCharacterAction.KillCharacterActionDetail.Executed || this.KillDetail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
				{
					return GameTexts.FindText("str_main_hero_battle_executed", null);
				}
				if (this.KillDetail == KillCharacterAction.KillCharacterActionDetail.Murdered)
				{
					return GameTexts.FindText("str_main_hero_battle_murdered", null);
				}
				return GameTexts.FindText("str_family_member_death", null);
			}
		}

		// Token: 0x06001399 RID: 5017 RVA: 0x0005B106 File Offset: 0x00059306
		public override Banner[] GetBanners()
		{
			return new Banner[] { this.DeadHero.ClanBanner };
		}

		// Token: 0x0600139A RID: 5018 RVA: 0x0005B11C File Offset: 0x0005931C
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			Equipment overridenEquipment = this.DeadHero.CivilianEquipment.Clone(false);
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
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

		// Token: 0x0600139B RID: 5019 RVA: 0x0005B1EC File Offset: 0x000593EC
		public ClanMemberPeaceDeathSceneNotificationItem(Hero deadHero, CampaignTime creationTime, KillCharacterAction.KillCharacterActionDetail killDetail)
		{
			this.DeadHero = deadHero;
			this._creationCampaignTime = creationTime;
			this.KillDetail = killDetail;
		}

		// Token: 0x0400066A RID: 1642
		private const int NumberOfAudienceHeroes = 5;

		// Token: 0x0400066D RID: 1645
		private readonly CampaignTime _creationCampaignTime;
	}
}
