using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Navigation.NavigationElements;

public class KingdomNavigationElement : MapNavigationElementBase
{
	private readonly TextObject _needToBeInKingdomText;

	public override string StringId => "kingdom";

	public override bool IsActive => base._game.GameStateManager.ActiveState is KingdomState;

	public override bool IsLockingNavigation => false;

	public override bool HasAlert => false;

	public KingdomNavigationElement(MapNavigationHandler handler)
		: base(handler)
	{
		_needToBeInKingdomText = GameTexts.FindText("str_need_to_be_a_part_of_kingdom");
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
		if (!Hero.MainHero.MapFaction.IsKingdomFaction)
		{
			return new NavigationPermissionItem(isAuthorized: false, _needToBeInKingdomText);
		}
		Mission current = Mission.Current;
		if (current != null && !current.IsKingdomWindowAccessAllowed)
		{
			return new NavigationPermissionItem(isAuthorized: false, null);
		}
		return new NavigationPermissionItem(isAuthorized: true, null);
	}

	protected override TextObject GetTooltip()
	{
		if (!Input.IsGamepadActive && (base.Permission.IsAuthorized || IsActive))
		{
			string variable = Game.Current.GameTextManager.GetHotKeyGameText("GenericCampaignPanelsGameKeyCategory", 40).ToString();
			TextObject textObject = GameTexts.FindText("str_hotkey_with_hint");
			textObject.SetTextVariable("TEXT", GameTexts.FindText("str_kingdom").ToString());
			textObject.SetTextVariable("HOTKEY", variable);
			return textObject;
		}
		return GameTexts.FindText("str_kingdom");
	}

	protected override TextObject GetAlertTooltip()
	{
		return TextObject.GetEmpty();
	}

	public override void OpenView()
	{
		PrepareToOpenKingdomScreen(delegate
		{
			OpenKingdomAction();
		});
	}

	public override void OpenView(params object[] parameters)
	{
		if (parameters.Length == 0)
		{
			return;
		}
		object obj = parameters[0];
		Army army;
		Settlement settlement;
		Clan clan;
		PolicyObject policy;
		IFaction faction;
		KingdomDecision decision;
		if ((army = obj as Army) != null)
		{
			PrepareToOpenKingdomScreen(delegate
			{
				OpenKingdomAction(army);
			});
		}
		else if ((settlement = obj as Settlement) != null)
		{
			PrepareToOpenKingdomScreen(delegate
			{
				OpenKingdomAction(settlement);
			});
		}
		else if ((clan = obj as Clan) != null)
		{
			PrepareToOpenKingdomScreen(delegate
			{
				OpenKingdomAction(clan);
			});
		}
		else if ((policy = obj as PolicyObject) != null)
		{
			PrepareToOpenKingdomScreen(delegate
			{
				OpenKingdomAction(policy);
			});
		}
		else if ((faction = obj as IFaction) != null)
		{
			PrepareToOpenKingdomScreen(delegate
			{
				OpenKingdomAction(faction);
			});
		}
		else if ((decision = obj as KingdomDecision) != null)
		{
			PrepareToOpenKingdomScreen(delegate
			{
				OpenKingdomAction(decision);
			});
		}
		else
		{
			Debug.FailedAssert($"Invalid parameter type when opening the kingdom screen from navigation: {obj.GetType()}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Map\\Navigation\\NavigationElements\\KindomNavigationElement.cs", "OpenView", 113);
		}
	}

	public override void GoToLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(Hero.MainHero.MapFaction.EncyclopediaLink);
	}

	private void PrepareToOpenKingdomScreen(Action openKingdomAction)
	{
		if (base.Permission.IsAuthorized)
		{
			if (ScreenManager.TopScreen is IChangeableScreen changeableScreen && changeableScreen.AnyUnsavedChanges())
			{
				InformationManager.ShowInquiry(changeableScreen.CanChangesBeApplied() ? MapNavigationHelper.GetUnsavedChangedInquiry(openKingdomAction) : MapNavigationHelper.GetUnapplicableChangedInquiry());
			}
			else
			{
				MapNavigationHelper.SwitchToANewScreen(openKingdomAction);
			}
		}
	}

	private void OpenKingdomAction()
	{
		KingdomState gameState = base._game.GameStateManager.CreateState<KingdomState>();
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenKingdomAction(Army army)
	{
		KingdomState gameState = base._game.GameStateManager.CreateState<KingdomState>(new object[1] { army });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenKingdomAction(Settlement settlement)
	{
		KingdomState gameState = base._game.GameStateManager.CreateState<KingdomState>(new object[1] { settlement });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenKingdomAction(Clan clan)
	{
		KingdomState gameState = base._game.GameStateManager.CreateState<KingdomState>(new object[1] { clan });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenKingdomAction(PolicyObject policy)
	{
		KingdomState gameState = base._game.GameStateManager.CreateState<KingdomState>(new object[1] { policy });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenKingdomAction(IFaction faction)
	{
		KingdomState gameState = base._game.GameStateManager.CreateState<KingdomState>(new object[1] { faction });
		base._game.GameStateManager.PushState(gameState);
	}

	private void OpenKingdomAction(KingdomDecision decision)
	{
		KingdomState gameState = base._game.GameStateManager.CreateState<KingdomState>(new object[1] { decision });
		base._game.GameStateManager.PushState(gameState);
	}
}
