using System;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000F1 RID: 241
	public class DefaultBanditDensityModel : BanditDensityModel
	{
		// Token: 0x17000613 RID: 1555
		// (get) Token: 0x0600162C RID: 5676 RVA: 0x00065543 File Offset: 0x00063743
		public override int NumberOfMinimumBanditPartiesInAHideoutToInfestIt
		{
			get
			{
				return 2;
			}
		}

		// Token: 0x17000614 RID: 1556
		// (get) Token: 0x0600162D RID: 5677 RVA: 0x00065546 File Offset: 0x00063746
		public override int NumberOfMaximumBanditPartiesInEachHideout
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x17000615 RID: 1557
		// (get) Token: 0x0600162E RID: 5678 RVA: 0x00065549 File Offset: 0x00063749
		public override int NumberOfMaximumBanditPartiesAroundEachHideout
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x17000616 RID: 1558
		// (get) Token: 0x0600162F RID: 5679 RVA: 0x0006554C File Offset: 0x0006374C
		public override int NumberOfMaximumHideoutsAtEachBanditFaction
		{
			get
			{
				return 9;
			}
		}

		// Token: 0x17000617 RID: 1559
		// (get) Token: 0x06001630 RID: 5680 RVA: 0x00065550 File Offset: 0x00063750
		public override int NumberOfInitialHideoutsAtEachBanditFaction
		{
			get
			{
				return 7;
			}
		}

		// Token: 0x17000618 RID: 1560
		// (get) Token: 0x06001631 RID: 5681 RVA: 0x00065553 File Offset: 0x00063753
		public override int NumberOfMinimumBanditTroopsInHideoutMission
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x17000619 RID: 1561
		// (get) Token: 0x06001632 RID: 5682 RVA: 0x00065557 File Offset: 0x00063757
		public override int NumberOfMaximumTroopCountForFirstFightInHideout
		{
			get
			{
				return MathF.Floor(9f * (2f + Campaign.Current.PlayerProgress));
			}
		}

		// Token: 0x1700061A RID: 1562
		// (get) Token: 0x06001633 RID: 5683 RVA: 0x00065574 File Offset: 0x00063774
		public override int NumberOfMaximumTroopCountForBossFightInHideout
		{
			get
			{
				return MathF.Floor(1f + 5f * (1f + Campaign.Current.PlayerProgress));
			}
		}

		// Token: 0x1700061B RID: 1563
		// (get) Token: 0x06001634 RID: 5684 RVA: 0x00065597 File Offset: 0x00063797
		public override float SpawnPercentageForFirstFightInHideoutMission
		{
			get
			{
				return 0.75f;
			}
		}

		// Token: 0x1700061C RID: 1564
		// (get) Token: 0x06001635 RID: 5685 RVA: 0x0006559E File Offset: 0x0006379E
		private Clan DeserterClan
		{
			get
			{
				if (this._deserterClan == null)
				{
					this._deserterClan = Clan.FindFirst((Clan x) => x.StringId == "deserters");
				}
				return this._deserterClan;
			}
		}

		// Token: 0x06001636 RID: 5686 RVA: 0x000655D8 File Offset: 0x000637D8
		public override int GetMinimumTroopCountForHideoutMission(MobileParty party, bool isAssault)
		{
			if (!isAssault)
			{
				return 20;
			}
			return 8;
		}

		// Token: 0x06001637 RID: 5687 RVA: 0x000655E4 File Offset: 0x000637E4
		public override int GetMaxSupportedNumberOfLootersForClan(Clan clan)
		{
			if (clan == this.DeserterClan)
			{
				return 50;
			}
			if (clan.StringId == "looters" && this.DeserterClan != null)
			{
				return 270 - this.DeserterClan.WarPartyComponents.Count;
			}
			return 270;
		}

		// Token: 0x06001638 RID: 5688 RVA: 0x00065634 File Offset: 0x00063834
		public override int GetMaximumTroopCountForHideoutMission(MobileParty party, bool isAssault)
		{
			int num = (isAssault ? 15 : 40);
			if (party.HasPerk(DefaultPerks.Tactics.SmallUnitTactics, false))
			{
				num += (int)DefaultPerks.Tactics.SmallUnitTactics.PrimaryBonus;
			}
			return num;
		}

		// Token: 0x06001639 RID: 5689 RVA: 0x00065668 File Offset: 0x00063868
		public override bool IsPositionInsideNavalSafeZone(CampaignVec2 position)
		{
			return false;
		}

		// Token: 0x0400075C RID: 1884
		private Clan _deserterClan;
	}
}
