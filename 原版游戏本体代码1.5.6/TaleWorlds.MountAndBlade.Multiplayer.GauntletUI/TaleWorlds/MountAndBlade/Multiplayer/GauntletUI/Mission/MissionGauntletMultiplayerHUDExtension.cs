using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerHUDExtensionUIHandler))]
public class MissionGauntletMultiplayerHUDExtension : MissionView
{
	private MissionMultiplayerHUDExtensionVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _mpMissionCategory;

	private MissionLobbyComponent _lobbyComponent;

	public MissionMultiplayerHUDExtensionVM DataSource => _dataSource;

	public MissionGauntletMultiplayerHUDExtension()
	{
		ViewOrderPriority = 2;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_mpMissionCategory = UIResourceManager.LoadSpriteCategory("ui_mpmission");
		_dataSource = new MissionMultiplayerHUDExtensionVM(base.Mission);
		_gauntletLayer = new GauntletLayer("HUDExtension", ViewOrderPriority);
		_gauntletLayer.LoadMovie("HUDExtension", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.MissionScreen.OnSpectateAgentFocusIn += _dataSource.OnSpectatedAgentFocusIn;
		base.MissionScreen.OnSpectateAgentFocusOut += _dataSource.OnSpectatedAgentFocusOut;
		_dataSource.OnPlayerFollowRequested += OnPlayerFollowRequested;
		_dataSource.SpectatorControls.OnCycleTargetRequested += OnCycleTargetRequested;
		Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnMissionPlayerToggledOrderViewEvent);
		_lobbyComponent = base.Mission.GetMissionBehavior<MissionLobbyComponent>();
		_lobbyComponent.OnPostMatchEnded += OnPostMatchEnded;
		GameKeyContext category = HotKeyManager.GetCategory("ScoreboardHotKeyCategory");
		if (!base.MissionScreen.SceneLayer.Input.IsCategoryRegistered(category))
		{
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category);
		}
	}

	public override void OnMissionScreenFinalize()
	{
		_lobbyComponent.OnPostMatchEnded -= OnPostMatchEnded;
		base.MissionScreen.OnSpectateAgentFocusIn -= _dataSource.OnSpectatedAgentFocusIn;
		base.MissionScreen.OnSpectateAgentFocusOut -= _dataSource.OnSpectatedAgentFocusOut;
		_dataSource.OnPlayerFollowRequested -= OnPlayerFollowRequested;
		_dataSource.SpectatorControls.OnCycleTargetRequested -= OnCycleTargetRequested;
		_gauntletLayer.InputRestrictions.ResetInputRestrictions();
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_mpMissionCategory?.Unload();
		_dataSource.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
		Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnMissionPlayerToggledOrderViewEvent);
		base.OnMissionScreenFinalize();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.Tick(dt);
		UpdateOverlayInputClaim();
		if (MultiplayerSpectatorHelper.IsLocalPeerSpectator() && base.MissionScreen.SceneLayer.Input.IsHotKeyPressed("ToggleHud"))
		{
			_dataSource.IsRetracted = !_dataSource.IsRetracted;
		}
	}

	private void UpdateOverlayInputClaim()
	{
		if (_gauntletLayer == null)
		{
			return;
		}
		bool flag = _dataSource != null && !TaleWorlds.InputSystem.Input.IsGamepadActive && MultiplayerSpectatorHelper.IsLocalPeerSpectator();
		bool flag2 = _gauntletLayer.InputRestrictions.InputUsageMask == InputUsageMask.Mouse;
		if (flag != flag2)
		{
			if (flag)
			{
				_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Mouse);
			}
			else
			{
				_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			}
		}
		if (flag && ScreenManager.FocusedLayer == _gauntletLayer && !_gauntletLayer.IsFocusedOnInput())
		{
			ScreenManager.TryLoseFocus(_gauntletLayer);
		}
	}

	private void OnCycleTargetRequested(int direction)
	{
		base.MissionScreen.RequestSpectatorCycle(direction);
	}

	private void OnPlayerFollowRequested(Agent agent)
	{
		if (agent != null && agent.IsCameraAttachable())
		{
			base.MissionScreen.SetAgentToFollow(agent);
			base.MissionScreen.SuppressSpectatorCyclingThisFrame();
		}
	}

	private void OnMissionPlayerToggledOrderViewEvent(MissionPlayerToggledOrderViewEvent eventObj)
	{
		_dataSource.IsOrderActive = eventObj.IsOrderEnabled;
	}

	private void OnPostMatchEnded()
	{
		_dataSource.IsRetracted = false;
		_dataSource.OnMatchEnded();
	}
}
