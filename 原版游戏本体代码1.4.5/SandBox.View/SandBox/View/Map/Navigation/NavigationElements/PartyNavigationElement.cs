using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Navigation.NavigationElements;

public class PartyNavigationElement : MapNavigationElementBase
{
	public override string StringId => "party";

	public override bool IsActive => base._game.GameStateManager.ActiveState is PartyState;

	public override bool IsLockingNavigation
	{
		get
		{
			if (GameStateManager.Current?.ActiveState is PartyState { PartyScreenLogic: not null, PartyScreenMode: not PartyScreenHelper.PartyScreenMode.Normal })
			{
				return true;
			}
			return false;
		}
	}

	public override bool HasAlert => _viewDataTracker.IsPartyNotificationActive;

	public PartyNavigationElement(MapNavigationHandler handler)
		: base(handler)
	{
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
		if (MobileParty.MainParty.IsInRaftState || Hero.MainHero.HeroState == Hero.CharacterStates.Prisoner)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		if (MobileParty.MainParty.MapEvent != null)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		Mission current = Mission.Current;
		if (current != null && !current.IsPartyWindowAccessAllowed)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		return new NavigationPermissionItem(isAuthorized: true, null);
	}

	protected override TextObject GetTooltip()
	{
		if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || IsActive))
		{
			string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 43).ToString();
			TextObject textObject = GameTexts.FindText("str_hotkey_with_hint");
			textObject.SetTextVariable("TEXT", GameTexts.FindText("str_party").ToString());
			textObject.SetTextVariable("HOTKEY", variable);
			return textObject;
		}
		return GameTexts.FindText("str_party");
	}

	protected override TextObject GetAlertTooltip()
	{
		if (HasAlert)
		{
			return _viewDataTracker.GetPartyNotificationText();
		}
		return TextObject.GetEmpty();
	}

	public override void OpenView()
	{
		if (base.Permission.IsAuthorized)
		{
			if (ScreenManager.TopScreen is IChangeableScreen changeableScreen && changeableScreen.AnyUnsavedChanges())
			{
				InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(PartyScreenHelper.OpenScreenAsNormal) : MapNavigationHelper.GetUnapplicableChangedInquiry());
			}
			else
			{
				MapNavigationHelper.SwitchToANewScreen(PartyScreenHelper.OpenScreenAsNormal);
			}
		}
	}

	public override void OpenView(params object[] parameters)
	{
		Debug.FailedAssert("Party screen shouldn't be opened with parameters from navigation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\PartyNavigationElement.cs", "OpenView", 118);
		OpenView();
	}

	public override void GoToLink()
	{
	}
}
