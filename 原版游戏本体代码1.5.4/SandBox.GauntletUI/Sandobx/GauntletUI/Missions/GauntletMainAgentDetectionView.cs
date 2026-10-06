using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.View.Missions;
using SandBox.ViewModelCollection;
using SandBox.ViewModelCollection.Missions.MainAgentDetection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace Sandobx.GauntletUI.Missions;

[OverrideView(typeof(MissionMainAgentDetectionView))]
public class GauntletMainAgentDetectionView : MissionMainAgentDetectionView
{
	private GauntletLayer _markersGauntletLayer;

	private GauntletLayer _losingTargetGauntletLayer;

	private GauntletLayer _detectionBarGauntletLayer;

	private MainAgentDetectionVM _detectionDataSource;

	private MissionDisguiseMarkersVM _markersDataSource;

	private MissionLosingTargetVM _losingTargetDataSource;

	private DisguiseMissionLogic _disguiseMissionLogic;

	private float _lastSuspicousLevel;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_detectionDataSource = new MainAgentDetectionVM();
		_losingTargetDataSource = new MissionLosingTargetVM();
		_markersDataSource = new MissionDisguiseMarkersVM();
		_detectionBarGauntletLayer = new GauntletLayer("MissionMainAgentDetection", 10);
		_detectionBarGauntletLayer.LoadMovie("MissionMainAgentDetection", _detectionDataSource);
		_losingTargetGauntletLayer = new GauntletLayer("MissionLosingTarget", 11);
		_losingTargetGauntletLayer.LoadMovie("MissionLosingTarget", _losingTargetDataSource);
		_markersGauntletLayer = new GauntletLayer("MissionDetectionMarkers", 12);
		_markersGauntletLayer.LoadMovie("MissionDetectionMarkers", _markersDataSource);
		base.MissionScreen.AddLayer(_detectionBarGauntletLayer);
		base.MissionScreen.AddLayer(_losingTargetGauntletLayer);
		base.MissionScreen.AddLayer(_markersGauntletLayer);
	}

	public override void AfterStart()
	{
		_disguiseMissionLogic = base.Mission.GetMissionBehavior<DisguiseMissionLogic>();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		_detectionDataSource.OnFinalize();
		base.MissionScreen.RemoveLayer(_detectionBarGauntletLayer);
		_detectionBarGauntletLayer = null;
		_detectionDataSource = null;
		_disguiseMissionLogic = null;
	}

	private void UpdateSuspicion(float dt)
	{
		_detectionDataSource.UpdateDetectionValues(0f, 1f, _disguiseMissionLogic.PlayerSuspiciousLevel);
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_disguiseMissionLogic != null)
		{
			if (_losingTargetDataSource != null)
			{
				UpdateLosingTarget(dt);
			}
			if (_detectionDataSource != null)
			{
				UpdateSuspicion(dt);
			}
			if (_markersDataSource != null)
			{
				UpdateMarkers(dt);
			}
			_lastSuspicousLevel = _disguiseMissionLogic.PlayerSuspiciousLevel;
		}
	}

	private void UpdateLosingTarget(float dt)
	{
		_losingTargetDataSource.UpdateLosingTargetValues(isLosingTarget: false, 0f, 1f);
	}

	private void UpdateMarkers(float dt)
	{
		bool isInStealthMode = _disguiseMissionLogic.IsInStealthMode;
		bool isSuspicious = isInStealthMode || _lastSuspicousLevel < _disguiseMissionLogic.PlayerSuspiciousLevel;
		List<MissionDisguiseMarkerItemVM> list = new List<MissionDisguiseMarkerItemVM>();
		foreach (MissionDisguiseMarkerItemVM hostileAgent in _markersDataSource.HostileAgents)
		{
			if (_disguiseMissionLogic.GetAgentOffenseInfo(hostileAgent?.OffenseInfo.Agent) == null)
			{
				list.Add(hostileAgent);
			}
		}
		foreach (KeyValuePair<Agent, DisguiseMissionLogic.ShadowingAgentOffenseInfo> threatAgentInfo in _disguiseMissionLogic.ThreatAgentInfos)
		{
			bool flag = true;
			foreach (MissionDisguiseMarkerItemVM hostileAgent2 in _markersDataSource.HostileAgents)
			{
				if (threatAgentInfo.Key != null && hostileAgent2.OffenseInfo?.Agent == threatAgentInfo.Key)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				_markersDataSource.HostileAgents.Add(new MissionDisguiseMarkerItemVM(base.MissionScreen.CombatCamera, threatAgentInfo.Value));
			}
		}
		foreach (MissionDisguiseMarkerItemVM item in list)
		{
			_markersDataSource.HostileAgents.Remove(item);
		}
		foreach (MissionDisguiseMarkerItemVM hostileAgent3 in _markersDataSource.HostileAgents)
		{
			if (hostileAgent3.OffenseInfo.Agent.IsActive())
			{
				Vec3 origin = base.MissionScreen.CombatCamera.Frame.origin;
				Vec3 eyeGlobalPosition = hostileAgent3.OffenseInfo.Agent.GetEyeGlobalPosition();
				float collisionDistance;
				bool flag2 = isInStealthMode || !base.Mission.Scene.RayCastForClosestEntityOrTerrain(origin, eyeGlobalPosition, out collisionDistance, 0.035f);
				hostileAgent3.OffenseInfo.SetCanPlayerCameraSeeTheAgent(flag2);
				hostileAgent3.IsInVision = flag2;
				hostileAgent3.IsInVisibilityRange = SandBoxUIHelper.IsAgentInVisibilityRangeApproximate(Agent.Main, hostileAgent3.OffenseInfo.Agent);
				hostileAgent3.IsStealthModeEnabled = isInStealthMode;
				hostileAgent3.IsSuspicious = isSuspicious;
			}
			else
			{
				hostileAgent3.IsInVision = false;
				hostileAgent3.IsInVisibilityRange = false;
			}
			hostileAgent3.UpdatePosition();
			hostileAgent3.RefreshVisuals();
		}
	}
}
