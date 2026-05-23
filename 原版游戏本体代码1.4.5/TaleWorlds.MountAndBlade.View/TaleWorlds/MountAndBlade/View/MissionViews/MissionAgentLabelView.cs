using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionAgentLabelView : MissionView
{
	private const float _highlightedLabelScaleFactor = 20f;

	private const float _labelBannerWidth = 0.4f;

	private const float _labelBlackBorderWidth = 0.44f;

	private readonly Vec3 _meshOffset = new Vec3(0f, 0f, 2f);

	private const float _nearDistance = 1.5f;

	private const float _farDistance = 8f;

	private readonly List<Agent> _closeAgentsWithMeshes;

	private readonly Dictionary<Agent, MetaMesh> _agentMeshes;

	private readonly Dictionary<Texture, Material> _labelMaterials;

	private bool _isSuspendingView;

	private bool _isResumingView;

	private bool _isOrderFlagVisible;

	private bool _alwaysShowFriendlyTroopBanners;

	private bool _indicatorsActive;

	private bool IndicatorsActive
	{
		get
		{
			return _indicatorsActive;
		}
		set
		{
			if (_indicatorsActive != value)
			{
				_indicatorsActive = value;
				UpdateAllAgentMeshVisibilities();
			}
		}
	}

	private OrderController PlayerOrderController => base.Mission.PlayerTeam?.PlayerOrderController;

	private SiegeWeaponController PlayerSiegeWeaponController => base.Mission.PlayerTeam?.PlayerOrderController.SiegeWeaponController;

	public MissionAgentLabelView()
	{
		_agentMeshes = new Dictionary<Agent, MetaMesh>();
		_labelMaterials = new Dictionary<Texture, Material>();
		_closeAgentsWithMeshes = new List<Agent>();
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		base.Mission.Teams.OnPlayerTeamChanged += Mission_OnPlayerTeamChanged;
		base.Mission.OnMainAgentChanged += OnMainAgentChanged;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		base.MissionScreen.OnSpectateAgentFocusIn += HandleSpectateAgentFocusIn;
		base.MissionScreen.OnSpectateAgentFocusOut += HandleSpectateAgentFocusOut;
	}

	public override void AfterStart()
	{
		if (PlayerOrderController != null)
		{
			PlayerOrderController.OnSelectedFormationsChanged += OrderController_OnSelectedFormationsChanged;
			base.Mission.PlayerTeam.OnFormationsChanged += PlayerTeam_OnFormationsChanged;
		}
		BannerBearerLogic missionBehavior = base.Mission.GetMissionBehavior<BannerBearerLogic>();
		if (missionBehavior != null)
		{
			missionBehavior.OnBannerBearerAgentUpdated += BannerBearerLogic_OnBannerBearerAgentUpdated;
		}
		UpdateAlwaysShowFriendlyTroopBanners();
	}

	public override void OnMissionTick(float dt)
	{
		bool isOrderFlagVisible = _isOrderFlagVisible;
		UpdateIsOrderFlagVisible();
		if (!_isOrderFlagVisible && isOrderFlagVisible)
		{
			UpdateAllAgentMeshVisibilities();
			SetHighlightForAgents(highlight: false, useSiegeMachineUsers: false, useAllTeamAgents: false);
			SetHighlightForAgents(highlight: false, useSiegeMachineUsers: true, useAllTeamAgents: false);
		}
		if (_isOrderFlagVisible && !isOrderFlagVisible)
		{
			UpdateAllAgentMeshVisibilities();
			SetHighlightForAgents(highlight: true, useSiegeMachineUsers: false, useAllTeamAgents: false);
			SetHighlightForAgents(highlight: true, useSiegeMachineUsers: true, useAllTeamAgents: false);
		}
		UpdateProximityBannerTransparencies();
		IndicatorsActive = _alwaysShowFriendlyTroopBanners || base.Input.IsGameKeyDown(5);
	}

	private void UpdateProximityBannerTransparencies()
	{
		for (int i = 0; i < _closeAgentsWithMeshes.Count; i++)
		{
			Agent agent = _closeAgentsWithMeshes[i];
			SetBannerHighlightVisibility(agent, IsAgentListeningToOrders(agent));
		}
		_closeAgentsWithMeshes.Clear();
		AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(base.Mission, base.MissionScreen.CombatCamera.Position.AsVec2, 8f);
		while (searchStruct.LastFoundAgent != null)
		{
			if (_agentMeshes.ContainsKey(searchStruct.LastFoundAgent))
			{
				_closeAgentsWithMeshes.Add(searchStruct.LastFoundAgent);
			}
			AgentProximityMap.FindNext(base.Mission, ref searchStruct);
		}
		for (int j = 0; j < _closeAgentsWithMeshes.Count; j++)
		{
			Agent agent2 = _closeAgentsWithMeshes[j];
			SetBannerHighlightVisibility(agent2, IsAgentListeningToOrders(agent2));
		}
	}

	public override void OnRemoveBehavior()
	{
		UnregisterEvents();
		base.OnRemoveBehavior();
	}

	public override void OnMissionScreenFinalize()
	{
		UnregisterEvents();
		base.OnMissionScreenFinalize();
	}

	private void UnregisterEvents()
	{
		if (base.Mission != null)
		{
			base.Mission.Teams.OnPlayerTeamChanged -= Mission_OnPlayerTeamChanged;
			base.Mission.OnMainAgentChanged -= OnMainAgentChanged;
		}
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		if (base.MissionScreen != null)
		{
			base.MissionScreen.OnSpectateAgentFocusIn -= HandleSpectateAgentFocusIn;
			base.MissionScreen.OnSpectateAgentFocusOut -= HandleSpectateAgentFocusOut;
		}
		if (PlayerOrderController != null)
		{
			PlayerOrderController.OnSelectedFormationsChanged -= OrderController_OnSelectedFormationsChanged;
			if (base.Mission != null)
			{
				base.Mission.PlayerTeam.OnFormationsChanged -= PlayerTeam_OnFormationsChanged;
			}
		}
		BannerBearerLogic missionBehavior = base.Mission.GetMissionBehavior<BannerBearerLogic>();
		if (missionBehavior != null)
		{
			missionBehavior.OnBannerBearerAgentUpdated -= BannerBearerLogic_OnBannerBearerAgentUpdated;
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		RemoveAgentLabel(affectedAgent);
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		InitAgentLabel(agent, banner);
	}

	public override void OnAssignPlayerAsSergeantOfFormation(Agent agent)
	{
		SetBannerHighlightVisibility(agent, highlightVisibility: true);
	}

	public override void OnClearScene()
	{
		_agentMeshes.Clear();
		_labelMaterials.Clear();
		_closeAgentsWithMeshes.Clear();
	}

	private void PlayerTeam_OnFormationsChanged(Team team, Formation formation)
	{
		UpdateIsOrderFlagVisible();
		if (_isOrderFlagVisible)
		{
			DehighlightAllAgents();
			SetHighlightForAgents(highlight: true, useSiegeMachineUsers: false, useAllTeamAgents: false);
		}
	}

	private void Mission_OnPlayerTeamChanged(Team previousTeam, Team currentTeam)
	{
		DehighlightAllAgents();
		_isOrderFlagVisible = false;
		if (previousTeam?.PlayerOrderController != null)
		{
			previousTeam.PlayerOrderController.OnSelectedFormationsChanged -= OrderController_OnSelectedFormationsChanged;
			previousTeam.PlayerOrderController.SiegeWeaponController.OnSelectedSiegeWeaponsChanged -= PlayerSiegeWeaponController_OnSelectedSiegeWeaponsChanged;
		}
		if (PlayerOrderController != null)
		{
			PlayerOrderController.OnSelectedFormationsChanged += OrderController_OnSelectedFormationsChanged;
			PlayerSiegeWeaponController.OnSelectedSiegeWeaponsChanged += PlayerSiegeWeaponController_OnSelectedSiegeWeaponsChanged;
		}
		SetHighlightForAgents(highlight: true, useSiegeMachineUsers: false, useAllTeamAgents: true);
		foreach (Agent agent in base.Mission.Agents)
		{
			UpdateVisibilityOfAgentMesh(agent);
		}
	}

	private void OrderController_OnSelectedFormationsChanged()
	{
		UpdateAllAgentMeshVisibilities();
		DehighlightAllAgents();
		UpdateIsOrderFlagVisible();
		if (_isOrderFlagVisible)
		{
			SetHighlightForAgents(highlight: true, useSiegeMachineUsers: false, useAllTeamAgents: false);
		}
	}

	private void PlayerSiegeWeaponController_OnSelectedSiegeWeaponsChanged()
	{
		DehighlightAllAgents();
		SetHighlightForAgents(highlight: true, useSiegeMachineUsers: true, useAllTeamAgents: false);
	}

	private void BannerBearerLogic_OnBannerBearerAgentUpdated(Agent agent, bool isBannerBearer)
	{
		RemoveAgentLabel(agent);
		InitAgentLabel(agent);
	}

	private void RemoveAgentLabel(Agent agent)
	{
		if (agent.IsHuman && _agentMeshes.ContainsKey(agent))
		{
			if (agent.AgentVisuals != null)
			{
				agent.AgentVisuals.ReplaceMeshWithMesh(_agentMeshes[agent], null, BodyMeshTypes.Label);
			}
			_agentMeshes.Remove(agent);
		}
		if (_closeAgentsWithMeshes.Contains(agent))
		{
			_closeAgentsWithMeshes.Remove(agent);
		}
	}

	private void InitAgentLabel(Agent agent, Banner peerBanner = null)
	{
		if (!agent.IsHuman)
		{
			return;
		}
		Banner banner = peerBanner ?? agent.Origin.Banner;
		if (banner == null)
		{
			return;
		}
		Texture texture = null;
		MetaMesh copy = MetaMesh.GetCopy("troop_banner_selection", showErrors: false, mayReturnNull: true);
		Material tableauMaterial = Material.GetFromResource("agent_label_with_tableau");
		texture = banner.GetTableauTextureSmall(BannerDebugInfo.CreateManual(GetType().Name), null);
		if (!(copy != null) || !(tableauMaterial != null))
		{
			return;
		}
		Texture fromResource = Texture.GetFromResource("banner_top_of_head");
		if (_labelMaterials.TryGetValue(texture ?? fromResource, out var value))
		{
			tableauMaterial = value;
		}
		else
		{
			tableauMaterial = tableauMaterial.CreateCopy();
			Action<Texture> setAction = delegate(Texture tex)
			{
				tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap, tex);
			};
			texture = banner.GetTableauTextureSmall(BannerDebugInfo.CreateManual(GetType().Name), setAction);
			tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, fromResource);
			_labelMaterials.Add(texture, tableauMaterial);
		}
		copy.SetMaterial(tableauMaterial);
		copy.SetVectorArgument(0.5f, 0.5f, 0.25f, 0.25f);
		agent.AgentVisuals.AddMultiMesh(copy, BodyMeshTypes.Label);
		_agentMeshes.Add(agent, copy);
		UpdateVisibilityOfAgentMesh(agent);
		SetBannerHighlightVisibility(agent, highlightVisibility: false);
	}

	private void UpdateVisibilityOfAgentMesh(Agent agent)
	{
		if (agent.IsActive() && _agentMeshes.ContainsKey(agent))
		{
			bool flag = IsMeshVisibleForAgent(agent);
			_agentMeshes[agent].SetVisibilityMask(flag ? VisibilityMaskFlags.Final : ((VisibilityMaskFlags)0u));
		}
	}

	private bool IsMeshVisibleForAgent(Agent agent)
	{
		if ((_isResumingView || (!base.IsViewSuspended && !_isSuspendingView)) && IsAllyInAllyTeam(agent) && base.MissionScreen.LastFollowedAgent != agent && BannerlordConfig.FriendlyTroopsBannerOpacity > 0f && !base.MissionScreen.IsPhotoModeEnabled)
		{
			if (!IndicatorsActive && base.Mission.Mode != MissionMode.Deployment)
			{
				return IsAgentListeningToOrders(agent);
			}
			return true;
		}
		return false;
	}

	public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
	{
		base.OnMissionModeChange(oldMissionMode, atStart);
		UpdateAllAgentMeshVisibilities();
	}

	private void OnUpdateOpacityValueOfAgentMesh(Agent agent)
	{
		if (agent.IsActive() && _agentMeshes.ContainsKey(agent))
		{
			SetBannerHighlightVisibility(agent, IsAgentListeningToOrders(agent));
		}
	}

	private bool IsAllyInAllyTeam(Agent agent)
	{
		if (agent?.Team != null && base.Mission != null && agent != base.Mission.MainAgent)
		{
			Team team = null;
			Team team2;
			if (GameNetwork.IsSessionActive)
			{
				team2 = ((!GameNetwork.IsMyPeerReady) ? null : GameNetwork.MyPeer?.GetComponent<MissionPeer>()?.Team);
			}
			else
			{
				team2 = base.Mission.PlayerTeam;
				team = base.Mission.PlayerAllyTeam;
			}
			if (agent.Team != team2)
			{
				return agent.Team == team;
			}
			return true;
		}
		return false;
	}

	private void OnMainAgentChanged(Agent oldAgent)
	{
		UpdateAllAgentMeshVisibilities();
	}

	private void HandleSpectateAgentFocusIn(Agent agent)
	{
		UpdateVisibilityOfAgentMesh(agent);
	}

	private void HandleSpectateAgentFocusOut(Agent agent)
	{
		UpdateVisibilityOfAgentMesh(agent);
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType optionType)
	{
		if (optionType == ManagedOptions.ManagedOptionsType.AlwaysShowFriendlyTroopBannersType)
		{
			UpdateAlwaysShowFriendlyTroopBanners();
			UpdateAllAgentMeshVisibilities();
		}
		if (optionType == ManagedOptions.ManagedOptionsType.FriendlyTroopsBannerOpacity || optionType == ManagedOptions.ManagedOptionsType.AlwaysShowFriendlyTroopBannersType)
		{
			UpdateAllAgentMeshVisibilities();
		}
	}

	private void UpdateAlwaysShowFriendlyTroopBanners()
	{
		float config = ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.AlwaysShowFriendlyTroopBannersType);
		_alwaysShowFriendlyTroopBanners = config == 2f || (config == 1f && GameNetwork.IsMultiplayer);
	}

	private void UpdateAllAgentMeshVisibilities()
	{
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.IsHuman)
			{
				UpdateVisibilityOfAgentMesh(agent);
				if (IsMeshVisibleForAgent(agent))
				{
					OnUpdateOpacityValueOfAgentMesh(agent);
				}
			}
		}
	}

	private bool IsAgentListeningToOrders(Agent agent)
	{
		UpdateIsOrderFlagVisible();
		if (!_isOrderFlagVisible)
		{
			return false;
		}
		if (PlayerOrderController != null && agent.Formation != null && PlayerOrderController.IsFormationListening(agent.Formation))
		{
			return true;
		}
		if (PlayerSiegeWeaponController != null && agent.IsUsingGameObject)
		{
			UsableMissionObject currentlyUsedGameObject = agent.CurrentlyUsedGameObject;
			for (int i = 0; i < PlayerSiegeWeaponController.SelectedWeapons.Count; i++)
			{
				TaleWorlds.MountAndBlade.SiegeWeapon siegeWeapon = PlayerSiegeWeaponController.SelectedWeapons[i];
				for (int j = 0; j < siegeWeapon.StandingPoints.Count; j++)
				{
					if (currentlyUsedGameObject == siegeWeapon.StandingPoints[j])
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private void SetBannerHighlightVisibility(Agent agent, bool highlightVisibility)
	{
		if (!_agentMeshes.TryGetValue(agent, out var value))
		{
			Debug.FailedAssert("Trying to update the banner of an agent that isn't present in _agentMeshes!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\MissionViews\\MissionAgentLabelView.cs", "SetBannerHighlightVisibility", 499);
			return;
		}
		float num = (highlightVisibility ? 1f : (-1f));
		float num2 = (agent.Position + _meshOffset).Distance(base.MissionScreen.CombatCamera.Position);
		if (num2 < 1.5f)
		{
			num = 0f;
		}
		else if (num2 < 8f)
		{
			num *= (num2 - 1.5f) / 6.5f;
		}
		value.SetVectorArgument2(20f, 0.4f, 0.44f, num * BannerlordConfig.FriendlyTroopsBannerOpacity);
	}

	private void UpdateIsOrderFlagVisible()
	{
		_isOrderFlagVisible = PlayerOrderController != null && base.MissionScreen.OrderFlag != null && base.MissionScreen.OrderFlag.IsVisible;
	}

	private void SetHighlightForAgents(bool highlight, bool useSiegeMachineUsers, bool useAllTeamAgents)
	{
		if (PlayerOrderController == null)
		{
			bool flag = base.Mission.PlayerTeam == null;
			Debug.Print($"PlayerOrderController is null and playerTeamIsNull: {flag}", 0, Debug.DebugColor.White, 17179869184uL);
		}
		if (useSiegeMachineUsers)
		{
			foreach (TaleWorlds.MountAndBlade.SiegeWeapon selectedWeapon in PlayerSiegeWeaponController.SelectedWeapons)
			{
				foreach (StandingPoint standingPoint in selectedWeapon.StandingPoints)
				{
					Agent userAgent = standingPoint.UserAgent;
					if (userAgent != null)
					{
						SetBannerHighlightVisibility(userAgent, highlight);
					}
				}
			}
			return;
		}
		if (useAllTeamAgents)
		{
			if (PlayerOrderController.Owner != null)
			{
				Team team = PlayerOrderController.Owner.Team;
				if (team == null)
				{
					Debug.Print("PlayerOrderController.Owner.Team is null, overriding with Mission.Current.PlayerTeam", 0, Debug.DebugColor.White, 17179869184uL);
					team = Mission.Current.PlayerTeam;
				}
				{
					foreach (Agent activeAgent in team.ActiveAgents)
					{
						SetBannerHighlightVisibility(activeAgent, highlight);
					}
					return;
				}
			}
			Debug.Print("PlayerOrderController.Owner is null", 0, Debug.DebugColor.White, 17179869184uL);
			return;
		}
		foreach (Formation selectedFormation in PlayerOrderController.SelectedFormations)
		{
			selectedFormation.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				SetBannerHighlightVisibility(agent, highlight);
			});
		}
	}

	private void DehighlightAllAgents()
	{
		foreach (KeyValuePair<Agent, MetaMesh> agentMesh in _agentMeshes)
		{
			SetBannerHighlightVisibility(agentMesh.Key, highlightVisibility: false);
		}
	}

	public override void OnAgentTeamChanged(Team prevTeam, Team newTeam, Agent agent)
	{
		UpdateVisibilityOfAgentMesh(agent);
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		UpdateAllAgentMeshVisibilities();
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		UpdateAllAgentMeshVisibilities();
	}

	protected override void OnSuspendView()
	{
		base.OnSuspendView();
		_isSuspendingView = true;
		UpdateAllAgentMeshVisibilities();
		_isSuspendingView = false;
	}

	protected override void OnResumeView()
	{
		base.OnResumeView();
		_isResumingView = true;
		UpdateAllAgentMeshVisibilities();
		_isResumingView = false;
	}
}
