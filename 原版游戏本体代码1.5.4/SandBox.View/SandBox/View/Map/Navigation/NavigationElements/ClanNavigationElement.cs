using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Navigation.NavigationElements;

public class ClanNavigationElement : MapNavigationElementBase
{
	private readonly ClanScreenPermissionEvent _clanScreenPermissionEvent;

	private NavigationPermissionItem? _mostRecentClanScreenPermission;

	public override string StringId => "clan";

	public override bool IsActive => base._game.GameStateManager.ActiveState is ClanState;

	public override bool IsLockingNavigation => false;

	public override bool HasAlert => false;

	public ClanNavigationElement(MapNavigationHandler handler)
		: base(handler)
	{
		_clanScreenPermissionEvent = new ClanScreenPermissionEvent(OnClanScreenPermission);
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
		if (current != null && !current.IsClanWindowAccessAllowed)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		_mostRecentClanScreenPermission = null;
		Game.Current.EventManager.TriggerEvent(_clanScreenPermissionEvent);
		return _mostRecentClanScreenPermission ?? new NavigationPermissionItem(isAuthorized: true, null);
	}

	protected override TextObject GetTooltip()
	{
		if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || IsActive))
		{
			string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 41).ToString();
			TextObject textObject = GameTexts.FindText("str_hotkey_with_hint");
			textObject.SetTextVariable("TEXT", GameTexts.FindText("str_clan").ToString());
			textObject.SetTextVariable("HOTKEY", variable);
			return textObject;
		}
		return GameTexts.FindText("str_clan");
	}

	protected override TextObject GetAlertTooltip()
	{
		return TextObject.GetEmpty();
	}

	public override void OpenView()
	{
		PrepareToOpenClanScreen(delegate
		{
			OpenClanScreenAction();
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
		PartyBase party;
		Settlement settlement;
		Workshop workshop;
		Alley alley;
		if ((hero = obj as Hero) != null)
		{
			PrepareToOpenClanScreen(delegate
			{
				OpenClanScreenAction(hero);
			});
		}
		else if ((party = obj as PartyBase) != null)
		{
			PrepareToOpenClanScreen(delegate
			{
				OpenClanScreenAction(party);
			});
		}
		else if ((settlement = obj as Settlement) != null)
		{
			PrepareToOpenClanScreen(delegate
			{
				OpenClanScreenAction(settlement);
			});
		}
		else if ((workshop = obj as Workshop) != null)
		{
			PrepareToOpenClanScreen(delegate
			{
				OpenClanScreenAction(workshop);
			});
		}
		else if ((alley = obj as Alley) != null)
		{
			PrepareToOpenClanScreen(delegate
			{
				OpenClanScreenAction(alley);
			});
		}
		else
		{
			Debug.FailedAssert($"Invalid parameter type when opening the clan screen from navigation: {obj.GetType()}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\ClanNavigationElement.cs", "OpenView", 110);
		}
	}

	public override void GoToLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(Hero.MainHero.Clan.EncyclopediaLink);
	}

	public void OnClanScreenPermission(bool isAvailable, TextObject reasonString)
	{
		if (!isAvailable)
		{
			_mostRecentClanScreenPermission = new NavigationPermissionItem(isAvailable, reasonString);
		}
	}

	private void PrepareToOpenClanScreen(Action openClanScreenAction)
	{
		if (base.Permission.IsAuthorized)
		{
			if (ScreenManager.TopScreen is IChangeableScreen changeableScreen && changeableScreen.AnyUnsavedChanges())
			{
				InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(openClanScreenAction) : MapNavigationHelper.GetUnapplicableChangedInquiry());
			}
			else
			{
				MapNavigationHelper.SwitchToANewScreen(openClanScreenAction);
			}
		}
	}

	private void OpenClanScreenAction()
	{
		ClanState gameState = base._game.GameStateManager.CreateState<ClanState>();
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenClanScreenAction(Hero hero)
	{
		ClanState gameState = base._game.GameStateManager.CreateState<ClanState>(new object[1] { hero });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenClanScreenAction(PartyBase party)
	{
		ClanState gameState = base._game.GameStateManager.CreateState<ClanState>(new object[1] { party });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenClanScreenAction(Settlement settlement)
	{
		ClanState gameState = base._game.GameStateManager.CreateState<ClanState>(new object[1] { settlement });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenClanScreenAction(Workshop workshop)
	{
		ClanState gameState = base._game.GameStateManager.CreateState<ClanState>(new object[1] { workshop });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenClanScreenAction(Alley alley)
	{
		ClanState gameState = base._game.GameStateManager.CreateState<ClanState>(new object[1] { alley });
		base._game.GameStateManager.PushState(gameState);
	}
}
