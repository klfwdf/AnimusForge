using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000156 RID: 342
	public class DefaultSiegeLordsHallFightModel : SiegeLordsHallFightModel
	{
		// Token: 0x170006C7 RID: 1735
		// (get) Token: 0x06001A5A RID: 6746 RVA: 0x000850A3 File Offset: 0x000832A3
		public override float AreaLostRatio
		{
			get
			{
				return 3f;
			}
		}

		// Token: 0x170006C8 RID: 1736
		// (get) Token: 0x06001A5B RID: 6747 RVA: 0x000850AA File Offset: 0x000832AA
		public override float AttackerDefenderTroopCountRatio
		{
			get
			{
				return 0.7f;
			}
		}

		// Token: 0x170006C9 RID: 1737
		// (get) Token: 0x06001A5C RID: 6748 RVA: 0x000850B1 File Offset: 0x000832B1
		public override float DefenderMaxArcherRatio
		{
			get
			{
				return 0.7f;
			}
		}

		// Token: 0x170006CA RID: 1738
		// (get) Token: 0x06001A5D RID: 6749 RVA: 0x000850B8 File Offset: 0x000832B8
		public override int MaxDefenderSideTroopCount
		{
			get
			{
				return 27;
			}
		}

		// Token: 0x170006CB RID: 1739
		// (get) Token: 0x06001A5E RID: 6750 RVA: 0x000850BC File Offset: 0x000832BC
		public override int MaxDefenderArcherCount
		{
			get
			{
				return MathF.Round((float)this.MaxDefenderSideTroopCount * this.DefenderMaxArcherRatio);
			}
		}

		// Token: 0x170006CC RID: 1740
		// (get) Token: 0x06001A5F RID: 6751 RVA: 0x000850D1 File Offset: 0x000832D1
		public override int MaxAttackerSideTroopCount
		{
			get
			{
				return 19;
			}
		}

		// Token: 0x170006CD RID: 1741
		// (get) Token: 0x06001A60 RID: 6752 RVA: 0x000850D5 File Offset: 0x000832D5
		public override int DefenderTroopNumberForSuccessfulPullBack
		{
			get
			{
				return 20;
			}
		}

		// Token: 0x06001A61 RID: 6753 RVA: 0x000850DC File Offset: 0x000832DC
		public override FlattenedTroopRoster GetPriorityListForLordsHallFightMission(MapEvent playerMapEvent, BattleSideEnum side, int troopCount)
		{
			List<MapEventParty> list = (from x in playerMapEvent.PartiesOnSide(side)
				where x.Party.IsMobile
				select x).ToList<MapEventParty>();
			FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster(list.Sum((MapEventParty x) => x.Party.MemberRoster.TotalHealthyCount));
			foreach (MapEventParty mapEventParty in list)
			{
				flattenedTroopRoster.Add(mapEventParty.Party.MemberRoster.GetTroopRoster());
			}
			List<FlattenedTroopRosterElement> list2 = (from x in flattenedTroopRoster
				where !x.Troop.IsHero && x.Troop.IsRanged && !x.IsWounded
				select x).ToList<FlattenedTroopRosterElement>();
			list2.Shuffle<FlattenedTroopRosterElement>();
			List<FlattenedTroopRosterElement> list3 = (from x in flattenedTroopRoster
				where !x.Troop.IsHero && !x.Troop.IsRanged && !x.IsWounded
				select x).ToList<FlattenedTroopRosterElement>();
			list3.Shuffle<FlattenedTroopRosterElement>();
			flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => !x.Troop.IsHero || x.IsWounded);
			int num = troopCount - flattenedTroopRoster.Count<FlattenedTroopRosterElement>();
			if (num > 0)
			{
				int count = list2.Count;
				int count2 = list3.Count;
				int num2 = MathF.Min(count, Campaign.Current.Models.SiegeLordsHallFightModel.MaxDefenderArcherCount);
				int num3 = 0;
				int num4 = 0;
				while (num > 0 && (num3 < num2 || num4 < count2))
				{
					if (num3 < num2)
					{
						FlattenedTroopRosterElement flattenedTroopRosterElement = list2[num3];
						flattenedTroopRoster.Add(flattenedTroopRosterElement.Troop, false, flattenedTroopRosterElement.Xp);
						num--;
					}
					if (num4 < count2 && num > 0)
					{
						FlattenedTroopRosterElement flattenedTroopRosterElement2 = list3[num4];
						flattenedTroopRoster.Add(flattenedTroopRosterElement2.Troop, false, flattenedTroopRosterElement2.Xp);
						num--;
					}
					num3++;
					num4++;
				}
			}
			return flattenedTroopRoster;
		}
	}
}
