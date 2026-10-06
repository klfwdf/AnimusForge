using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

[OverrideView(typeof(MissionMainAgentCheerBarkControllerView))]
public class MissionGauntletMainAgentCheerControllerView : MissionView
{
	private const float CooldownPeriodDurationAfterCheer = 4f;

	private const float CooldownPeriodDurationAfterBark = 2f;

	private const float _minHoldTime = 0f;

	private readonly IMissionScreen _missionScreenAsInterface;

	private MissionMainAgentController _missionMainAgentController;

	private readonly TextObject _cooldownInfoText = new TextObject("{=aogZyZlR}You need to wait {SECONDS} seconds until you can cheer/shout again.");

	private bool _holdHandled;

	private float _holdTime;

	private bool _prevCheerKeyDown;

	private GauntletLayer _gauntletLayer;

	private MissionMainAgentCheerBarkControllerVM _dataSource;

	private float _cooldownTimeRemaining;

	private bool _isSelectingFromInput;

	private bool IsDisplayingADialog
	{
		get
		{
			IMissionScreen missionScreenAsInterface = _missionScreenAsInterface;
			if ((missionScreenAsInterface == null || !missionScreenAsInterface.GetDisplayDialog()) && !base.MissionScreen.IsRadialMenuActive)
			{
				return base.Mission.IsOrderMenuOpen;
			}
			return true;
		}
	}

	private bool HoldHandled
	{
		get
		{
			return _holdHandled;
		}
		set
		{
			_holdHandled = value;
		}
	}

	public MissionGauntletMainAgentCheerControllerView()
	{
		_missionScreenAsInterface = base.MissionScreen;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_gauntletLayer = new GauntletLayer("MissionCheerController", ViewOrderPriority);
		_missionMainAgentController = base.Mission.GetMissionBehavior<MissionMainAgentController>();
		_dataSource = new MissionMainAgentCheerBarkControllerVM(OnCheerSelect, OnBarkSelect);
		_gauntletLayer.LoadMovie("MainAgentCheerBarkController", _dataSource);
		GameKeyContext category = HotKeyManager.GetCategory("CombatHotKeyCategory");
		if (_missionMainAgentController != null && _missionMainAgentController.Input is InputContext inputContext && !inputContext.IsCategoryRegistered(category))
		{
			inputContext.RegisterHotKeyCategory(category);
		}
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.Mission.OnMainAgentChanged += OnMainAgentChanged;
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.Mission.OnMainAgentChanged -= OnMainAgentChanged;
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
		_missionMainAgentController = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (IsMainAgentAvailable() && base.Mission.Mode != MissionMode.Deployment && (!base.MissionScreen.IsRadialMenuActive || _dataSource.IsActive))
		{
			TickControls(dt);
		}
		else if (_dataSource.IsActive)
		{
			HandleClosingHold(applySelection: false);
		}
	}

	private void OnMainAgentChanged(Agent oldAgent)
	{
		if (base.Mission.MainAgent == null)
		{
			HandleClosingHold(applySelection: false);
		}
	}

	private void HandleNodeSelectionInput(CheerBarkNodeItemVM node, int nodeIndex, int parentNodeIndex = -1)
	{
		if (_missionMainAgentController == null)
		{
			return;
		}
		IInputContext input = _missionMainAgentController.Input;
		if (node.ShortcutKey == null)
		{
			return;
		}
		if (input.IsHotKeyPressed(node.ShortcutKey.HotKey.Id))
		{
			if (parentNodeIndex != -1)
			{
				_dataSource.SelectItem(parentNodeIndex, nodeIndex);
				return;
			}
			_dataSource.SelectItem(nodeIndex);
			_isSelectingFromInput = node.HasSubNodes;
		}
		else
		{
			if (!input.IsHotKeyReleased(node.ShortcutKey.HotKey.Id))
			{
				return;
			}
			if (!_isSelectingFromInput)
			{
				HandleClosingHold(applySelection: true);
				_dataSource.Nodes.ApplyActionOnAllItems(delegate(CheerBarkNodeItemVM n)
				{
					n.ClearSelectionRecursive();
				});
			}
			_isSelectingFromInput = false;
		}
	}

