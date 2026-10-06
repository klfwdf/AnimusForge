using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace TaleWorlds.CampaignSystem.Actions;

public class RaftStateChangeAction
{
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
					foreach (TroopRosterElement item in mobileParty.PrisonRoster.GetTroopRoster())
					{
						if (item.Character.IsHero)
						{
							EndCaptivityAction.ApplyByEscape(item.Character.HeroObject);
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

	public static void ActivateRaftStateForParty(MobileParty mobileParty)
	{
		ApplyInternal(mobileParty, isRaftState: true);
	}

	public static void DeactivateRaftStateForParty(MobileParty mobileParty)
	{
		ApplyInternal(mobileParty, isRaftState: false);
	}
}
