using System.Collections.Generic;
using System.Linq;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class VisualTrackerMissionBehavior : MissionLogic
{
	public enum AgentTrackTypes
	{
		AvailableIssue,
		ActiveIssue,
		ActiveStoryQuest,
		TrackedIssue,
		TrackedStoryQuest
	}

	private List<TrackedObject> _currentTrackedObjects = new List<TrackedObject>();

	private int _trackedObjectsVersion = -1;

	private readonly VisualTrackerManager _visualTrackerManager = Campaign.Current.VisualTrackerManager;

	public override void OnAgentCreated(Agent agent)
	{
	}

	public override void AfterStart()
	{
		Refresh();
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (_visualTrackerManager.TrackedObjectsVersion != _trackedObjectsVersion)
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		if (PlayerEncounter.LocationEncounter != null)
		{
			RefreshCommonAreas();
		}
		_trackedObjectsVersion = _visualTrackerManager.TrackedObjectsVersion;
	}

	public void RegisterLocalOnlyObject(ITrackableBase obj)
	{
		foreach (TrackedObject currentTrackedObject in _currentTrackedObjects)
		{
			if (currentTrackedObject.Object == obj)
			{
				return;
			}
		}
		_currentTrackedObjects.Add(new TrackedObject(obj));
	}

	private void RefreshCommonAreas()
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		foreach (CommonAreaMarker item in base.Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>().ToList())
		{
			if (settlement.Alleys.Count >= item.AreaIndex)
			{
				RegisterLocalOnlyObject(item);
			}
		}
	}

	public override List<CompassItemUpdateParams> GetCompassTargets()
	{
		List<CompassItemUpdateParams> list = new List<CompassItemUpdateParams>();
		foreach (TrackedObject currentTrackedObject in _currentTrackedObjects)
		{
			list.Add(new CompassItemUpdateParams(currentTrackedObject.Object, TargetIconType.Flag_A, currentTrackedObject.Position, 4288256409u, uint.MaxValue));
		}
		return list;
	}

	private void RemoveLocalObject(ITrackableBase obj)
	{
		_currentTrackedObjects.RemoveAll((TrackedObject x) => x.Object == obj);
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		RemoveLocalObject(affectedAgent);
	}

	public override void OnAgentDeleted(Agent affectedAgent)
	{
		RemoveLocalObject(affectedAgent);
	}
}
