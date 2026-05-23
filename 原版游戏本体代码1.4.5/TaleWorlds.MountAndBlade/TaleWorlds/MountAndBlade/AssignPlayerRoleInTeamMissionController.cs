using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade;

public class AssignPlayerRoleInTeamMissionController : MissionLogic
{
	protected readonly List<string> CharactersInPlayerSideByPriority;

	protected Queue<string> CharacterNamesInPlayerSideByPriorityQueue;

	protected List<Formation> RemainingFormationsToAssignSergeantsTo;

	protected Dictionary<int, Agent> FormationsLockedWithSergeants;

	protected Dictionary<int, Agent> FormationsWithLooselyChosenSergeants;

	public bool IsPlayerInArmy { get; }

	public bool IsPlayerGeneral { get; }

	public bool IsPlayerSergeant { get; }

	public int PlayerChosenIndex { get; protected set; }

	public event PlayerTurnToChooseFormationToLeadEvent OnPlayerTurnToChooseFormationToLead;

	public event AllFormationsAssignedSergeantsEvent OnAllFormationsAssignedSergeants;

	public AssignPlayerRoleInTeamMissionController(bool isPlayerGeneral, bool isPlayerSergeant, bool isPlayerInArmy, List<string> charactersInPlayerSideByPriority = null)
	{
		IsPlayerGeneral = isPlayerGeneral;
		IsPlayerSergeant = isPlayerSergeant;
		IsPlayerInArmy = isPlayerInArmy;
		PlayerChosenIndex = -1;
		CharactersInPlayerSideByPriority = charactersInPlayerSideByPriority;
	}

	public override void AfterStart()
	{
		Mission.Current.PlayerTeam.SetPlayerRole(IsPlayerGeneral, IsPlayerSergeant);
	}

	public override void OnTeamDeployed(Team team)
	{
		base.OnTeamDeployed(team);
		if (team != base.Mission.PlayerTeam)
		{
			return;
		}
		team.PlayerOrderController.Owner = Agent.Main;
		if (team.IsPlayerGeneral)
		{
			foreach (Formation item in team.FormationsIncludingEmpty)
			{
				item.PlayerOwner = Agent.Main;
			}
		}
		team.PlayerOrderController.SelectAllFormations();
	}

	public virtual void OnPlayerTeamDeployed()
	{
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
		}
		else
		{
			CharacterNamesInPlayerSideByPriorityQueue = ((CharactersInPlayerSideByPriority != null) ? new Queue<string>(CharactersInPlayerSideByPriority) : new Queue<string>());
			RemainingFormationsToAssignSergeantsTo = playerTeam.FormationsIncludingSpecialAndEmpty.WhereQ((Formation f) => f.CountOfUnits > 0).ToList();
			while (RemainingFormationsToAssignSergeantsTo.Count > 0 && CharacterNamesInPlayerSideByPriorityQueue.Count > 0)
			{
				string nextAgentNameToProcess = CharacterNamesInPlayerSideByPriorityQueue.Dequeue();
				Agent agent = playerTeam.ActiveAgents.FirstOrDefault((Agent aa) => aa.Character.StringId.Equals(nextAgentNameToProcess));
				if (agent != null)
				{
					if (agent == Agent.Main)
					{
						break;
					}
					Formation formation = ChooseFormationToLead(RemainingFormationsToAssignSergeantsTo, agent);
					if (formation != null)
					{
						FormationsLockedWithSergeants.Add(formation.Index, agent);
						RemainingFormationsToAssignSergeantsTo.Remove(formation);
					}
				}
			}
		}
		this.OnPlayerTurnToChooseFormationToLead?.Invoke(FormationsLockedWithSergeants, RemainingFormationsToAssignSergeantsTo.Select((Formation ftcsf) => ftcsf.Index).ToList());
	}

	public virtual void OnPlayerChoiceMade(int chosenIndex)
	{
		if (PlayerChosenIndex == chosenIndex)
		{
			return;
		}
		PlayerChosenIndex = chosenIndex;
		FormationsWithLooselyChosenSergeants.Clear();
		List<Formation> list = base.Mission.PlayerTeam.FormationsIncludingEmpty.WhereQ((Formation f) => f.CountOfUnits > 0 && !FormationsLockedWithSergeants.ContainsKey(f.Index)).ToList();
		if (chosenIndex != -1)
		{
			Formation item = list.FirstOrDefault((Formation fr) => fr.Index == chosenIndex);
			FormationsWithLooselyChosenSergeants.Add(chosenIndex, base.Mission.PlayerTeam.PlayerOrderController.Owner);
			list.Remove(item);
		}
		Queue<string> queue = new Queue<string>(CharacterNamesInPlayerSideByPriorityQueue);
		while (list.Count > 0 && queue.Count > 0)
		{
			string nextAgentNameToProcess = queue.Dequeue();
			Agent agent = base.Mission.PlayerTeam.ActiveAgents.FirstOrDefault((Agent aa) => aa.Character.StringId.Equals(nextAgentNameToProcess));
			if (agent != null)
			{
				Formation formation = ChooseFormationToLead(list, agent);
				if (formation != null)
				{
					FormationsWithLooselyChosenSergeants.Add(formation.Index, agent);
					list.Remove(formation);
				}
			}
		}
		if (this.OnAllFormationsAssignedSergeants != null)
		{
			this.OnAllFormationsAssignedSergeants(FormationsWithLooselyChosenSergeants);
		}
	}

	public void OnPlayerChoiceFinalized()
	{
		foreach (KeyValuePair<int, Agent> formationsLockedWithSergeant in FormationsLockedWithSergeants)
		{
			AssignSergeant(formationsLockedWithSergeant.Value.Team.GetFormation((FormationClass)formationsLockedWithSergeant.Key), formationsLockedWithSergeant.Value);
		}
		foreach (KeyValuePair<int, Agent> formationsWithLooselyChosenSergeant in FormationsWithLooselyChosenSergeants)
		{
			AssignSergeant(formationsWithLooselyChosenSergeant.Value.Team.GetFormation((FormationClass)formationsWithLooselyChosenSergeant.Key), formationsWithLooselyChosenSergeant.Value);
		}
	}

	protected virtual void AssignSergeant(Formation formationToLead, Agent sergeant)
	{
		sergeant.Formation = formationToLead;
		if (!sergeant.IsAIControlled || sergeant == Agent.Main)
		{
			formationToLead.PlayerOwner = sergeant;
		}
		formationToLead.Captain = sergeant;
	}

	private Formation ChooseFormationToLead(IEnumerable<Formation> formationsToChooseFrom, Agent agent)
	{
		bool hasMount = agent.HasMount;
		bool flag = agent.HasRangedWeapon();
		List<Formation> list = formationsToChooseFrom.ToList();
		while (list.Count > 0)
		{
			Formation formation = Extensions.MaxBy(list, (Formation ftcf) => ftcf.QuerySystem.FormationPower);
			list.Remove(formation);
			if ((flag || (!formation.QuerySystem.IsRangedFormation && !formation.QuerySystem.IsRangedCavalryFormation)) && (hasMount || (!formation.QuerySystem.IsCavalryFormation && !formation.QuerySystem.IsRangedCavalryFormation)))
			{
				return formation;
			}
		}
		return null;
	}
}
