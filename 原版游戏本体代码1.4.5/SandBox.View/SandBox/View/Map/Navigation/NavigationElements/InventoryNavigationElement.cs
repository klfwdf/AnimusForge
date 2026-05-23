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

public class InventoryNavigationElement : MapNavigationElementBase
{
	public override string StringId => "inventory";

	public override bool IsActive => base._game.GameStateManager.ActiveState is InventoryState;

	public override bool IsLockingNavigation
	{
		get
		{
			if (GameStateManager.Current?.ActiveState is InventoryState { InventoryLogic: not null, InventoryMode: not InventoryScreenHelper.InventoryMode.Default })
			{
				return true;
			}
			return false;
		}
	}

	public override bool HasAlert => false;

	public InventoryNavigationElement(MapNavigationHandler handler)
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
		Mission current = Mission.Current;
		if (current != null && !current.IsInventoryAccessAllowed)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		return new NavigationPermissionItem(isAuthorized: true, null);
	}

	protected override TextObject GetTooltip()
	{
		if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || IsActive))
		{
			string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 38).ToString();
			TextObject textObject = GameTexts.FindText("str_hotkey_with_hint");
			textObject.SetTextVariable("TEXT", GameTexts.FindText("str_inventory").ToString());
			textObject.SetTextVariable("HOTKEY", variable);
			return textObject;
		}
		return GameTexts.FindText("str_inventory");
	}

	protected override TextObject GetAlertTooltip()
	{
		return TextObject.GetEmpty();
	}

	public override void OpenView()
	{
		if (!base.Permission.IsAuthorized)
		{
			return;
		}
		if (ScreenManager.TopScreen is IChangeableScreen changeableScreen && changeableScreen.AnyUnsavedChanges())
		{
			InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(delegate
			{
				InventoryScreenHelper.OpenScreenAsInventory();
			}) : MapNavigationHelper.GetUnapplicableChangedInquiry());
		}
		else
		{
			MapNavigationHelper.SwitchToANewScreen(delegate
			{
				InventoryScreenHelper.OpenScreenAsInventory();
			});
		}
	}

	public override void OpenView(params object[] parameters)
	{
		Debug.FailedAssert("Inventory screen shouldn't be opened with parameters from navigation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\InventoryNavigationElement.cs", "OpenView", 106);
		OpenView();
	}

	public override void GoToLink()
	{
	}
}
