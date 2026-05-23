using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class CampaignSiegeStateHandler : MissionLogic
{
	private readonly MapEvent _mapEvent;

	private bool _isRetreat;

	private bool _defenderVictory;

	public bool IsSiege => _mapEvent.IsSiegeAssault;

	public bool IsSallyOut => _mapEvent.IsSallyOut;

	public Settlement Settlement => _mapEvent.MapEventSettlement;

	public CampaignSiegeStateHandler()
	{
		_mapEvent = PlayerEncounter.Battle;
	}

	public override void OnRetreatMission()
	{
		_isRetreat = true;
	}

	public override void OnMissionResultReady(MissionResult missionResult)
	{
		_defenderVictory = missionResult.BattleState == BattleState.DefenderVictory;
	}

	public override void OnSurrenderMission()
	{
		PlayerEncounter.PlayerSurrender = true;
	}

	protected override void OnEndMission()
	{
		if (IsSiege && _mapEvent.PlayerSide == BattleSideEnum.Attacker && !_isRetreat && !_defenderVictory)
		{
			Settlement.SetNextSiegeState();
		}
	}
}
