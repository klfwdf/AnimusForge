using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions;

public class NavalAssignPlayerRoleInTeamMissionController : AssignPlayerRoleInTeamMissionController
{
	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	public NavalAssignPlayerRoleInTeamMissionController(bool isPlayerGeneral, bool isPlayerSergeant, bool isPlayerInArmy, List<string> charactersInPlayerSideByPriority = null)
		: base(isPlayerGeneral, isPlayerSergeant, isPlayerInArmy, charactersInPlayerSideByPriority)
	{
	}

	public override void OnPlayerChoiceMade(int chosenIndex)
	{
		Debug.FailedAssert("Player cannot make a choice in naval battles as its decision is fixed by design", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Missions\\NavalAssignPlayerRoleInTeamMissionController.cs", "OnPlayerChoiceMade", 24);
	}

	public override void OnPlayerTeamDeployed()
	{
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		base.PlayerChosenIndex = 0;
		if (!MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
		{
			return;
		}
		Team playerTeam = Mission.Current.PlayerTeam;
		FormationsLockedWithSergeants = new Dictionary<int, Agent>();
		FormationsWithLooselyChosenSergeants = new Dictionary<int, Agent>();
		if (playerTeam.IsPlayerGeneral)
		{
			CharacterNamesInPlayerSideByPriorityQueue = new Queue<string>();
			RemainingFormationsToAssignSergeantsTo = new List<Formation>();
			return;
		}
		CharacterNamesInPlayerSideByPriorityQueue = ((CharactersInPlayerSideByPriority != null) ? new Queue<string>(CharactersInPlayerSideByPriority) : new Queue<string>());
		RemainingFormationsToAssignSergeantsTo = playerTeam.FormationsIncludingSpecialAndEmpty.WhereQ((Formation f) => f.CountOfUnits > 0).ToList();
		while (CharacterNamesInPlayerSideByPriorityQueue.Count > 0 && RemainingFormationsToAssignSergeantsTo.Count > 0)
		{
			string nextAgentNameToProcess = CharacterNamesInPlayerSideByPriorityQueue.Dequeue();
			Agent agent = playerTeam.ActiveAgents.FirstOrDefault((Agent aa) => aa.Character.StringId.Equals(nextAgentNameToProcess));
			if (agent != null)
			{
				Formation formation = RemainingFormationsToAssignSergeantsTo[0];
				FormationsLockedWithSergeants.Add(formation.Index, agent);
				RemainingFormationsToAssignSergeantsTo.Remove(formation);
			}
		}
	}

	protected override void AssignSergeant(Formation formationToLead, Agent sergeant)
	{
		_navalShipsLogic.GetShip(formationToLead, out var ship);
		if (formationToLead.Captain != sergeant)
		{
			_navalAgentsLogic.AssignCaptainToShipForDeploymentMode(sergeant, ship);
		}
		if (!sergeant.IsAIControlled || sergeant == Agent.Main)
		{
			formationToLead.PlayerOwner = sergeant;
		}
	}
}
