using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004BF RID: 1215
	public class RaftStateChangeAction
	{
		// Token: 0x06004A04 RID: 18948 RVA: 0x00174A08 File Offset: 0x00172C08
		private static void ApplyInternal(MobileParty mobileParty, bool isRaftState)
		{
			mobileParty.IsInRaftState = isRaftState;
			if (mobileParty.Army != null)
			{
				mobileParty.Army = null;
			}
			if (isRaftState)
			{
				mobileParty.MovePartyToTheClosestLand();
				mobileParty.Ai.DisableAi();
				if (mobileParty.Party.PrisonRoster.TotalManCount > 0)
				{
					if (mobileParty.Party.PrisonRoster.TotalHeroes > 0)
					{
						foreach (TroopRosterElement troopRosterElement in mobileParty.PrisonRoster.GetTroopRoster())
						{
							if (troopRosterElement.Character.IsHero)
							{
								EndCaptivityAction.ApplyByEscape(troopRosterElement.Character.HeroObject, null, true);
							}
						}
					}
					mobileParty.PrisonRoster.Clear();
				}
			}
			else
			{
				mobileParty.Ai.EnableAi();
				mobileParty.RecalculateShortTermBehavior();
				mobileParty.Ai.DefaultBehaviorNeedsUpdate = true;
				mobileParty.Ai.RethinkAtNextHourlyTick = true;
			}
			CampaignEventDispatcher.Instance.OnMobilePartyRaftStateChanged(mobileParty);
		}

		// Token: 0x06004A05 RID: 18949 RVA: 0x00174B10 File Offset: 0x00172D10
		public static void ActivateRaftStateForParty(MobileParty mobileParty)
		{
			RaftStateChangeAction.ApplyInternal(mobileParty, true);
		}

		// Token: 0x06004A06 RID: 18950 RVA: 0x00174B19 File Offset: 0x00172D19
		public static void DeactivateRaftStateForParty(MobileParty mobileParty)
		{
			RaftStateChangeAction.ApplyInternal(mobileParty, false);
		}
	}
}
