using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
	private static readonly string[] _spectatorToggleLayerNames = new string[4] { "MultiplayerKillFeed", "MPMissionMarkers", "MultiplayerScoreboard", "HUDExtension" };

	private MissionMultiplayerHUDExtensionVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private SpriteCategory _mpMissionCategory;

	private MissionLobbyComponent _lobbyComponent;

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
		SetAllSpectatorLayersVisible(isVisible: true);
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
			_dataSource.ShowHud = !_dataSource.ShowHud;
			SetAllSpectatorLayersVisible(_dataSource.ShowHud);
			if (!_dataSource.ShowHud)
			{
				TextObject textObject = new TextObject("{=RsT4BS5O}Spectator UI hidden. Press {KEY} to show it again.");
				textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("ScoreboardHotKeyCategory", "ToggleHud")));
				MBInformationManager.AddQuickInformation(textObject);
			}
		}
	}

	private void UpdateOverlayInputClaim()
	{
		if (_gauntletLayer == null)
		{
			return;
		}
		bool flag = _dataSource != null && MultiplayerSpectatorHelper.IsLocalPeerSpectator();
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

	private void SetAllSpectatorLayersVisible(bool isVisible)
	{
		foreach (ScreenLayer layer in base.MissionScreen.Layers)
		{
			if (!(layer is GauntletLayer gauntletLayer))
			{
				continue;
			}
			string[] spectatorToggleLayerNames = _spectatorToggleLayerNames;
			foreach (string text in spectatorToggleLayerNames)
			{
				if (gauntletLayer.Name == text)
				{
					ScreenManager.SetSuspendLayer(gauntletLayer, !isVisible);
					break;
				}
			}
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
		_dataSource.ShowHud = false;
		SetAllSpectatorLayersVisible(isVisible: true);
	}
}
