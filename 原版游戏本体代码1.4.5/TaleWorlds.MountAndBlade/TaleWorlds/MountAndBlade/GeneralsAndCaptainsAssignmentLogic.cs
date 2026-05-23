using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class GeneralsAndCaptainsAssignmentLogic : MissionLogic
{
	public int MinimumAgentCountToLeadGeneralFormation = 3;

	private BannerBearerLogic _bannerLogic;

	private readonly TextObject _attackerGeneralName;

	private readonly TextObject _defenderGeneralName;

	private readonly TextObject _attackerAllyGeneralName;

	private readonly TextObject _defenderAllyGeneralName;

	private readonly bool _createBodyguard;

	private bool _isPlayerTeamGeneralFormationSet;

	public GeneralsAndCaptainsAssignmentLogic(TextObject attackerGeneralName, TextObject defenderGeneralName, TextObject attackerAllyGeneralName = null, TextObject defenderAllyGeneralName = null, bool createBodyguard = true)
	{
		_attackerGeneralName = attackerGeneralName;
		_defenderGeneralName = defenderGeneralName;
		_attackerAllyGeneralName = attackerAllyGeneralName;
		_defenderAllyGeneralName = defenderAllyGeneralName;
		_createBodyguard = createBodyguard;
		_isPlayerTeamGeneralFormationSet = false;
	}

	public override void AfterStart()
	{
		_bannerLogic = base.Mission.GetMissionBehavior<BannerBearerLogic>();
	}

	public override void OnTeamDeployed(Team team)
	{
		SetGeneralAgentOfTeam(team);
		if (team.IsPlayerTeam)
		{
			if (!MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
			{
				if (CanTeamHaveGeneralsFormation(team))
				{
					CreateGeneralFormationForTeam(team);
					_isPlayerTeamGeneralFormationSet = true;
				}
				AssignBestCaptainsForTeam(team);
			}
		}
		else
		{
			if (CanTeamHaveGeneralsFormation(team))
			{
				CreateGeneralFormationForTeam(team);
			}
			AssignBestCaptainsForTeam(team);
		}
	}

	public override void OnDeploymentFinished()
	{
		Team playerTeam = base.Mission.PlayerTeam;
		if (!_isPlayerTeamGeneralFormationSet && CanTeamHaveGeneralsFormation(playerTeam))
		{
			CreateGeneralFormationForTeam(playerTeam);
			_isPlayerTeamGeneralFormationSet = true;
		}
		Agent mainAgent;
		if (_isPlayerTeamGeneralFormationSet && (mainAgent = base.Mission.MainAgent) != null && playerTeam.GeneralAgent != mainAgent && !base.Mission.IsNavalBattle)
		{
			mainAgent.SetCanLeadFormationsRemotely(value: true);
			Formation formation = (mainAgent.Formation = playerTeam.GetFormation(FormationClass.NumberOfRegularFormations));
			mainAgent.Team.TriggerOnFormationsChanged(formation);
			formation.QuerySystem.Expire();
		}
	}

	protected virtual void SortCaptainsByPriority(Team team, ref List<Agent> captains)
	{
		captains = captains.OrderByDescending((Agent captain) => (team.GeneralAgent != captain) ? captain.Character.GetPower() : float.MaxValue).ToList();
	}

	protected virtual Formation PickBestRegularFormationToLead(Agent agent, List<Formation> candidateFormations)
	{
		Formation result = null;
		int num = 0;
		foreach (Formation candidateFormation in candidateFormations)
		{
			if (!(agent.HasMount ^ candidateFormation.CalculateHasSignificantNumberOfMounted))
			{
				int countOfUnits = candidateFormation.CountOfUnits;
				if (countOfUnits > num)
				{
					num = countOfUnits;
					result = candidateFormation;
				}
			}
		}
		return result;
	}

	private bool CanTeamHaveGeneralsFormation(Team team)
	{
		if (base.Mission.IsNavalBattle)
		{
			return false;
		}
		Agent generalAgent = team.GeneralAgent;
		if (generalAgent != null)
		{
			if (generalAgent != base.Mission.MainAgent)
			{
				return team.QuerySystem.MemberCount >= 50;
			}
			return true;
		}
		return false;
	}

	private void AssignBestCaptainsForTeam(Team team)
	{
		List<Agent> captains = team.ActiveAgents.Where((Agent agent) => agent.IsHero).ToList();
		SortCaptainsByPriority(team, ref captains);
		int numRegularFormations = 8;
		List<Formation> list = team.FormationsIncludingEmpty.WhereQ((Formation f) => f.CountOfUnits > 0 && (int)f.FormationIndex < numRegularFormations).ToList();
		List<Agent> list2 = new List<Agent>();
		foreach (Agent item in captains)
		{
			Formation formation = null;
			_ = MissionGameModels.Current.BattleBannerBearersModel;
			if (item == team.GeneralAgent && team.BodyGuardFormation != null && team.BodyGuardFormation.CountOfUnits > 0)
			{
				formation = team.BodyGuardFormation;
			}
			if (formation == null)
			{
				formation = PickBestRegularFormationToLead(item, list);
				if (formation != null)
				{
					list.Remove(formation);
				}
			}
			if (formation != null)
			{
				list2.Add(item);
				OnCaptainAssignedToFormation(item, formation);
			}
		}
		foreach (Agent item2 in list2)
		{
			captains.Remove(item2);
		}
		foreach (Agent candidate in captains)
		{
			if (list.IsEmpty())
			{
				break;
			}
			Formation formation2 = list.FirstOrDefault((Formation f) => f.CalculateHasSignificantNumberOfMounted == candidate.HasMount);
			if (formation2 != null)
			{
				OnCaptainAssignedToFormation(candidate, formation2);
				list.Remove(formation2);
			}
		}
	}

	private void SetGeneralAgentOfTeam(Team team)
	{
		Agent agent = null;
		if (team.IsPlayerTeam && team.IsPlayerGeneral)
		{
			agent = base.Mission.MainAgent;
		}
		else
		{
			List<IFormationUnit> source = team.FormationsIncludingEmpty.SelectMany((Formation f) => f.UnitsWithoutLooseDetachedOnes).ToList();
			TextObject generalName = ((team == base.Mission.AttackerTeam) ? _attackerGeneralName : ((team == base.Mission.DefenderTeam) ? _defenderGeneralName : ((team == base.Mission.AttackerAllyTeam) ? _attackerAllyGeneralName : ((team == base.Mission.DefenderAllyTeam) ? _defenderAllyGeneralName : null))));
			if (generalName != null && source.Count((IFormationUnit ta) => ((Agent)ta).Character != null && ((Agent)ta).Character.GetName().Equals(generalName)) == 1)
			{
				agent = (Agent)source.First((IFormationUnit ta) => ((Agent)ta).Character != null && ((Agent)ta).Character.GetName().Equals(generalName));
			}
			else if (source.Any((IFormationUnit u) => !((Agent)u).IsMainAgent && ((Agent)u).IsHero))
			{
				agent = (Agent)TaleWorlds.Core.Extensions.MaxBy(source.Where((IFormationUnit u) => !((Agent)u).IsMainAgent && ((Agent)u).IsHero), (IFormationUnit u) => ((Agent)u).CharacterPowerCached);
			}
		}
		if (agent != null && !base.Mission.IsNavalBattle)
		{
			agent.SetCanLeadFormationsRemotely(value: true);
		}
		team.GeneralAgent = agent;
	}

	private void CreateGeneralFormationForTeam(Team team)
	{
		Agent generalAgent = team.GeneralAgent;
		Formation formation = team.GetFormation(FormationClass.NumberOfRegularFormations);
		base.Mission.SetFormationPositioningFromDeploymentPlan(formation);
		WorldPosition position = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.GroundVec3);
		formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
		formation.SetControlledByAI(isControlledByAI: true);
		team.GeneralsFormation = formation;
		generalAgent.Formation = formation;
		generalAgent.Team.TriggerOnFormationsChanged(formation);
		formation.QuerySystem.Expire();
		TacticComponent.SetDefaultBehaviorWeights(formation);
		formation.AI.SetBehaviorWeight<BehaviorGeneral>(1f);
		formation.PlayerOwner = null;
		if (!_createBodyguard || generalAgent == base.Mission.MainAgent)
		{
			return;
		}
		List<IFormationUnit> list = team.FormationsIncludingEmpty.SelectMany((Formation f) => f.UnitsWithoutLooseDetachedOnes).ToList();
		list.Remove(generalAgent);
		List<IFormationUnit> list2 = list.Where((IFormationUnit u) => u is Agent agent && (agent.Character == null || !agent.Character.IsHero) && agent.Banner == null && ((generalAgent.MountAgent == null) ? (!agent.HasMount) : agent.HasMount)).ToList();
		int num = MathF.Min((int)((float)list2.Count / 10f), 20);
		if (num == 0)
		{
			return;
		}
		Formation formation2 = team.GetFormation(FormationClass.Bodyguard);
		formation2.SetMovementOrder(MovementOrder.MovementOrderMove(position));
		formation2.SetControlledByAI(isControlledByAI: true);
		List<IFormationUnit> list3 = list2.OrderByDescending((IFormationUnit u) => ((Agent)u).CharacterPowerCached).Take(num).ToList();
		IEnumerable<Formation> enumerable = list3.Select((IFormationUnit bu) => ((Agent)bu).Formation).Distinct();
		foreach (Agent item in list3)
		{
			item.Formation = formation2;
		}
		foreach (Formation item2 in enumerable)
		{
			team.TriggerOnFormationsChanged(item2);
			item2.QuerySystem.Expire();
		}
		TacticComponent.SetDefaultBehaviorWeights(formation2);
		formation2.AI.SetBehaviorWeight<BehaviorProtectGeneral>(1f);
		formation2.PlayerOwner = null;
		formation2.QuerySystem.Expire();
		team.BodyGuardFormation = formation2;
		team.TriggerOnFormationsChanged(formation2);
	}

	private void OnCaptainAssignedToFormation(Agent captain, Formation formation)
	{
		if (captain.Formation != formation && captain != formation.Team.GeneralAgent)
		{
			captain.Formation = formation;
			formation.Team.TriggerOnFormationsChanged(formation);
			formation.QuerySystem.Expire();
		}
		formation.Captain = captain;
		if (_bannerLogic != null && captain.FormationBanner != null)
		{
			_bannerLogic.SetFormationBanner(formation, captain.FormationBanner);
		}
	}
}
