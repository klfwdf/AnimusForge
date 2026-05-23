using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class Team : IMissionTeam
{
	public readonly MBTeam MBTeam;

	private List<OrderController> _orderControllers;

	private MBList<Agent> _activeAgents;

	private MBList<Agent> _teamAgents;

	private bool _tickDetachments = true;

	private MBList<(float, WorldPosition, int, Vec2, Vec2, bool)> _cachedEnemyDataForFleeing;

	private bool _alreadyHasCustomOrderController;

	private static Team _invalid;

	public BattleSideEnum Side { get; }

	public Mission Mission { get; }

	public MBList<Formation> FormationsIncludingEmpty { get; private set; }

	public MBList<Formation> FormationsIncludingSpecialAndEmpty { get; private set; }

	public TeamAIComponent TeamAI { get; private set; }

	public bool IsPlayerTeam
	{
		get
		{
			if (Mission != null)
			{
				return Mission.PlayerTeam == this;
			}
			return false;
		}
	}

	public bool IsPlayerAlly
	{
		get
		{
			if (Mission != null && Mission.PlayerTeam != null)
			{
				return Mission.PlayerTeam.Side == Side;
			}
			return false;
		}
	}

	public TeamSideEnum TeamSide
	{
		get
		{
			if (!IsPlayerTeam)
			{
				if (!IsPlayerAlly)
				{
					return TeamSideEnum.EnemyTeam;
				}
				return TeamSideEnum.PlayerAllyTeam;
			}
			return TeamSideEnum.PlayerTeam;
		}
	}

	public bool IsDefender => Side == BattleSideEnum.Defender;

	public bool IsAttacker => Side == BattleSideEnum.Attacker;

	public uint Color { get; private set; }

	public uint Color2 { get; private set; }

	public Banner Banner { get; }

	public OrderController MasterOrderController => _orderControllers[0];

	public OrderController PlayerOrderController => _orderControllers[1];

	public TeamQuerySystem QuerySystem { get; private set; }

	public DetachmentManager DetachmentManager { get; private set; }

	public bool IsPlayerGeneral { get; private set; }

	public bool IsPlayerSergeant { get; private set; }

	public MBReadOnlyList<Agent> ActiveAgents => _activeAgents;

	public MBReadOnlyList<Agent> TeamAgents => _teamAgents;

	public MBReadOnlyList<(float, WorldPosition, int, Vec2, Vec2, bool)> CachedEnemyDataForFleeing => _cachedEnemyDataForFleeing;

	public int TeamIndex => MBTeam.Index;

	public float MoraleChangeFactor { get; private set; }

	public Formation GeneralsFormation { get; set; }

	public Formation BodyGuardFormation { get; set; }

	public Agent GeneralAgent { get; set; }

	public IEnumerable<Agent> Heroes
	{
		get
		{
			Agent main = Agent.Main;
			if (main != null && main.Team == this)
			{
				yield return main;
			}
		}
	}

	public bool HasBots
	{
		get
		{
			foreach (Agent activeAgent in ActiveAgents)
			{
				if (!activeAgent.IsMount && !activeAgent.IsPlayerControlled)
				{
					return true;
				}
			}
			return false;
		}
	}

	public Agent Leader
	{
		get
		{
			if (Agent.Main != null && Agent.Main.Team == this)
			{
				return Agent.Main;
			}
			Agent agent = null;
			foreach (Agent activeAgent in ActiveAgents)
			{
				if (agent == null || activeAgent.IsHero)
				{
					agent = activeAgent;
					if (agent.IsHero)
					{
						break;
					}
				}
			}
			return agent;
		}
	}

	public static Team Invalid
	{
		get
		{
			return _invalid ?? (_invalid = new Team(MBTeam.InvalidTeam, BattleSideEnum.None, null));
		}
		internal set
		{
			_invalid = value;
		}
	}

	public bool IsValid => MBTeam.IsValid;

	public bool HasTeamAi => TeamAI != null;

	public event Action<Team, Formation> OnFormationsChanged;

	public event OnOrderIssuedDelegate OnOrderIssued;

	public event Action<Formation> OnFormationAIActiveBehaviorChanged;

	public event Action<Team> OnFormationsChangedInDeployment;

	public Team(MBTeam mbTeam, BattleSideEnum side, Mission mission, uint color = uint.MaxValue, uint color2 = uint.MaxValue, Banner banner = null)
	{
		MBTeam = mbTeam;
		Side = side;
		Mission = mission;
		Color = color;
		Color2 = color2;
		Banner = banner;
		IsPlayerGeneral = true;
		IsPlayerSergeant = false;
		if (this != _invalid)
		{
			Initialize();
		}
		MoraleChangeFactor = 1f;
	}

	public void SetCustomOrderController(OrderController customMasterOrderController, OrderController customPlayerOrderController)
	{
		_alreadyHasCustomOrderController = true;
		OrderController orderController = ((_orderControllers.Count > 0) ? _orderControllers[0] : null);
		OrderController obj = ((_orderControllers.Count > 1) ? _orderControllers[1] : null);
		_orderControllers.Clear();
		_orderControllers.Add(customMasterOrderController);
		_orderControllers.Add(customPlayerOrderController);
		orderController?.AssignDelegatesToController(customMasterOrderController);
		obj?.AssignDelegatesToController(customPlayerOrderController);
	}

	public void UpdateCachedEnemyDataForFleeing()
	{
		if (!_cachedEnemyDataForFleeing.IsEmpty())
		{
			return;
		}
		foreach (Team team in Mission.Teams)
		{
			if (!team.IsEnemyOf(this))
			{
				continue;
			}
			foreach (Formation item4 in team.FormationsIncludingSpecialAndEmpty)
			{
				int countOfUnits = item4.CountOfUnits;
				if (countOfUnits > 0)
				{
					WorldPosition cachedMedianPosition = item4.CachedMedianPosition;
					float movementSpeedMaximum = item4.QuerySystem.MovementSpeedMaximum;
					bool item = (item4.QuerySystem.IsCavalryFormation || item4.QuerySystem.IsRangedCavalryFormation) && item4.HasAnyMountedUnit;
					if (countOfUnits == 1)
					{
						Vec2 asVec = cachedMedianPosition.AsVec2;
						_cachedEnemyDataForFleeing.Add((movementSpeedMaximum, cachedMedianPosition, countOfUnits, asVec, asVec, item));
						continue;
					}
					Vec2 vec = item4.QuerySystem.EstimatedDirection.LeftVec();
					float num = item4.Width / 2f;
					Vec2 item2 = cachedMedianPosition.AsVec2 - vec * num;
					Vec2 item3 = cachedMedianPosition.AsVec2 + vec * num;
					_cachedEnemyDataForFleeing.Add((movementSpeedMaximum, cachedMedianPosition, countOfUnits, item2, item3, item));
				}
			}
		}
	}

	private void Initialize()
	{
		_activeAgents = new MBList<Agent>();
		_teamAgents = new MBList<Agent>();
		_cachedEnemyDataForFleeing = new MBList<(float, WorldPosition, int, Vec2, Vec2, bool)>();
		if (GameNetwork.IsReplay)
		{
			return;
		}
		FormationsIncludingSpecialAndEmpty = new MBList<Formation>(10);
		FormationsIncludingEmpty = new MBList<Formation>(8);
		for (int i = 0; i < 10; i++)
		{
			Formation formation = new Formation(this, i);
			FormationsIncludingSpecialAndEmpty.Add(formation);
			if (i < 8)
			{
				FormationsIncludingEmpty.Add(formation);
			}
			formation.AI.OnActiveBehaviorChanged += FormationAI_OnActiveBehaviorChanged;
		}
		if (Mission != null)
		{
			_orderControllers = new List<OrderController>();
			OrderController orderController = new OrderController(Mission, this, null);
			_orderControllers.Add(orderController);
			orderController.OnOrderIssued += OrderController_OnOrderIssued;
			OrderController orderController2 = new OrderController(Mission, this, null);
			_orderControllers.Add(orderController2);
			orderController2.OnOrderIssued += OrderController_OnOrderIssued;
		}
		QuerySystem = new TeamQuerySystem(this);
		DetachmentManager = new DetachmentManager(this);
	}

	public void Reset()
	{
		if (!GameNetwork.IsReplay)
		{
			foreach (Formation item in FormationsIncludingSpecialAndEmpty)
			{
				item.Reset();
			}
			List<OrderController> orderControllers = _orderControllers;
			if (orderControllers != null && orderControllers.Count > 2)
			{
				for (int num = _orderControllers.Count - 1; num >= 2; num--)
				{
					_orderControllers[num].OnOrderIssued -= OrderController_OnOrderIssued;
					_orderControllers.RemoveAt(num);
				}
			}
			QuerySystem = new TeamQuerySystem(this);
		}
		_teamAgents.Clear();
		_activeAgents.Clear();
	}

	public void Clear()
	{
		if (!GameNetwork.IsReplay)
		{
			foreach (Formation item in FormationsIncludingSpecialAndEmpty)
			{
				item.AI.OnActiveBehaviorChanged -= FormationAI_OnActiveBehaviorChanged;
			}
		}
		Reset();
	}

	private void OrderController_OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
	{
		this.OnOrderIssued?.Invoke(orderType, appliedFormations, orderController, delegateParams);
	}

	public static bool DoesFirstFormationClassContainSecond(FormationClass f1, FormationClass f2)
	{
		return (f1 & f2) == f2;
	}

	public static FormationClass GetFormationFormationClass(Formation f)
	{
		if (!f.QuerySystem.IsRangedCavalryFormation)
		{
			if (!f.QuerySystem.IsCavalryFormation)
			{
				if (!f.QuerySystem.IsRangedFormation)
				{
					return FormationClass.Infantry;
				}
				return FormationClass.Ranged;
			}
			return FormationClass.Cavalry;
		}
		return FormationClass.HorseArcher;
	}

	public static FormationClass GetPlayerTeamFormationClass(Agent mainAgent)
	{
		if (mainAgent.IsRangedCached && mainAgent.HasMount)
		{
			return FormationClass.HorseArcher;
		}
		if (mainAgent.IsRangedCached)
		{
			return FormationClass.Ranged;
		}
		if (!mainAgent.HasMount)
		{
			return FormationClass.Infantry;
		}
		return FormationClass.Cavalry;
	}

	public void AssignPlayerAsSergeantOfFormation(MissionPeer peer, FormationClass formationClass)
	{
		Formation formation = GetFormation(formationClass);
		formation.PlayerOwner = peer.ControlledAgent;
		formation.BannerCode = peer.Peer.BannerCode;
		if (peer.IsMine)
		{
			PlayerOrderController.Owner = peer.ControlledAgent;
		}
		else
		{
			GetOrderControllerOf(peer.ControlledAgent).Owner = peer.ControlledAgent;
		}
		formation.SetControlledByAI(isControlledByAI: false);
		foreach (MissionBehavior missionBehavior in Mission.MissionBehaviors)
		{
			missionBehavior.OnAssignPlayerAsSergeantOfFormation(peer.ControlledAgent);
		}
		if (peer.IsMine)
		{
			PlayerOrderController.SelectAllFormations();
		}
		peer.ControlledFormation = formation;
		if (GameNetwork.IsServer)
		{
			peer.ControlledAgent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
			if (!peer.IsMine)
			{
				GameNetwork.BeginModuleEventAsServer(peer.GetNetworkPeer());
				GameNetwork.WriteMessage(new AssignFormationToPlayer(peer.GetNetworkPeer(), formationClass));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	private void FormationAI_OnActiveBehaviorChanged(Formation formation)
	{
		if (formation.CountOfUnits > 0)
		{
			this.OnFormationAIActiveBehaviorChanged?.Invoke(formation);
		}
	}

	public void AddTacticOption(TacticComponent tacticOption)
	{
		if (HasTeamAi)
		{
			TeamAI.AddTacticOption(tacticOption);
		}
	}

	public void RemoveTacticOption(Type tacticType)
	{
		if (HasTeamAi)
		{
			TeamAI.RemoveTacticOption(tacticType);
		}
	}

	public void ClearTacticOptions()
	{
		if (HasTeamAi)
		{
			TeamAI.ClearTacticOptions();
		}
	}

	public void ResetTactic()
	{
		if (HasTeamAi)
		{
			TeamAI.ResetTactic();
		}
	}

	public void AddTeamAI(TeamAIComponent teamAI, bool forceNotAIControlled = false)
	{
		TeamAI = teamAI;
		foreach (Formation item in FormationsIncludingSpecialAndEmpty)
		{
			item.SetControlledByAI(!forceNotAIControlled && (this != Mission.PlayerTeam || !IsPlayerGeneral));
		}
		TeamAI.InitializeDetachments(Mission);
		TeamAI.CreateMissionSpecificBehaviors();
		TeamAI.ResetTactic();
		foreach (Formation item2 in FormationsIncludingSpecialAndEmpty)
		{
			if (item2.CountOfUnits > 0)
			{
				item2.AI.Tick();
			}
		}
		TeamAI.TickOccasionally();
	}

	public void DelegateCommandToAI()
	{
		foreach (Formation item in FormationsIncludingEmpty)
		{
			item.SetControlledByAI(isControlledByAI: true);
		}
	}

	public void RearrangeFormationsAccordingToFilter(List<(Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents)> MassTransferData)
	{
		List<Formation> list = new List<Formation>();
		foreach (var MassTransferDatum in MassTransferData)
		{
			MassTransferDatum.formation.OnMassUnitTransferStart();
			if (MassTransferDatum.formation.GetReadonlyMovementOrderReference() == MovementOrder.MovementOrderStop && (MassTransferDatum.formation.CountOfUnits > 0 || MassTransferDatum.troopCount > 0))
			{
				list.Add(MassTransferDatum.formation);
				MassTransferDatum.formation.SetMovementOrder(MovementOrder.MovementOrderMove(MassTransferDatum.formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			}
		}
		List<Agent>[] array = new List<Agent>[MassTransferData.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new List<Agent>();
		}
		List<FormationPocket> list2 = new List<FormationPocket>();
		for (int j = 0; j < MassTransferData.Count; j++)
		{
			TroopTraitsMask item = MassTransferData[j].troopFilter;
			TroopFilteringUtilities.GetPriorityFunction(item, out Func<Agent, int> priorityFunc);
			int maxPriority = TroopFilteringUtilities.GetMaxPriority(item);
			list2.Add(new FormationPocket(priorityFunc, maxPriority, MassTransferData[j].troopCount, j));
		}
		list2.RemoveAll((FormationPocket pfamv) => pfamv.TroopCount <= 0);
		list2 = list2.OrderBy((FormationPocket pfamv) => pfamv.TroopCount).ToList();
		list2 = list2.OrderByDescending((FormationPocket pfamv) => pfamv.ScoreToSeek).ToList();
		List<IFormationUnit> list3 = new List<IFormationUnit>();
		list3 = MassTransferData.SelectMany(((Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents) mtd) => mtd.formation.DetachedUnits.Concat(mtd.formation.Arrangement.GetAllUnits()).Except(mtd.excludedAgents)).ToList();
		int num = MassTransferData.Sum(((Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents) mtd) => mtd.excludedAgents.Count);
		int num2 = MassTransferData.Sum(((Formation formation, int troopCount, TroopTraitsMask troopFilter, List<Agent> excludedAgents) mtd) => mtd.formation.CountOfUnits) - num;
		int scoreToSeek = list2[0].ScoreToSeek;
		while (num2 > 0)
		{
			for (int num3 = 0; num3 < num2; num3++)
			{
				Agent agent = list3[num3] as Agent;
				for (int num4 = 0; num4 < list2.Count; num4++)
				{
					FormationPocket formationPocket = list2[num4];
					int num5 = formationPocket.PriorityFunction(agent);
					if (scoreToSeek <= formationPocket.ScoreToSeek && num5 >= scoreToSeek)
					{
						array[formationPocket.Index].Add(agent);
						formationPocket.AddTroop();
						if (formationPocket.IsFormationPocketFilled())
						{
							list2.RemoveAt(num4);
						}
						num2--;
						list3[num3] = list3[num2];
						num3--;
						break;
					}
					if (num5 > formationPocket.BestScoreSoFar)
					{
						formationPocket.SetBestScoreSoFar(num5);
					}
				}
			}
			if (list2.Count == 0)
			{
				break;
			}
			for (int num6 = 0; num6 < list2.Count; num6++)
			{
				list2[num6].UpdateScoreToSeek();
			}
			list2.OrderByDescending((FormationPocket pfamv) => pfamv.ScoreToSeek);
			scoreToSeek = list2[0].ScoreToSeek;
		}
		for (int num7 = 0; num7 < array.Length; num7++)
		{
			foreach (Agent item2 in array[num7])
			{
				item2.Formation = MassTransferData[num7].formation;
			}
		}
		foreach (var MassTransferDatum2 in MassTransferData)
		{
			TriggerOnFormationsChanged(MassTransferDatum2.formation);
			MassTransferDatum2.formation.OnMassUnitTransferEnd();
			if (MassTransferDatum2.formation.CountOfUnits > 0 && !MassTransferDatum2.formation.OrderPositionIsValid)
			{
				Vec2 averagePositionOfUnits = MassTransferDatum2.formation.GetAveragePositionOfUnits(excludeDetachedUnits: false, excludePlayer: false);
				float height = Mission.Scene.GetTerrainHeight(averagePositionOfUnits);
				Mission.Scene.GetHeightAtPoint(averagePositionOfUnits, BodyFlags.None, ref height);
				WorldPosition value = new WorldPosition(position: new Vec3(averagePositionOfUnits, height), scene: Mission.Scene, navMesh: UIntPtr.Zero, hasValidZ: false);
				MassTransferDatum2.formation.SetPositioning(value);
			}
		}
		foreach (Formation item3 in list)
		{
			item3.SetMovementOrder(MovementOrder.MovementOrderStop);
		}
	}

	public void Tick(float dt)
	{
		if (!_cachedEnemyDataForFleeing.IsEmpty())
		{
			_cachedEnemyDataForFleeing.Clear();
		}
		if (Mission.AllowAiTicking)
		{
			if (Mission.RetreatSide != BattleSideEnum.None && Side == Mission.RetreatSide)
			{
				foreach (Formation item in FormationsIncludingSpecialAndEmpty)
				{
					if (item.CountOfUnits > 0)
					{
						item.SetMovementOrder(MovementOrder.MovementOrderRetreat);
					}
				}
			}
			else if (TeamAI != null && HasBots)
			{
				TeamAI.Tick(dt);
			}
		}
		if (GameNetwork.IsReplay)
		{
			return;
		}
		if (_tickDetachments)
		{
			DetachmentManager.TickDetachments();
		}
		foreach (Formation item2 in FormationsIncludingSpecialAndEmpty)
		{
			if (item2.CountOfUnits > 0)
			{
				item2.Tick(dt);
			}
		}
	}

	public Formation GetFormation(FormationClass formationIndex)
	{
		return FormationsIncludingSpecialAndEmpty[(int)formationIndex];
	}

	public void SetIsEnemyOf(Team otherTeam, bool isEnemyOf)
	{
		MBTeam.SetIsEnemyOf(otherTeam.MBTeam, isEnemyOf);
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new TeamSetIsEnemyOf(TeamIndex, otherTeam.TeamIndex, isEnemyOf));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
	}

	public bool IsEnemyOf(Team otherTeam)
	{
		return MBTeam.IsEnemyOf(otherTeam.MBTeam);
	}

	public bool IsFriendOf(Team otherTeam)
	{
		if (this != otherTeam)
		{
			return !MBTeam.IsEnemyOf(otherTeam.MBTeam);
		}
		return true;
	}

	public void AddAgentToTeam(Agent unit)
	{
		_teamAgents.Add(unit);
		_activeAgents.Add(unit);
	}

	public void RemoveAgentFromTeam(Agent unit)
	{
		_teamAgents.Remove(unit);
		_activeAgents.Remove(unit);
	}

	public void DeactivateAgent(Agent agent)
	{
		_activeAgents.Remove(agent);
	}

	public void OnAgentRemoved(Agent agent)
	{
		if (GameNetwork.IsClientOrReplay)
		{
			return;
		}
		foreach (Formation item in FormationsIncludingSpecialAndEmpty)
		{
			item.AI.OnAgentRemoved(agent);
		}
	}

	public override string ToString()
	{
		return MBTeam.ToString();
	}

	public void OnMissionEnded()
	{
		if (HasTeamAi)
		{
			TeamAI.OnMissionEnded();
		}
	}

	public void TriggerOnFormationsChanged(Formation formation)
	{
		this.OnFormationsChanged?.Invoke(this, formation);
	}

	public void TriggerOnFormationsChangedInDeployment()
	{
		this.OnFormationsChangedInDeployment?.Invoke(this);
	}

	public OrderController GetOrderControllerOf(Agent agent)
	{
		OrderController orderController = _orderControllers.FirstOrDefault((OrderController oc) => oc.Owner == agent);
		if (orderController == null)
		{
			orderController = new OrderController(Mission, this, agent);
			_orderControllers.Add(orderController);
			orderController.OnOrderIssued += OrderController_OnOrderIssued;
		}
		return orderController;
	}

	public void SetPlayerRole(bool isPlayerGeneral, bool isPlayerSergeant)
	{
		IsPlayerGeneral = isPlayerGeneral;
		IsPlayerSergeant = isPlayerSergeant;
		foreach (Formation item in FormationsIncludingSpecialAndEmpty)
		{
			item.SetControlledByAI(this != Mission.PlayerTeam || !IsPlayerGeneral);
		}
	}

	public bool HasAnyEnemyTeamsWithAgents(bool ignoreMountedAgents)
	{
		foreach (Team team in Mission.Teams)
		{
			if (team == this || !team.IsEnemyOf(this) || team.ActiveAgents.Count <= 0)
			{
				continue;
			}
			if (ignoreMountedAgents)
			{
				foreach (Agent activeAgent in team.ActiveAgents)
				{
					if (!activeAgent.HasMount)
					{
						return true;
					}
				}
				continue;
			}
			return true;
		}
		return false;
	}

	public bool HasAnyFormationsIncludingSpecialThatIsNotEmpty()
	{
		foreach (Formation item in FormationsIncludingSpecialAndEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				return true;
			}
		}
		return false;
	}

	public int GetFormationCount()
	{
		int num = 0;
		foreach (Formation item in FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				num++;
			}
		}
		return num;
	}

	public int GetAIControlledFormationCount()
	{
		int num = 0;
		foreach (Formation item in FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0 && item.IsAIControlled)
			{
				num++;
			}
		}
		return num;
	}

	public Vec2 GetAveragePositionOfEnemies()
	{
		Vec2 zero = Vec2.Zero;
		int num = 0;
		foreach (Team team in Mission.Teams)
		{
			if (!team.MBTeam.IsValid || !IsEnemyOf(team))
			{
				continue;
			}
			foreach (Agent activeAgent in team.ActiveAgents)
			{
				zero += activeAgent.Position.AsVec2;
				num++;
			}
		}
		if (num > 0)
		{
			return zero * (1f / (float)num);
		}
		return Vec2.Invalid;
	}

	public Vec2 GetAveragePosition()
	{
		Vec2 zero = Vec2.Zero;
		MBReadOnlyList<Agent> activeAgents = ActiveAgents;
		int num = 0;
		foreach (Agent item in activeAgents)
		{
			zero += item.Position.AsVec2;
			num++;
		}
		if (num > 0)
		{
			return zero * (1f / (float)num);
		}
		return Vec2.Invalid;
	}

	public WorldPosition GetMedianPosition(Vec2 averagePosition)
	{
		float num = float.MaxValue;
		Agent agent = null;
		foreach (Agent activeAgent in ActiveAgents)
		{
			float num2 = activeAgent.Position.AsVec2.DistanceSquared(averagePosition);
			if (num2 <= num)
			{
				agent = activeAgent;
				num = num2;
			}
		}
		return agent?.GetWorldPosition() ?? WorldPosition.Invalid;
	}

	public Vec2 GetWeightedAverageOfEnemies(Vec2 basePoint)
	{
		Vec2 zero = Vec2.Zero;
		float num = 0f;
		foreach (Team team in Mission.Teams)
		{
			if (!team.MBTeam.IsValid || !IsEnemyOf(team))
			{
				continue;
			}
			foreach (Agent activeAgent in team.ActiveAgents)
			{
				Vec2 asVec = activeAgent.Position.AsVec2;
				float lengthSquared = (basePoint - asVec).LengthSquared;
				float num2 = 1f / lengthSquared;
				zero += asVec * num2;
				num += num2;
			}
		}
		if (num > 0f)
		{
			return zero * (1f / num);
		}
		return Vec2.Invalid;
	}

	public void DisableDetachmentTicking()
	{
		_tickDetachments = false;
	}

	[Conditional("DEBUG")]
	private void TickStandingPointDebug()
	{
	}
}
