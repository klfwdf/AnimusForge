using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C1 RID: 193
	public class KingdomCreatedSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x17000574 RID: 1396
		// (get) Token: 0x060013FE RID: 5118 RVA: 0x0005CA51 File Offset: 0x0005AC51
		public Kingdom NewKingdom { get; }

		// Token: 0x17000575 RID: 1397
		// (get) Token: 0x060013FF RID: 5119 RVA: 0x0005CA59 File Offset: 0x0005AC59
		public override string SceneID
		{
			get
			{
				return "scn_kingdom_made";
			}
		}

		// Token: 0x17000576 RID: 1398
		// (get) Token: 0x06001400 RID: 5120 RVA: 0x0005CA60 File Offset: 0x0005AC60
		public override bool PauseActiveState
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000577 RID: 1399
		// (get) Token: 0x06001401 RID: 5121 RVA: 0x0005CA64 File Offset: 0x0005AC64
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("KINGDOM_NAME", this.NewKingdom.Name);
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("LEADER_NAME", this.NewKingdom.Leader.Name);
				return GameTexts.FindText("str_kingdom_created", null);
			}
		}

		// Token: 0x17000578 RID: 1400
		// (get) Token: 0x06001402 RID: 5122 RVA: 0x0005CAD8 File Offset: 0x0005ACD8
		public override TextObject AffirmativeText
		{
			get
			{
				return GameTexts.FindText("str_ok", null);
			}
		}

		// Token: 0x06001403 RID: 5123 RVA: 0x0005CAE5 File Offset: 0x0005ACE5
		public override Banner[] GetBanners()
		{
			return new Banner[]
			{
				this.NewKingdom.Banner,
				this.NewKingdom.Banner
			};
		}

		// Token: 0x06001404 RID: 5124 RVA: 0x0005CB0C File Offset: 0x0005AD0C
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Hero leader = this.NewKingdom.Leader;
			Equipment overridenEquipment = leader.BattleEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, true, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(leader, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			foreach (Hero hero in CampaignSceneNotificationHelper.GetMilitaryAudienceForKingdom(this.NewKingdom, false).Take(5))
			{
				Equipment overridenEquipment2 = hero.CivilianEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment2, true, false);
				list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(hero, overridenEquipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			}
			return list.ToArray();
		}

		// Token: 0x06001405 RID: 5125 RVA: 0x0005CBE0 File Offset: 0x0005ADE0
		public KingdomCreatedSceneNotificationItem(Kingdom newKingdom)
		{
			this.NewKingdom = newKingdom;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x0400069D RID: 1693
		private const int NumberOfKingdomMemberAudience = 5;

		// Token: 0x0400069F RID: 1695
		private readonly CampaignTime _creationCampaignTime;
	}
}
