using NavalDLC.Storyline;
using NavalDLC.Storyline.Quests;
using SandBox.View.Map.Navigation.NavigationElements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace NavalDLC.View.Permissions;

public class NavalPermissionsSystem
{
	private static NavalPermissionsSystem Current;

	private NavalPermissionsSystem()
	{
		RegisterEvents();
	}

	public static void OnInitialize()
	{
		if (Current == null)
		{
			Current = new NavalPermissionsSystem();
		}
	}

	internal static void OnUnload()
	{
		if (Current != null)
		{
			Current.UnregisterEvents();
			Current = null;
		}
	}

	private void OnClanScreenPermission(ClanScreenPermissionEvent obj)
	{
	}

	private void OnSettlementOverlayTalkPermission(SettlementOverlayTalkPermissionEvent obj)
	{
		if (Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && obj.HeroToTalkTo == NavalStorylineData.Gunnar && Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToGunnarAndSisterQuest)))
		{
			obj.IsTalkAvailable(arg1: false, new TextObject("{=bkppYuaB}Take a walk around the port and find Gunnar to talk to him."));
		}
	}

	private void OnSettlementOverlayQuickTalkPermission(SettlementOverylayQuickTalkPermissionEvent obj)
	{
		if (NavalStorylineData.IsNavalStorylineHero(obj.HeroToTalkTo) && (!NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest5) || Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToGunnarAndSisterQuest))))
		{
			if (Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToGunnarAndSisterQuest)))
			{
				obj.IsTalkAvailable(arg1: false, new TextObject("{=bkppYuaB}Take a walk around the port and find Gunnar to talk to him."));
			}
			else
			{
				obj.IsTalkAvailable(arg1: false, new TextObject("{=UjERCi2F}This feature is disabled."));
			}
		}
	}

	private void OnSettlementOverlayLeaveMemberPermission(SettlementOverlayLeaveCharacterPermissionEvent obj)
	{
	}

	private void OnLeaveKingdomPermissionEvent(LeaveKingdomPermissionEvent obj)
	{
	}

	private void RegisterEvents()
	{
		Game.Current.EventManager.RegisterEvent<ClanScreenPermissionEvent>(OnClanScreenPermission);
		Game.Current.EventManager.RegisterEvent<SettlementOverlayTalkPermissionEvent>(OnSettlementOverlayTalkPermission);
		Game.Current.EventManager.RegisterEvent<SettlementOverylayQuickTalkPermissionEvent>(OnSettlementOverlayQuickTalkPermission);
		Game.Current.EventManager.RegisterEvent<SettlementOverlayLeaveCharacterPermissionEvent>(OnSettlementOverlayLeaveMemberPermission);
		Game.Current.EventManager.RegisterEvent<LeaveKingdomPermissionEvent>(OnLeaveKingdomPermissionEvent);
	}

	internal void UnregisterEvents()
	{
		Game.Current.EventManager.UnregisterEvent<ClanScreenPermissionEvent>(OnClanScreenPermission);
		Game.Current.EventManager.UnregisterEvent<SettlementOverlayTalkPermissionEvent>(OnSettlementOverlayTalkPermission);
		Game.Current.EventManager.UnregisterEvent<SettlementOverylayQuickTalkPermissionEvent>(OnSettlementOverlayQuickTalkPermission);
		Game.Current.EventManager.UnregisterEvent<SettlementOverlayLeaveCharacterPermissionEvent>(OnSettlementOverlayLeaveMemberPermission);
		Game.Current.EventManager.UnregisterEvent<LeaveKingdomPermissionEvent>(OnLeaveKingdomPermissionEvent);
	}
}
