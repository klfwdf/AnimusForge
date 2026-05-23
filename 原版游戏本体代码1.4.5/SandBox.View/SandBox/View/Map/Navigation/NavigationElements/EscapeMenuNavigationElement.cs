using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Map.Navigation.NavigationElements;

public class EscapeMenuNavigationElement : MapNavigationElementBase
{
	public override string StringId => "escape_menu";

	public override bool IsActive
	{
		get
		{
			if (base._game.GameStateManager.ActiveState is MapState)
			{
				return MapScreen.Instance?.IsEscapeMenuOpened ?? false;
			}
			return false;
		}
	}

	public override bool IsLockingNavigation => false;

	public override bool HasAlert => false;

	public EscapeMenuNavigationElement(MapNavigationHandler handler)
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
		return new NavigationPermissionItem(base._game.GameStateManager.ActiveState is MapState, null);
	}

	protected override TextObject GetTooltip()
	{
		if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || IsActive))
		{
			string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericPanelGameKeyCategory", "ToggleEscapeMenu").ToString();
			TextObject textObject = GameTexts.FindText("str_hotkey_with_hint");
			textObject.SetTextVariable("TEXT", GameTexts.FindText("str_escape_menu").ToString());
			textObject.SetTextVariable("HOTKEY", variable);
			return textObject;
		}
		return GameTexts.FindText("str_escape_menu");
	}

	protected override TextObject GetAlertTooltip()
	{
		return TextObject.GetEmpty();
	}

	public override void OpenView()
	{
		if (base.Permission.IsAuthorized)
		{
			MapScreen.Instance?.OpenEscapeMenu();
		}
	}

	public override void OpenView(params object[] parameters)
	{
		Debug.FailedAssert("Escape menu shouldn't be opened with parameters from navigation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\EscapeMenuNavigationElement.cs", "OpenView", 70);
		OpenView();
	}

	public override void GoToLink()
	{
	}
}
