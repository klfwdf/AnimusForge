using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class MissionLogic : MissionBehavior
{
	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;

	public virtual InquiryData OnEndMissionRequest(out bool canLeave)
	{
		canLeave = true;
		return null;
	}

	public virtual bool MissionEnded(ref MissionResult missionResult)
	{
		return false;
	}

	public virtual void OnBattleEnded()
	{
	}

	public virtual void ShowBattleResults()
	{
	}

	public virtual void OnRetreatMission()
	{
	}

	public virtual void OnSurrenderMission()
	{
	}

	public virtual void OnAutoDeployTeam(Team team)
	{
	}

	public virtual List<EquipmentElement> GetExtraEquipmentElementsForCharacter(BasicCharacterObject character, bool getAllEquipments = false)
	{
		return null;
	}

	public virtual void OnMissionResultReady(MissionResult missionResult)
	{
	}
}
