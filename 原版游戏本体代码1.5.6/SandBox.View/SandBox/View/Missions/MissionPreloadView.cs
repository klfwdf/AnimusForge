using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class MissionPreloadView : MissionView
{
	private readonly PreloadHelper _helperInstance = new PreloadHelper();

	private bool _preloadDone;

	public override void OnPreMissionTick(float dt)
	{
		if (_preloadDone)
		{
			return;
		}
		List<BasicCharacterObject> list = new List<BasicCharacterObject>();
		foreach (PartyBase involvedParty in MapEvent.PlayerMapEvent.InvolvedParties)
		{
			foreach (TroopRosterElement item in involvedParty.MemberRoster.GetTroopRoster())
			{
				for (int i = 0; i < item.Number; i++)
				{
					list.Add(item.Character);
				}
			}
		}
		_helperInstance.PreloadCharacters(list);
		SiegeDeploymentMissionController missionBehavior = base.Mission.GetMissionBehavior<SiegeDeploymentMissionController>();
		if (missionBehavior != null)
		{
			_helperInstance.PreloadItems(missionBehavior.GetSiegeMissiles());
		}
		_preloadDone = true;
	}

	public override void OnSceneRenderingStarted()
	{
		_helperInstance.WaitForMeshesToBeLoaded();
	}

	public override void OnMissionStateDeactivated()
	{
		base.OnMissionStateDeactivated();
		_helperInstance.Clear();
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_helperInstance.Clear();
	}
}
