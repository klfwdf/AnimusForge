using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction.InteractionItems;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

[OverrideView(typeof(MissionAgentStatusUIHandler))]
public class MissionGauntletAgentStatus : MissionAgentStatusUIHandler
{
	protected GauntletLayer _gauntletLayer;

	protected MissionAgentStatusVM _dataSource;

	protected MissionMainAgentController _missionMainAgentController;

	protected MissionGauntletMainAgentEquipmentControllerView _missionMainAgentEquipmentControllerView;

	protected MissionHintLogic _missionHintLogic;

	protected bool _isInDeployment;

	public MissionAgentStatusVM DataSource => _dataSource;

	public override void AddInteractionMessage(MissionInteractionItemBaseVM message)
	{
		base.AddInteractionMessage(message);
		_dataSource?.InteractionInterface.AddSecondaryMessage(message);
	}

	public override void RemoveInteractionMessage(MissionInteractionItemBaseVM message)
	{
		base.RemoveInteractionMessage(message);
		_dataSource?.InteractionInterface.RemoveSecondaryMessage(message);
	}

	public override bool HasInteractionMessage(MissionInteractionItemBaseVM message)
	{
		if (_dataSource == null)
		{
			return false;
		}
		return _dataSource.InteractionInterface.HasSecondaryInteractionMessage(message);
	}

	public override void OnMissionStateActivated()
	{
		base.OnMissionStateActivated();
		_dataSource?.OnMainAgentWeaponChange();
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		_dataSource = new MissionAgentStatusVM(base.Mission, base.MissionScreen.CombatCamera, base.MissionScreen.GetCameraToggleProgress);
		_gauntletLayer = new GauntletLayer("MainAgentHUD", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MainAgentHUD", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		_dataSource.TakenDamageController.SetIsEnabled(BannerlordConfig.EnableDamageTakenVisuals);
		RegisterInteractionEvents();
		CombatLogManager.OnGenerateCombatLog += OnGenerateCombatLog;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
	}

	protected override void OnCreateView()
	{
		_dataSource.IsAgentStatusAvailable = true;
	}

	protected override void OnDestroyView()
	{
		_dataSource.IsAgentStatusAvailable = false;
	}

	protected override void OnSuspendView()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		}
	}

	protected override void OnResumeView()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		}
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableDamageTakenVisuals)
		{
			_dataSource?.TakenDamageController.SetIsEnabled(BannerlordConfig.EnableDamageTakenVisuals);
		}
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_dataSource?.InitializeMainAgentPropterties();
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_isInDeployment = base.Mission.Mode == MissionMode.Deployment;
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_isInDeployment = false;
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		UnregisterInteractionEvents();
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		CombatLogManager.OnGenerateCombatLog -= OnGenerateCombatLog;
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource?.OnFinalize();
		_dataSource = null;
		_missionMainAgentController = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.IsInDeployement = _isInDeployment;
		_dataSource.Tick(dt);
		_dataSource.InteractionInterface.DisplayInteractionText = !base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen;
	}

	public override void OnFocusGained(Agent mainAgent, IFocusable focusableObject, bool isInteractable)
	{
		base.OnFocusGained(mainAgent, focusableObject, isInteractable);
		_dataSource?.OnFocusGained(mainAgent, focusableObject, isInteractable);
	}

	public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		base.OnAgentInteraction(userAgent, agent, agentBoneIndex);
		_dataSource?.OnAgentInteraction(userAgent, agent, agentBoneIndex);
	}

	public override void OnFocusLost(Agent agent, IFocusable focusableObject)
	{
		base.OnFocusLost(agent, focusableObject);
		_dataSource?.OnFocusLost(agent, focusableObject);
	}

	public override void OnAgentDeleted(Agent affectedAgent)
	{
		_dataSource?.OnAgentDeleted(affectedAgent);
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		_dataSource?.OnAgentRemoved(affectedAgent);
	}

	private void OnGenerateCombatLog(CombatLogData logData)
	{
		if (logData.IsVictimAgentMine && logData.TotalDamage > 0)
		{
			_dataSource?.OnMainAgentHit(logData.TotalDamage, logData.IsRangedAttack ? 1 : 0);
		}
		else if (logData.IsAttackerAgentMine && logData.ReflectedDamage > 0)
		{
			_dataSource?.OnMainAgentHit(logData.ReflectedDamage, logData.IsRangedAttack ? 1 : 0);
		}
	}

	private void RegisterInteractionEvents()
	{
		_missionMainAgentController = base.Mission.GetMissionBehavior<MissionMainAgentController>();
		if (_missionMainAgentController != null)
		{
			_missionMainAgentController.InteractionComponent.OnFocusGained += _dataSource.OnSecondaryFocusGained;
			_missionMainAgentController.InteractionComponent.OnFocusLost += _dataSource.OnSecondaryFocusLost;
			_missionMainAgentController.InteractionComponent.OnFocusHealthChanged += _dataSource.InteractionInterface.OnFocusedHealthChanged;
		}
		_missionMainAgentEquipmentControllerView = base.Mission.GetMissionBehavior<MissionGauntletMainAgentEquipmentControllerView>();
		if (_missionMainAgentEquipmentControllerView != null)
		{
			_missionMainAgentEquipmentControllerView.OnEquipmentDropInteractionViewToggled += _dataSource.OnEquipmentInteractionViewToggled;
			_missionMainAgentEquipmentControllerView.OnEquipmentEquipInteractionViewToggled += _dataSource.OnEquipmentInteractionViewToggled;
		}
		_missionHintLogic = base.Mission.GetMissionBehavior<MissionHintLogic>();
		if (_missionHintLogic != null)
		{
			_missionHintLogic.OnActiveHintChanged += _dataSource.InteractionInterface.OnActiveMissionHintChanged;
		}
	}

	private void UnregisterInteractionEvents()
	{
		if (_missionMainAgentController != null)
		{
			_missionMainAgentController.InteractionComponent.OnFocusGained -= _dataSource.OnSecondaryFocusGained;
			_missionMainAgentController.InteractionComponent.OnFocusLost -= _dataSource.OnSecondaryFocusLost;
			_missionMainAgentController.InteractionComponent.OnFocusHealthChanged -= _dataSource.InteractionInterface.OnFocusedHealthChanged;
		}
		if (_missionMainAgentEquipmentControllerView != null)
		{
			_missionMainAgentEquipmentControllerView.OnEquipmentDropInteractionViewToggled -= _dataSource.OnEquipmentInteractionViewToggled;
			_missionMainAgentEquipmentControllerView.OnEquipmentEquipInteractionViewToggled -= _dataSource.OnEquipmentInteractionViewToggled;
		}
		_missionHintLogic = base.Mission.GetMissionBehavior<MissionHintLogic>();
		if (_missionHintLogic != null)
		{
			_missionHintLogic.OnActiveHintChanged -= _dataSource.InteractionInterface.OnActiveMissionHintChanged;
		}
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
		UnregisterInteractionEvents();
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
		RegisterInteractionEvents();
	}
}
