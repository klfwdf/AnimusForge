using System;
using Helpers;
using SandBox.View;
using SandBox.View.Map.Navigation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace NavalDLC.View.Map.Navigation;

public class ManageFleetNavigationElement : MapNavigationElementBase
{
	public override string StringId => "manage_fleet";

	public override bool IsActive => base._game.GameStateManager.ActiveState is PortState;

	public override bool IsLockingNavigation
	{
		get
		{
			if (base._game.GameStateManager.ActiveState is PortState portState)
			{
				return portState.PortScreenMode != PortScreenModes.Manage;
			}
			return false;
		}
	}

	public override bool HasAlert => false;

	public ManageFleetNavigationElement(NavalMapNavigationHandler handler)
		: base(handler)
	{
	}

	protected override TextObject GetAlertTooltip()
	{
		return TextObject.GetEmpty();
	}

	protected override TextObject GetTooltip()
	{
		if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || IsActive))
		{
			string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 45).ToString();
			TextObject textObject = GameTexts.FindText("str_hotkey_with_hint");
			textObject.SetTextVariable("TEXT", GameTexts.FindText("str_fleet").ToString());
			textObject.SetTextVariable("HOTKEY", variable);
			return textObject;
		}
		return GameTexts.FindText("str_fleet");
	}

	protected override NavigationPermissionItem GetPermission()
	{
		if (!MapNavigationHelper.IsNavigationBarEnabled(_handler))
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		if (IsActive)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		if (PartyBase.MainParty.Ships.Count == 0)
		{
			return new NavigationPermissionItem(isAuthorized: false, new TextObject("{=lb2hbQyx}You don't have any ships"));
		}
		if (Mission.Current != null)
		{
			return new NavigationPermissionItem(isAuthorized: false, GameTexts.FindText("str_cannot_open_fleet"));
		}
		if (MobileParty.MainParty.MapEvent != null)
		{
			return new NavigationPermissionItem(isAuthorized: false, GameTexts.FindText("str_cannot_open_fleet"));
		}
		if (MobileParty.MainParty.IsInRaftState)
		{
			return new NavigationPermissionItem(isAuthorized: false, new TextObject("{=Lo0E5dKh}You cannot manage your fleet while you are drifting to shore"));
		}
		if (MobileParty.MainParty.IsInFerryState)
		{
			return new NavigationPermissionItem(isAuthorized: false, new TextObject("{=WNwouX8y}You cannot manage your fleet while you are on a ferry"));
		}
		if (Hero.MainHero.IsPrisoner)
		{
			return new NavigationPermissionItem(isAuthorized: false, new TextObject("{=a8UQow7P}You cannot manage your fleet while you are imprisoned"));
		}
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement != null && currentSettlement.HasPort)
		{
			return new NavigationPermissionItem(isAuthorized: false, new TextObject("{=Ug3Tmhr5}You can access your fleet from the port"));
		}
		MobileParty mainParty = MobileParty.MainParty;
		if (mainParty != null && !mainParty.IsCurrentlyAtSea)
		{
			return new NavigationPermissionItem(isAuthorized: false, new TextObject("{=lVes97xY}You cannot access your fleet when you are on land"));
		}
		return new NavigationPermissionItem(isAuthorized: true, null);
	}

	public override void OpenView()
	{
		PrepareToOpenManageFleet(delegate
		{
			OpenManageFleetAction();
		});
	}

	public override void OpenView(params object[] parameters)
	{
		Debug.FailedAssert("Manage Fleet screen shouldn't be opened with parameters from navigation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\Map\\Navigation\\ManageFleetNavigationElement.cs", "OpenView", 111);
		OpenView();
	}

	public override void GoToLink()
	{
	}

	private void OpenManageFleetAction()
	{
		PortStateHelper.OpenAsManageFleet(new MBReadOnlyList<Ship>());
	}

	private void PrepareToOpenManageFleet(Action openManageFleetAction)
	{
		if (base.Permission.IsAuthorized)
		{
			if (ScreenManager.TopScreen is IChangeableScreen changeableScreen && changeableScreen.AnyUnsavedChanges())
			{
				InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(openManageFleetAction) : MapNavigationHelper.GetUnapplicableChangedInquiry());
			}
			else
			{
				MapNavigationHelper.SwitchToANewScreen(openManageFleetAction);
			}
		}
	}
}
