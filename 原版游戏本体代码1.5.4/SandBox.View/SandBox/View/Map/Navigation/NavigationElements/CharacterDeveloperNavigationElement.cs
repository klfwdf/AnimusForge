using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Navigation.NavigationElements;

public class CharacterDeveloperNavigationElement : MapNavigationElementBase
{
	public override string StringId => "character_developer";

	public override bool IsActive => base._game.GameStateManager.ActiveState is CharacterDeveloperState;

	public override bool IsLockingNavigation => false;

	public override bool HasAlert => _viewDataTracker.IsCharacterNotificationActive;

	public CharacterDeveloperNavigationElement(MapNavigationHandler handler)
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
		Mission current = Mission.Current;
		if (current != null && !current.IsCharacterWindowAccessAllowed)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		return new NavigationPermissionItem(isAuthorized: true, null);
	}

	protected override TextObject GetTooltip()
	{
		if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || IsActive))
		{
			string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 37).ToString();
			TextObject textObject = GameTexts.FindText("str_hotkey_with_hint");
			textObject.SetTextVariable("TEXT", GameTexts.FindText("str_character").ToString());
			textObject.SetTextVariable("HOTKEY", variable);
			return textObject;
		}
		return GameTexts.FindText("str_character");
	}

	protected override TextObject GetAlertTooltip()
	{
		if (HasAlert)
		{
			return _viewDataTracker.GetCharacterNotificationText();
		}
		return TextObject.GetEmpty();
	}

	public override void OpenView()
	{
		PrepareToOpenCharacterDeveloper(delegate
		{
			OpenCharacterDeveloperScreenAction();
		});
	}

	public override void OpenView(params object[] parameters)
	{
		if (parameters.Length == 0)
		{
			return;
		}
		object obj = parameters[0];
		Hero hero;
		if ((hero = obj as Hero) != null)
		{
			PrepareToOpenCharacterDeveloper(delegate
			{
				OpenCharacterDeveloperScreenAction(hero);
			});
		}
		else
		{
			Debug.FailedAssert($"Invalid parameter type when opening the character developer screen from navigation: {obj.GetType()}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\CharacterDeveloperNavigationElement.cs", "OpenView", 90);
		}
	}

	public override void GoToLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(Hero.MainHero.EncyclopediaLink);
	}

	private void PrepareToOpenCharacterDeveloper(Action openCharacterDeveloperAction)
	{
		if (base.Permission.IsAuthorized)
		{
			if (ScreenManager.TopScreen is IChangeableScreen changeableScreen && changeableScreen.AnyUnsavedChanges())
			{
				InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(openCharacterDeveloperAction) : MapNavigationHelper.GetUnapplicableChangedInquiry());
			}
			else
			{
				MapNavigationHelper.SwitchToANewScreen(openCharacterDeveloperAction);
			}
		}
	}

	private void OpenCharacterDeveloperScreenAction()
	{
		CharacterDeveloperState gameState = base._game.GameStateManager.CreateState<CharacterDeveloperState>();
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenCharacterDeveloperScreenAction(Hero hero)
	{
		CharacterDeveloperState gameState = base._game.GameStateManager.CreateState<CharacterDeveloperState>(new object[1] { hero });
		base._game.GameStateManager.PushState(gameState);
	}
}
