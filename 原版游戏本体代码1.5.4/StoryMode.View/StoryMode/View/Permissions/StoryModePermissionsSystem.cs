using SandBox.View.Map.Navigation.NavigationElements;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace StoryMode.View.Permissions;

public class StoryModePermissionsSystem
{
	private static StoryModePermissionsSystem Current;

	private StoryModePermissionsSystem()
	{
		RegisterEvents();
	}

	public static void OnInitialize()
	{
		if (Current == null)
		{
			Current = new StoryModePermissionsSystem();
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

	private void OnPartyScreenCharacterTalkPermission(PartyScreenCharacterTalkPermissionEvent obj)
	{
		bool num = StoryModeManager.Current != null;
		StoryModeManager current = StoryModeManager.Current;
		bool flag = current != null && current.MainStoryLine?.TutorialPhase.IsCompleted == true;
		if (num && !flag)
		{
			obj.IsTalkAvailable(arg1: false, new TextObject("{=epQYhd1A}Cannot talk to hero right now"));
		}
	}

	private void OnClanScreenPermission(ClanScreenPermissionEvent obj)
	{
		StoryModeManager current = StoryModeManager.Current;
		if (current != null && current.MainStoryLine.IsPlayerInteractionRestricted)
		{
			obj.IsClanScreenAvailable(arg1: false, new TextObject("{=75nwCTEn}Clan Screen is disabled during Tutorial."));
		}
	}

	private void OnSettlementOverlayTalkPermission(SettlementOverlayTalkPermissionEvent obj)
	{
		bool num = StoryModeManager.Current != null;
		TutorialPhase instance = TutorialPhase.Instance;
		bool flag = instance != null && instance.TutorialQuestPhase >= TutorialQuestPhase.RecruitAndPurchaseStarted;
		StoryModeManager current = StoryModeManager.Current;
		bool flag2 = current != null && current.MainStoryLine?.TutorialPhase.IsCompleted == true;
		if (num && !flag && !flag2)
		{
			obj.IsTalkAvailable(arg1: false, new TextObject("{=UjERCi2F}This feature is disabled."));
		}
	}

	private void OnSettlementOverlayQuickTalkPermission(SettlementOverylayQuickTalkPermissionEvent obj)
	{
		bool num = StoryModeManager.Current != null;
		TutorialPhase instance = TutorialPhase.Instance;
		bool flag = instance != null && instance.TutorialQuestPhase >= TutorialQuestPhase.Finalized;
		StoryModeManager current = StoryModeManager.Current;
		bool flag2 = current != null && current.MainStoryLine?.TutorialPhase.IsCompleted == true;
		if (num && !flag && !flag2)
		{
			obj.IsTalkAvailable(arg1: false, new TextObject("{=UjERCi2F}This feature is disabled."));
		}
	}

	private void OnSettlementOverlayLeaveMemberPermission(SettlementOverlayLeaveCharacterPermissionEvent obj)
	{
		bool num = StoryModeManager.Current != null;
		TutorialPhase instance = TutorialPhase.Instance;
		bool flag = instance != null && instance.TutorialQuestPhase >= TutorialQuestPhase.RecruitAndPurchaseStarted;
		StoryModeManager current = StoryModeManager.Current;
		bool flag2 = current != null && current.MainStoryLine?.TutorialPhase.IsCompleted == true;
		if (num && !flag && !flag2)
		{
			obj.IsLeaveAvailable(arg1: false, new TextObject("{=UjERCi2F}This feature is disabled."));
		}
	}

	private void OnLeaveKingdomPermissionEvent(LeaveKingdomPermissionEvent obj)
	{
		if (StoryModeManager.Current?.MainStoryLine.PlayerSupportedKingdom != null && Clan.PlayerClan.Kingdom == StoryModeManager.Current.MainStoryLine.PlayerSupportedKingdom)
		{
			obj.IsLeaveKingdomPossbile?.Invoke(arg1: true, new TextObject("{=WFNLizqL}You've supported a kingdom through main story line. Leaving this kingdom will fail your quest.{newline}{newline}Are you sure?"));
		}
	}

	private void RegisterEvents()
	{
		Game.Current.EventManager.RegisterEvent<ClanScreenPermissionEvent>(OnClanScreenPermission);
		Game.Current.EventManager.RegisterEvent<PartyScreenCharacterTalkPermissionEvent>(OnPartyScreenCharacterTalkPermission);
		Game.Current.EventManager.RegisterEvent<SettlementOverlayTalkPermissionEvent>(OnSettlementOverlayTalkPermission);
		Game.Current.EventManager.RegisterEvent<SettlementOverylayQuickTalkPermissionEvent>(OnSettlementOverlayQuickTalkPermission);
		Game.Current.EventManager.RegisterEvent<SettlementOverlayLeaveCharacterPermissionEvent>(OnSettlementOverlayLeaveMemberPermission);
		Game.Current.EventManager.RegisterEvent<LeaveKingdomPermissionEvent>(OnLeaveKingdomPermissionEvent);
	}

	internal void UnregisterEvents()
	{
		Game.Current.EventManager.UnregisterEvent<ClanScreenPermissionEvent>(OnClanScreenPermission);
		Game.Current.EventManager.RegisterEvent<PartyScreenCharacterTalkPermissionEvent>(OnPartyScreenCharacterTalkPermission);
		Game.Current.EventManager.UnregisterEvent<SettlementOverlayTalkPermissionEvent>(OnSettlementOverlayTalkPermission);
		Game.Current.EventManager.UnregisterEvent<SettlementOverylayQuickTalkPermissionEvent>(OnSettlementOverlayQuickTalkPermission);
		Game.Current.EventManager.UnregisterEvent<SettlementOverlayLeaveCharacterPermissionEvent>(OnSettlementOverlayLeaveMemberPermission);
		Game.Current.EventManager.UnregisterEvent<LeaveKingdomPermissionEvent>(OnLeaveKingdomPermissionEvent);
	}
}