	private void TickControls(float dt)
	{
		if (_missionMainAgentController == null)
		{
			return;
		}
		IInputContext input = _missionMainAgentController.Input;
		if (GameNetwork.IsMultiplayer && _cooldownTimeRemaining > 0f)
		{
			_cooldownTimeRemaining -= dt;
			if (input.IsGameKeyDown(31))
			{
				if (!_prevCheerKeyDown && (double)_cooldownTimeRemaining >= 0.1)
				{
					_cooldownInfoText.SetTextVariable("SECONDS", _cooldownTimeRemaining.ToString("0.0"));
					InformationManager.DisplayMessage(new InformationMessage(_cooldownInfoText.ToString()));
				}
				_prevCheerKeyDown = true;
			}
			else
			{
				_prevCheerKeyDown = false;
			}
			return;
		}
		if (HoldHandled && _dataSource.IsActive)
		{
			int num = -1;
			for (int i = 0; i < _dataSource.Nodes.Count; i++)
			{
				if (_dataSource.Nodes[i].IsSelected)
				{
					num = i;
					break;
				}
			}
			if (_dataSource.IsNodesCategories)
			{
				if (num != -1)
				{
					for (int j = 0; j < _dataSource.Nodes[num].SubNodes.Count; j++)
					{
						HandleNodeSelectionInput(_dataSource.Nodes[num].SubNodes[j], j, num);
					}
				}
				else if (input.IsHotKeyReleased("CheerBarkSelectFirstCategory"))
				{
					_dataSource.SelectItem(0);
				}
				else if (input.IsHotKeyReleased("CheerBarkSelectSecondCategory"))
				{
					_dataSource.SelectItem(1);
				}
			}
			else
			{
				for (int k = 0; k < _dataSource.Nodes.Count; k++)
				{
					HandleNodeSelectionInput(_dataSource.Nodes[k], k);
				}
			}
		}
		if (input.IsGameKeyDown(31) && !IsDisplayingADialog && !base.MissionScreen.IsRadialMenuActive)
		{
			if (_holdTime > 0f && !HoldHandled)
			{
				HandleOpenHold();
				HoldHandled = true;
			}
			_holdTime += dt;
			_prevCheerKeyDown = true;
		}
		else if (_prevCheerKeyDown && !input.IsGameKeyDown(31))
		{
			if (_holdTime < 0f)
			{
				HandleQuickRelease();
			}
			else
			{
				HandleClosingHold(applySelection: true);
			}
			HoldHandled = false;
			_holdTime = 0f;
			_prevCheerKeyDown = false;
		}
	}

	private void HandleOpenHold()
	{
		if (!_dataSource.IsActive)
		{
			_dataSource.ExecuteActivate();
			base.MissionScreen.RegisterRadialMenuObject(this);
		}
	}

	private void HandleClosingHold(bool applySelection)
	{
		if (_dataSource.IsActive)
		{
			_dataSource.ExecuteDeactivate(applySelection);
			base.MissionScreen.UnregisterRadialMenuObject(this);
		}
	}

	private void HandleQuickRelease()
	{
		OnCheerSelect(-1);
	}

	private void OnCheerSelect(int tauntIndex)
	{
		if (tauntIndex < 0)
		{
			return;
		}
		if (GameNetwork.IsClient)
		{
			TauntUsageManager.TauntUsage.TauntUsageFlag actionNotUsableReason = CosmeticsManagerHelper.GetActionNotUsableReason(Agent.Main, tauntIndex);
			if (actionNotUsableReason != TauntUsageManager.TauntUsage.TauntUsageFlag.None)
			{
				InformationManager.DisplayMessage(new InformationMessage(TauntUsageManager.GetActionDisabledReasonText(actionNotUsableReason)));
				return;
			}
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new TauntSelected(tauntIndex));
			GameNetwork.EndModuleEventAsClient();
		}
		else
		{
			Agent.Main?.HandleTaunt(tauntIndex, isDefaultTaunt: true);
		}
		_cooldownTimeRemaining = 4f;
	}

	private void OnBarkSelect(int indexOfBark)
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new BarkSelected(indexOfBark));
			GameNetwork.EndModuleEventAsClient();
		}
		else
		{
			Agent.Main?.HandleBark(indexOfBark);
		}
		_cooldownTimeRemaining = 2f;
	}

	private bool IsMainAgentAvailable()
	{
		Agent main = Agent.Main;
		if (main != null && main.IsActive() && !Agent.Main.IsUsingGameObject)
		{
			return !Agent.Main.IsInWater();
		}
		return false;
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}
}
