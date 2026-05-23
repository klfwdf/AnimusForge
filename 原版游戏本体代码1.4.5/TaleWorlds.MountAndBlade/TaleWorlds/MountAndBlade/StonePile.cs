using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public class StonePile : UsableMachine, IDetachment
{
	[DefineSynchedMissionObjectType(typeof(StonePile))]
	public struct StonePileRecord : ISynchedMissionObjectReadableRecord
	{
		public int ReadAmmoCount { get; private set; }

		public StonePileRecord(int readAmmoCount)
		{
			ReadAmmoCount = readAmmoCount;
		}

		public bool ReadFromNetwork(ref bool bufferReadValid)
		{
			ReadAmmoCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.RangedSiegeWeaponAmmoCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}
	}

	private class ThrowingPoint
	{
		public struct StackArray8ThrowingPoint
		{
			private ThrowingPoint _element0;

			private ThrowingPoint _element1;

			private ThrowingPoint _element2;

			private ThrowingPoint _element3;

			private ThrowingPoint _element4;

			private ThrowingPoint _element5;

			private ThrowingPoint _element6;

			private ThrowingPoint _element7;

			public const int Length = 8;

			public ThrowingPoint this[int index]
			{
				get
				{
					return index switch
					{
						0 => _element0, 
						1 => _element1, 
						2 => _element2, 
						3 => _element3, 
						4 => _element4, 
						5 => _element5, 
						6 => _element6, 
						7 => _element7, 
						_ => null, 
					};
				}
				set
				{
					switch (index)
					{
					case 0:
						_element0 = value;
						break;
					case 1:
						_element1 = value;
						break;
					case 2:
						_element2 = value;
						break;
					case 3:
						_element3 = value;
						break;
					case 4:
						_element4 = value;
						break;
					case 5:
						_element5 = value;
						break;
					case 6:
						_element6 = value;
						break;
					case 7:
						_element7 = value;
						break;
					}
				}
			}
		}

		private const float CachedCanUseAttackEntityUpdateInterval = 1f;

		public StandingPointWithVolumeBox StandingPoint;

		public StandingPointWithWeaponRequirement AmmoPickUpPoint;

		public StandingPointWithWeaponRequirement WaitingPoint;

		public Timer EnemyInRangeTimer;

		public GameEntity AttackEntity;

		public float AttackEntityNearbyAgentsCheckRadius;

		private float _cachedCanUseAttackEntityExpireTime;

		private bool _cachedCanUseAttackEntity;

		public bool CanUseAttackEntity()
		{
			bool result = true;
			if (AttackEntityNearbyAgentsCheckRadius > 0f)
			{
				float currentTime = Mission.Current.CurrentTime;
				if (currentTime >= _cachedCanUseAttackEntityExpireTime)
				{
					_cachedCanUseAttackEntity = Mission.Current.HasAnyAgentsOfSideInRange(AttackEntity.GlobalPosition, AttackEntityNearbyAgentsCheckRadius, BattleSideEnum.Attacker);
					_cachedCanUseAttackEntityExpireTime = currentTime + 1f;
				}
				result = _cachedCanUseAttackEntity;
			}
			return result;
		}
	}

	private struct VolumeBoxTimerPair
	{
		public VolumeBox VolumeBox;

		public Timer Timer;
	}

	private const string ThrowingTargetTag = "throwing_target";

	private const string ThrowingPointTag = "throwing";

	private const string WaitingPointTag = "wait_to_throw";

	private const float EnemyInRangeTimerDuration = 0.5f;

	private const float EnemyWaitTimeLimit = 3f;

	private const float ThrowingTargetRadius = 1.31f;

	public int StartingAmmoCount = 12;

	public string GivenItemID = "boulder";

	[EditableScriptComponentVariable(true, "")]
	private float _givenItemRange = 15f;

	private ItemObject _givenItem;

	private List<GameEntity> _throwingTargets;

	private List<ThrowingPoint> _throwingPoints;

	private List<VolumeBoxTimerPair> _volumeBoxTimerPairs;

	private Timer _tickOccasionallyTimer;

	public int AmmoCount { get; protected set; }

	public bool HasThrowingPointUsed
	{
		get
		{
			foreach (ThrowingPoint throwingPoint in _throwingPoints)
			{
				if (throwingPoint.StandingPoint.HasUser || throwingPoint.StandingPoint.HasAIMovingTo || (throwingPoint.WaitingPoint != null && (throwingPoint.WaitingPoint.HasUser || throwingPoint.WaitingPoint.HasAIMovingTo)))
				{
					return true;
				}
			}
			return false;
		}
	}

	public virtual BattleSideEnum Side => BattleSideEnum.Defender;

	public override int MaxUserCount => _throwingPoints.Count;

	protected StonePile()
	{
	}

	protected void ConsumeAmmo()
	{
		AmmoCount--;
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetStonePileAmmo(base.Id, AmmoCount));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		UpdateAmmoMesh();
		CheckAmmo();
	}

	public void SetAmmo(int ammoLeft)
	{
		if (AmmoCount != ammoLeft)
		{
			AmmoCount = ammoLeft;
			UpdateAmmoMesh();
			CheckAmmo();
		}
	}

	protected virtual void CheckAmmo()
	{
		if (AmmoCount > 0)
		{
			return;
		}
		foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
		{
			ammoPickUpPoint.IsDeactivated = true;
		}
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		_tickOccasionallyTimer = new Timer(Mission.Current.CurrentTime, 0.5f + MBRandom.RandomFloat * 0.5f);
		_givenItem = Game.Current.ObjectManager.GetObject<ItemObject>(GivenItemID);
		MBList<VolumeBox> source = base.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<VolumeBox>();
		_throwingPoints = new List<ThrowingPoint>();
		_volumeBoxTimerPairs = new List<VolumeBoxTimerPair>();
		foreach (StandingPointWithWeaponRequirement item2 in base.StandingPoints.OfType<StandingPointWithWeaponRequirement>())
		{
			if (item2.GameEntity.HasTag(AmmoPickUpTag))
			{
				item2.InitGivenWeapon(_givenItem);
				item2.SetHasAlternative(hasAlternative: true);
				item2.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
			}
			else
			{
				if (!item2.GameEntity.HasTag("throwing"))
				{
					continue;
				}
				item2.InitRequiredWeapon(_givenItem);
				ThrowingPoint throwingPoint = new ThrowingPoint();
				throwingPoint.StandingPoint = item2 as StandingPointWithVolumeBox;
				throwingPoint.AmmoPickUpPoint = null;
				throwingPoint.AttackEntity = null;
				throwingPoint.AttackEntityNearbyAgentsCheckRadius = 0f;
				List<StandingPointWithWeaponRequirement> list = item2.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<StandingPointWithWeaponRequirement>("wait_to_throw");
				if (list != null && list.Count > 0)
				{
					throwingPoint.WaitingPoint = list[0];
					throwingPoint.WaitingPoint.InitRequiredWeapon(_givenItem);
				}
				else
				{
					throwingPoint.WaitingPoint = null;
				}
				bool flag = false;
				for (int i = 0; i < _volumeBoxTimerPairs.Count; i++)
				{
					if (flag)
					{
						break;
					}
					if (_volumeBoxTimerPairs[i].VolumeBox.GameEntity.HasTag(throwingPoint.StandingPoint.VolumeBoxTag))
					{
						throwingPoint.EnemyInRangeTimer = _volumeBoxTimerPairs[i].Timer;
						flag = true;
					}
				}
				if (!flag)
				{
					VolumeBox volumeBox = source.FirstOrDefault((VolumeBox vb) => vb.GameEntity.HasTag(throwingPoint.StandingPoint.VolumeBoxTag));
					VolumeBoxTimerPair item = new VolumeBoxTimerPair
					{
						VolumeBox = volumeBox,
						Timer = new Timer(-3.5f, 0.5f, autoReset: false)
					};
					throwingPoint.EnemyInRangeTimer = item.Timer;
					_volumeBoxTimerPairs.Add(item);
				}
				_throwingPoints.Add(throwingPoint);
			}
		}
		EnemyRangeToStopUsing = 5f;
		AmmoCount = StartingAmmoCount;
		UpdateAmmoMesh();
		SetScriptComponentToTick(GetTickRequirement());
		_throwingTargets = base.Scene.FindEntitiesWithTag("throwing_target").ToList();
	}

	protected internal override void OnMissionReset()
	{
		base.OnMissionReset();
		AmmoCount = StartingAmmoCount;
		UpdateAmmoMesh();
		foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
		{
			ammoPickUpPoint.IsDeactivated = false;
		}
		foreach (VolumeBoxTimerPair volumeBoxTimerPair in _volumeBoxTimerPairs)
		{
			volumeBoxTimerPair.Timer.Reset(-3.5f);
		}
		foreach (ThrowingPoint throwingPoint in _throwingPoints)
		{
			throwingPoint.AmmoPickUpPoint = null;
		}
	}

	public override void AfterMissionStart()
	{
		if (base.AmmoPickUpPoints != null)
		{
			foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
			{
				ammoPickUpPoint.LockUserFrames = true;
			}
		}
		if (_throwingPoints == null)
		{
			return;
		}
		foreach (ThrowingPoint throwingPoint in _throwingPoints)
		{
			throwingPoint.StandingPoint.IsDisabledForPlayers = true;
			throwingPoint.StandingPoint.LockUserFrames = false;
			throwingPoint.StandingPoint.LockUserPositions = true;
		}
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
		{
			TextObject textObject = new TextObject("{=jfcceEoE}{PILE_TYPE} Pile");
			textObject.SetTextVariable("PILE_TYPE", new TextObject("{=1CPdu9K0}Stone"));
			return textObject;
		}
		return null;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		if (gameEntity.IsValid && gameEntity.HasTag(AmmoPickUpTag))
		{
			TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
			textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			return textObject;
		}
		return null;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new StonePileAI(this);
	}

	public override bool IsInRangeToCheckAlternativePoints(Agent agent)
	{
		float num = ((base.StandingPoints.Count > 0) ? (agent.GetInteractionDistanceToUsable(base.StandingPoints[0]) + 2f) : 2f);
		return base.GameEntity.GlobalPosition.DistanceSquared(agent.Position) < num * num;
	}

	public override StandingPoint GetBestPointAlternativeTo(StandingPoint standingPoint, Agent agent)
	{
		if (base.AmmoPickUpPoints.Contains(standingPoint))
		{
			float num = standingPoint.GameEntity.GlobalPosition.DistanceSquared(agent.Position);
			StandingPoint result = standingPoint;
			{
				foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
				{
					float num2 = ammoPickUpPoint.GameEntity.GlobalPosition.DistanceSquared(agent.Position);
					if (num2 < num && ((!ammoPickUpPoint.HasUser && !ammoPickUpPoint.HasAIMovingTo) || ammoPickUpPoint.IsInstantUse) && !ammoPickUpPoint.IsDeactivated && !ammoPickUpPoint.IsDisabledForAgent(agent))
					{
						num = num2;
						result = ammoPickUpPoint;
					}
				}
				return result;
			}
		}
		return standingPoint;
	}

	private void TickOccasionally()
	{
		if (GameNetwork.IsClientOrReplay)
		{
			return;
		}
		if (AmmoCount <= 0 && !HasThrowingPointUsed)
		{
			ReleaseAllUserAgentsAndFormations(BattleSideEnum.None, disableForNonAIControlledAgents: true);
			return;
		}
		if (IsDisabledForBattleSideAI(Side))
		{
			ReleaseAllUserAgentsAndFormations(Side, disableForNonAIControlledAgents: false);
			return;
		}
		bool flag = _volumeBoxTimerPairs.Count == 0;
		foreach (VolumeBoxTimerPair volumeBoxTimerPair in _volumeBoxTimerPairs)
		{
			if (volumeBoxTimerPair.VolumeBox.HasAgentsInAttackerSide())
			{
				flag = true;
				if (volumeBoxTimerPair.Timer.ElapsedTime() > 3.5f)
				{
					volumeBoxTimerPair.Timer.Reset(Mission.Current.CurrentTime);
				}
				else
				{
					volumeBoxTimerPair.Timer.Reset(Mission.Current.CurrentTime - 0.5f);
				}
			}
		}
		MBReadOnlyList<Formation> userFormations = base.UserFormations;
		if (flag && userFormations.CountQ((Formation f) => f.Team.Side == Side) == 0)
		{
			float minDistanceSquared = float.MaxValue;
			Formation bestFormation = null;
			foreach (Team team in Mission.Current.Teams)
			{
				if (team.Side != Side)
				{
					continue;
				}
				foreach (Formation formation in team.FormationsIncludingEmpty)
				{
					if (formation.CountOfUnits <= 0 || formation.CountOfUnitsWithoutLooseDetachedOnes < MaxUserCount || formation.CountOfUnitsWithoutLooseDetachedOnes <= 0)
					{
						continue;
					}
					formation.ApplyActionOnEachUnit(delegate(Agent agent)
					{
						float num = agent.Position.DistanceSquared(base.GameEntity.GlobalPosition);
						if (minDistanceSquared > num)
						{
							minDistanceSquared = num;
							bestFormation = formation;
						}
					});
				}
			}
			bestFormation?.StartUsingMachine(this);
		}
		else if (!flag)
		{
			if (userFormations.Count > 0)
			{
				ReleaseAllUserAgentsAndFormations(BattleSideEnum.None, disableForNonAIControlledAgents: true);
			}
		}
		else if (userFormations.All((Formation f) => f.Team.Side == Side && f.UnitsWithoutLooseDetachedOnes.Count == 0) && base.StandingPoints.Count((StandingPoint sp) => sp.HasUser || sp.HasAIMovingTo) == 0)
		{
			ReleaseAllUserAgentsAndFormations(BattleSideEnum.None, disableForNonAIControlledAgents: true);
		}
		else
		{
			UpdateThrowingPointAttackEntities();
		}
	}

	private void ReleaseAllUserAgentsAndFormations(BattleSideEnum sideFilterForAIControlledAgents, bool disableForNonAIControlledAgents)
	{
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			Agent agent = (standingPoint.HasUser ? standingPoint.UserAgent : (standingPoint.HasAIMovingTo ? standingPoint.MovingAgent : null));
			if (agent == null)
			{
				continue;
			}
			if (!agent.IsAIControlled)
			{
				goto IL_0061;
			}
			if (sideFilterForAIControlledAgents != BattleSideEnum.None)
			{
				Team team = agent.Team;
				if (team == null || team.Side != sideFilterForAIControlledAgents)
				{
					goto IL_0061;
				}
			}
			goto IL_006e;
			IL_0061:
			if (!(!agent.IsAIControlled && disableForNonAIControlledAgents))
			{
				continue;
			}
			goto IL_006e;
			IL_006e:
			if (agent.GetPrimaryWieldedItemIndex() == EquipmentIndex.ExtraWeaponSlot && agent.Equipment[EquipmentIndex.ExtraWeaponSlot].Item == _givenItem)
			{
				agent.DropItem(EquipmentIndex.ExtraWeaponSlot);
			}
			base.Ai.StopUsingStandingPoint(standingPoint);
		}
		MBReadOnlyList<Formation> userFormations = base.UserFormations;
		for (int num = userFormations.Count - 1; num >= 0; num--)
		{
			Formation formation = userFormations[num];
			if (formation.Team.Side == Side)
			{
				formation.StopUsingMachine(this);
			}
		}
	}

	private void UpdateThrowingPointAttackEntities()
	{
		bool flag = false;
		List<WeakGameEntity> list = null;
		foreach (ThrowingPoint throwingPoint in _throwingPoints)
		{
			if (throwingPoint.StandingPoint.HasAIUser)
			{
				if (!flag)
				{
					list = GetEnemySiegeWeapons();
					flag = true;
					if (list == null)
					{
						foreach (ThrowingPoint throwingPoint2 in _throwingPoints)
						{
							throwingPoint2.AttackEntity = null;
							throwingPoint2.AttackEntityNearbyAgentsCheckRadius = 0f;
						}
						if (_throwingTargets.Count == 0)
						{
							break;
						}
					}
				}
				Agent userAgent = throwingPoint.StandingPoint.UserAgent;
				GameEntity attackEntity = throwingPoint.AttackEntity;
				if (attackEntity != null)
				{
					bool flag2 = false;
					if (!CanShootAtEntity(userAgent, attackEntity.WeakEntity))
					{
						flag2 = true;
					}
					else if (_throwingTargets.Contains(attackEntity))
					{
						flag2 = !throwingPoint.CanUseAttackEntity();
					}
					else if (!list.Contains(attackEntity.WeakEntity))
					{
						flag2 = true;
					}
					if (flag2)
					{
						throwingPoint.AttackEntity = null;
						throwingPoint.AttackEntityNearbyAgentsCheckRadius = 0f;
					}
				}
				if (!(throwingPoint.AttackEntity == null))
				{
					continue;
				}
				bool flag3 = false;
				if (_throwingTargets.Count > 0)
				{
					foreach (GameEntity throwingTarget in _throwingTargets)
					{
						if (attackEntity != throwingTarget && CanShootAtEntity(userAgent, throwingTarget.WeakEntity, canShootEvenIfRayCastHitsNothing: true))
						{
							throwingPoint.AttackEntity = throwingTarget;
							throwingPoint.AttackEntityNearbyAgentsCheckRadius = 1.31f;
							flag3 = true;
							break;
						}
					}
				}
				if (flag3 || list == null)
				{
					continue;
				}
				foreach (WeakGameEntity item in list)
				{
					if (attackEntity != item && CanShootAtEntity(userAgent, item))
					{
						throwingPoint.AttackEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item);
						throwingPoint.AttackEntityNearbyAgentsCheckRadius = 0f;
						break;
					}
				}
			}
			else
			{
				throwingPoint.AttackEntity = null;
			}
		}
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (GameNetwork.IsClientOrReplay)
		{
			return;
		}
		if (_tickOccasionallyTimer.Check(Mission.Current.CurrentTime))
		{
			TickOccasionally();
		}
		StandingPoint.StackArray8StandingPoint stackArray8StandingPoint = default(StandingPoint.StackArray8StandingPoint);
		int num = 0;
		Agent.StackArray8Agent stackArray8Agent = default(Agent.StackArray8Agent);
		int num2 = 0;
		foreach (StandingPoint ammoPickUpPoint2 in base.AmmoPickUpPoints)
		{
			if (ammoPickUpPoint2.HasUser)
			{
				ActionIndexCache currentAction = ammoPickUpPoint2.UserAgent.GetCurrentAction(1);
				if (!(currentAction == ActionIndexCache.act_pickup_boulder_begin))
				{
					if (currentAction == ActionIndexCache.act_pickup_boulder_end)
					{
						MissionWeapon weapon = new MissionWeapon(_givenItem, null, null, 1);
						Agent userAgent = ammoPickUpPoint2.UserAgent;
						userAgent.EquipWeaponToExtraSlotAndWield(ref weapon);
						base.Ai.StopUsingStandingPoint(ammoPickUpPoint2);
						ConsumeAmmo();
						if (userAgent.IsAIControlled)
						{
							stackArray8Agent[num2++] = userAgent;
						}
					}
					else if (!ammoPickUpPoint2.UserAgent.SetActionChannel(1, in ActionIndexCache.act_pickup_boulder_begin, ignorePriority: false, (AnimFlags)0uL))
					{
						base.Ai.StopUsingStandingPoint(ammoPickUpPoint2);
					}
				}
			}
			if (ammoPickUpPoint2.HasAIUser || ammoPickUpPoint2.HasAIMovingTo)
			{
				stackArray8StandingPoint[num++] = ammoPickUpPoint2;
			}
		}
		ThrowingPoint.StackArray8ThrowingPoint stackArray8ThrowingPoint = default(ThrowingPoint.StackArray8ThrowingPoint);
		int num3 = 0;
		foreach (ThrowingPoint throwingPoint in _throwingPoints)
		{
			throwingPoint.AmmoPickUpPoint = null;
			if (throwingPoint.AttackEntity != null || (throwingPoint.EnemyInRangeTimer.Check(Mission.Current.CurrentTime) && throwingPoint.EnemyInRangeTimer.ElapsedTime() < 3.5f))
			{
				if (!UpdateThrowingPointIfHasAnyInteractingAgent(throwingPoint))
				{
					stackArray8ThrowingPoint[num3++] = throwingPoint;
				}
				continue;
			}
			throwingPoint.StandingPoint.IsDeactivated = true;
			if (throwingPoint.WaitingPoint != null)
			{
				throwingPoint.WaitingPoint.IsDeactivated = true;
			}
		}
		for (int i = 0; i < num; i++)
		{
			if (num3 > i)
			{
				StandingPointWithWeaponRequirement ammoPickUpPoint = stackArray8StandingPoint[i] as StandingPointWithWeaponRequirement;
				stackArray8ThrowingPoint[i].AmmoPickUpPoint = ammoPickUpPoint;
			}
			else if (stackArray8StandingPoint[i].HasUser || stackArray8StandingPoint[i].HasAIMovingTo)
			{
				base.Ai.StopUsingStandingPoint(stackArray8StandingPoint[i]);
			}
		}
		for (int j = 0; j < num2; j++)
		{
			Agent agent = stackArray8Agent[j];
			StandingPoint suitableStandingPointFor = GetSuitableStandingPointFor(Side, agent);
			AssignAgentToStandingPoint(suitableStandingPointFor, agent);
		}
	}

	private bool ShouldStandAtWaitingPoint(ThrowingPoint throwingPoint)
	{
		bool result = false;
		if (throwingPoint.WaitingPoint != null)
		{
			result = true;
			Vec2 asVec = throwingPoint.StandingPoint.GameEntity.GlobalPosition.AsVec2;
			if (AgentProximityMap.CanSearchRadius(_givenItemRange))
			{
				AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, asVec, _givenItemRange);
				while (searchStruct.LastFoundAgent != null)
				{
					if (searchStruct.LastFoundAgent.State == AgentState.Active && searchStruct.LastFoundAgent.Team != null && searchStruct.LastFoundAgent.Team.Side == BattleSideEnum.Attacker)
					{
						result = false;
						break;
					}
					AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
				}
			}
			else
			{
				float num = _givenItemRange * _givenItemRange;
				if (Mission.Current.AttackerTeam != null)
				{
					MBReadOnlyList<Agent> activeAgents = Mission.Current.AttackerTeam.ActiveAgents;
					int count = activeAgents.Count;
					for (int i = 0; i < count; i++)
					{
						if (activeAgents[i].Position.AsVec2.DistanceSquared(asVec) <= num)
						{
							result = false;
							break;
						}
					}
				}
				if (Mission.Current.AttackerAllyTeam != null)
				{
					MBReadOnlyList<Agent> activeAgents2 = Mission.Current.AttackerAllyTeam.ActiveAgents;
					int count2 = activeAgents2.Count;
					for (int j = 0; j < count2; j++)
					{
						if (activeAgents2[j].Position.AsVec2.DistanceSquared(asVec) <= num)
						{
							result = true;
							break;
						}
					}
				}
			}
		}
		return result;
	}

	private bool UpdateThrowingPointIfHasAnyInteractingAgent(ThrowingPoint throwingPoint)
	{
		Agent agent = null;
		StandingPoint standingPoint = null;
		throwingPoint.StandingPoint.IsDeactivated = false;
		if (throwingPoint.StandingPoint.HasAIMovingTo)
		{
			agent = throwingPoint.StandingPoint.MovingAgent;
			standingPoint = throwingPoint.StandingPoint;
		}
		else if (throwingPoint.StandingPoint.HasUser)
		{
			agent = throwingPoint.StandingPoint.UserAgent;
			standingPoint = throwingPoint.StandingPoint;
		}
		if (throwingPoint.WaitingPoint != null)
		{
			throwingPoint.WaitingPoint.IsDeactivated = false;
			if (throwingPoint.WaitingPoint.HasAIMovingTo)
			{
				agent = throwingPoint.WaitingPoint.MovingAgent;
				standingPoint = throwingPoint.WaitingPoint;
			}
			else if (throwingPoint.WaitingPoint.HasUser)
			{
				agent = throwingPoint.WaitingPoint.UserAgent;
				standingPoint = throwingPoint.WaitingPoint;
			}
		}
		bool num = agent != null;
		if (num && agent.Controller == AgentControllerType.AI)
		{
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (primaryWieldedItemIndex == EquipmentIndex.None || agent.Equipment[primaryWieldedItemIndex].Item != _givenItem)
			{
				base.Ai.StopUsingStandingPoint(standingPoint);
				throwingPoint.AttackEntity = null;
				return num;
			}
			if (standingPoint == throwingPoint.WaitingPoint)
			{
				if (!ShouldStandAtWaitingPoint(throwingPoint))
				{
					base.Ai.StopUsingStandingPoint(standingPoint);
					AssignAgentToStandingPoint(throwingPoint.StandingPoint, agent);
					return num;
				}
			}
			else
			{
				if (agent.IsUsingGameObject && throwingPoint.AttackEntity != null)
				{
					if (throwingPoint.CanUseAttackEntity())
					{
						agent.SetScriptedTargetEntity(throwingPoint.AttackEntity.WeakEntity, Agent.AISpecialCombatModeFlags.None, ignoreIfAlreadyAttacking: true);
						return num;
					}
					agent.DisableScriptedCombatMovement();
					throwingPoint.AttackEntity = null;
					return num;
				}
				if (ShouldStandAtWaitingPoint(throwingPoint))
				{
					base.Ai.StopUsingStandingPoint(standingPoint);
					AssignAgentToStandingPoint(throwingPoint.WaitingPoint, agent);
				}
			}
		}
		return num;
	}

	public override void WriteToNetwork()
	{
		base.WriteToNetwork();
		GameNetworkMessage.WriteIntToPacket(AmmoCount, CompressionMission.RangedSiegeWeaponAmmoCompressionInfo);
	}

	float? IDetachment.GetWeightOfAgentAtNextSlot(List<(Agent, float)> candidates, out Agent match)
	{
		BattleSideEnum side = candidates[0].Item1.Team.Side;
		StandingPoint suitableStandingPointFor = GetSuitableStandingPointFor(side, null, null, candidates);
		if (suitableStandingPointFor != null)
		{
			float? weightOfNextSlot = ((IDetachment)this).GetWeightOfNextSlot(side);
			match = StonePileAI.GetSuitableAgentForStandingPoint(this, suitableStandingPointFor, candidates, new List<Agent>(), weightOfNextSlot.Value);
			if (match != null)
			{
				return weightOfNextSlot * 1f;
			}
			return null;
		}
		match = null;
		return null;
	}

	float? IDetachment.GetWeightOfAgentAtNextSlot(List<Agent> candidates, out Agent match)
	{
		BattleSideEnum side = candidates[0].Team.Side;
		StandingPoint suitableStandingPointFor = GetSuitableStandingPointFor(side, null, candidates);
		if (suitableStandingPointFor != null)
		{
			match = StonePileAI.GetSuitableAgentForStandingPoint(this, suitableStandingPointFor, candidates, new List<Agent>());
			if (match != null)
			{
				return ((IDetachment)this).GetWeightOfNextSlot(side) * 1f;
			}
			return null;
		}
		match = null;
		return null;
	}

	protected override StandingPoint GetSuitableStandingPointFor(BattleSideEnum side, Agent agent = null, List<Agent> agents = null, List<(Agent, float)> agentValuePairs = null)
	{
		List<Agent> list = new List<Agent>();
		if (agents == null)
		{
			if (agent != null)
			{
				list.Add(agent);
			}
			else if (agentValuePairs != null)
			{
				foreach (var agentValuePair in agentValuePairs)
				{
					list.Add(agentValuePair.Item1);
				}
			}
		}
		else
		{
			list.AddRange(agents);
		}
		bool flag = false;
		bool flag2 = false;
		StandingPoint standingPoint = null;
		for (int i = 0; i < _throwingPoints.Count; i++)
		{
			if (!(standingPoint == null || flag2))
			{
				break;
			}
			ThrowingPoint throwingPoint = _throwingPoints[i];
			if (IsThrowingPointAssignable(throwingPoint))
			{
				StandingPoint standingPoint2 = throwingPoint.StandingPoint;
				bool flag3 = ShouldStandAtWaitingPoint(throwingPoint);
				if (flag3)
				{
					standingPoint2 = throwingPoint.WaitingPoint;
				}
				bool flag4 = false;
				int num = 0;
				while (!flag4 && num < list.Count)
				{
					flag4 = !standingPoint2.IsDisabledForAgent(list[num]);
					num++;
				}
				if (flag4)
				{
					flag2 = flag3;
					standingPoint = standingPoint2;
				}
				else
				{
					flag = true;
				}
			}
		}
		for (int j = 0; j < base.StandingPoints.Count; j++)
		{
			if (standingPoint != null)
			{
				break;
			}
			StandingPoint standingPoint3 = base.StandingPoints[j];
			if (standingPoint3.IsDeactivated || (!standingPoint3.IsInstantUse && (standingPoint3.HasUser || standingPoint3.HasAIMovingTo)) || standingPoint3.GameEntity.HasTag("throwing") || standingPoint3.GameEntity.HasTag("wait_to_throw") || (!flag && standingPoint3.GameEntity.HasTag(AmmoPickUpTag)))
			{
				continue;
			}
			for (int k = 0; k < list.Count; k++)
			{
				if (standingPoint != null)
				{
					break;
				}
				if (!standingPoint3.IsDisabledForAgent(list[k]))
				{
					standingPoint = standingPoint3;
				}
			}
			if (list.Count == 0)
			{
				standingPoint = standingPoint3;
			}
		}
		return standingPoint;
	}

	protected override float GetDetachmentWeightAux(BattleSideEnum side)
	{
		if (IsDisabledForBattleSideAI(side))
		{
			return float.MinValue;
		}
		UsableStandingPoints.Clear();
		int num = 0;
		foreach (ThrowingPoint throwingPoint in _throwingPoints)
		{
			if (IsThrowingPointAssignable(throwingPoint))
			{
				num++;
			}
		}
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < base.StandingPoints.Count; i++)
		{
			StandingPoint standingPoint = base.StandingPoints[i];
			if (!standingPoint.GameEntity.HasTag(AmmoPickUpTag) || num <= 0)
			{
				continue;
			}
			num--;
			if (!standingPoint.IsUsableBySide(side))
			{
				continue;
			}
			if (!standingPoint.HasAIMovingTo)
			{
				if (!flag2)
				{
					UsableStandingPoints.Clear();
				}
				flag2 = true;
			}
			else if (flag2 || standingPoint.MovingAgent.Formation.Team.Side != side)
			{
				continue;
			}
			flag = true;
			UsableStandingPoints.Add((i, standingPoint));
		}
		AreUsableStandingPointsVacant = flag2;
		if (!flag)
		{
			return float.MinValue;
		}
		if (flag2)
		{
			return 1f;
		}
		if (!base.IsDetachmentRecentlyEvaluated)
		{
			return 0.1f;
		}
		return 0.01f;
	}

	protected virtual void UpdateAmmoMesh()
	{
		int num = 20 - AmmoCount;
		if (!base.GameEntity.IsValid)
		{
			return;
		}
		for (int i = 0; i < base.GameEntity.MultiMeshComponentCount; i++)
		{
			MetaMesh metaMesh = base.GameEntity.GetMetaMesh(i);
			for (int j = 0; j < metaMesh.MeshCount; j++)
			{
				metaMesh.GetMeshAtIndex(j).SetVectorArgument(0f, num, 0f, 0f);
			}
		}
	}

	private bool CanShootAtEntity(Agent agent, WeakGameEntity entity, bool canShootEvenIfRayCastHitsNothing = false)
	{
		bool result = false;
		Vec3 eyeGlobalPosition = agent.GetEyeGlobalPosition();
		Vec3 globalPosition = entity.GlobalPosition;
		Vec3 vec = eyeGlobalPosition - globalPosition;
		float num = vec.Normalize();
		if (num > 1E-05f && TaleWorlds.Library.MathF.Abs(vec.z) < TaleWorlds.Library.MathF.Cos(System.MathF.PI / 12f) && num < _givenItemRange)
		{
			if (base.Scene.RayCastForClosestEntityOrTerrain(agent.GetEyeGlobalPosition(), entity.GlobalPosition, out float _, out WeakGameEntity collidedEntity, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags))
			{
				while (collidedEntity.IsValid)
				{
					if (collidedEntity == entity)
					{
						result = true;
						break;
					}
					collidedEntity = collidedEntity.Parent;
				}
			}
			else
			{
				result = canShootEvenIfRayCastHitsNothing;
			}
		}
		return result;
	}

	private List<WeakGameEntity> GetEnemySiegeWeapons()
	{
		List<WeakGameEntity> list = null;
		if (Mission.Current.Teams.Attacker.TeamAI is TeamAISiegeComponent)
		{
			foreach (IPrimarySiegeWeapon primarySiegeWeapon in ((TeamAISiegeComponent)Mission.Current.Teams.Attacker.TeamAI).PrimarySiegeWeapons)
			{
				if (primarySiegeWeapon is SiegeWeapon { GameEntity: var gameEntity } siegeWeapon && gameEntity.GetFirstScriptOfType<DestructableComponent>() != null && siegeWeapon.IsUsed)
				{
					if (list == null)
					{
						list = new List<WeakGameEntity>();
					}
					list.Add(siegeWeapon.GameEntity);
				}
			}
		}
		return list;
	}

	private bool IsThrowingPointAssignable(ThrowingPoint throwingPoint)
	{
		if (throwingPoint.AmmoPickUpPoint == null && !throwingPoint.StandingPoint.IsDeactivated && !throwingPoint.StandingPoint.HasUser && !throwingPoint.StandingPoint.HasAIMovingTo)
		{
			if (throwingPoint.WaitingPoint != null)
			{
				if (!throwingPoint.WaitingPoint.IsDeactivated && !throwingPoint.WaitingPoint.HasUser)
				{
					return !throwingPoint.WaitingPoint.HasAIMovingTo;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private bool AssignAgentToStandingPoint(StandingPoint standingPoint, Agent agent)
	{
		if (standingPoint == null || agent == null || !StonePileAI.IsAgentAssignable(agent))
		{
			return false;
		}
		int num = base.StandingPoints.IndexOf(standingPoint);
		if (num >= 0)
		{
			((IDetachment)this).AddAgent(agent, num, Agent.AIScriptedFrameFlags.None);
			if (agent.Formation != null)
			{
				agent.Formation.DetachUnit(agent, ((IDetachment)this).IsLoose);
				agent.Detachment = this;
				agent.SetDetachmentWeight(GetWeightOfStandingPoint(standingPoint));
				return true;
			}
		}
		return false;
	}
}
