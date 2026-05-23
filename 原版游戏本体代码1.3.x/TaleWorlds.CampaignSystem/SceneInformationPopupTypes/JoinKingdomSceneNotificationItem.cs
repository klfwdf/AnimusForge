using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000C0 RID: 192
	public class JoinKingdomSceneNotificationItem : SceneNotificationData
	{
		// Token: 0x1700056F RID: 1391
		// (get) Token: 0x060013F6 RID: 5110 RVA: 0x0005C8AC File Offset: 0x0005AAAC
		public Clan NewMemberClan { get; }

		// Token: 0x17000570 RID: 1392
		// (get) Token: 0x060013F7 RID: 5111 RVA: 0x0005C8B4 File Offset: 0x0005AAB4
		public Kingdom KingdomToUse { get; }

		// Token: 0x17000571 RID: 1393
		// (get) Token: 0x060013F8 RID: 5112 RVA: 0x0005C8BC File Offset: 0x0005AABC
		public override string SceneID
		{
			get
			{
				return "scn_cutscene_factionjoin";
			}
		}

		// Token: 0x17000572 RID: 1394
		// (get) Token: 0x060013F9 RID: 5113 RVA: 0x0005C8C3 File Offset: 0x0005AAC3
		public override SceneNotificationData.RelevantContextType RelevantContext
		{
			get
			{
				return SceneNotificationData.RelevantContextType.Any;
			}
		}

		// Token: 0x17000573 RID: 1395
		// (get) Token: 0x060013FA RID: 5114 RVA: 0x0005C8C8 File Offset: 0x0005AAC8
		public override TextObject TitleText
		{
			get
			{
				GameTexts.SetVariable("CLAN_NAME", this.NewMemberClan.Name);
				GameTexts.SetVariable("DAY_OF_YEAR", CampaignSceneNotificationHelper.GetFormalDayAndSeasonText(this._creationCampaignTime));
				GameTexts.SetVariable("YEAR", this._creationCampaignTime.GetYear);
				GameTexts.SetVariable("KINGDOM_FORMALNAME", CampaignSceneNotificationHelper.GetFormalNameForKingdom(this.KingdomToUse));
				return GameTexts.FindText("str_new_faction_member", null);
			}
		}

		// Token: 0x060013FB RID: 5115 RVA: 0x0005C937 File Offset: 0x0005AB37
		public override Banner[] GetBanners()
		{
			return new Banner[]
			{
				this.KingdomToUse.Banner,
				this.KingdomToUse.Banner
			};
		}

		// Token: 0x060013FC RID: 5116 RVA: 0x0005C95C File Offset: 0x0005AB5C
		public override SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			List<SceneNotificationData.SceneNotificationCharacter> list = new List<SceneNotificationData.SceneNotificationCharacter>();
			Hero leader = this.NewMemberClan.Leader;
			Equipment overridenEquipment = leader.CivilianEquipment.Clone(false);
			CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, true, false);
			list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(leader, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			foreach (Hero hero in CampaignSceneNotificationHelper.GetMilitaryAudienceForKingdom(this.KingdomToUse, true).Take(5))
			{
				Equipment overridenEquipment2 = hero.CivilianEquipment.Clone(false);
				CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment2, true, false);
				list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(hero, overridenEquipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
			}
			return list.ToArray();
		}

		// Token: 0x060013FD RID: 5117 RVA: 0x0005CA30 File Offset: 0x0005AC30
		public JoinKingdomSceneNotificationItem(Clan newMember, Kingdom kingdom)
		{
			this.NewMemberClan = newMember;
			this.KingdomToUse = kingdom;
			this._creationCampaignTime = CampaignTime.Now;
		}

		// Token: 0x04000699 RID: 1689
		private const int NumberOfKingdomMembers = 5;

		// Token: 0x0400069C RID: 1692
		private readonly CampaignTime _creationCampaignTime;
	}
}
