using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.AnimationPoints;

public class DynamicObjectAnimationPoint : StandingPoint
{
	private enum State
	{
		NotUsing,
		StartToUse,
		Using
	}

	private const float RangeThreshold = 0.2f;

	private const float RotationScoreThreshold = 0.99f;

	private const float ActionSpeedRandomMinValue = 0.8f;

	private const float AnimationRandomProgressMaxValue = 0.5f;

	private const string AlternativeTag = "alternative";

	private ActionIndexCache _lastAction;

	public string ArriveAction = "";

	public string LoopStartAction = "";

	public string LeaveAction = "";

	public float ActionSpeed = 1f;

	public bool KeepOldVisibility;

	private Vec3 _pointRotation;

	private ActionIndexCache ArriveActionCode;

	protected ActionIndexCache LoopStartActionCode;

	private ActionIndexCache LeaveActionCode;

	protected ActionIndexCache DefaultActionCode;

	private State _state;

	public float ForwardDistanceToPivotPoint;

	public float SideDistanceToPivotPoint;

	private List<AnimationPoint.ItemForBone> _itemsForBones;

	public string RightHandItem = "";

	public HumanBone RightHandItemBone = HumanBone.ItemR;

	public string LeftHandItem = "";

	public HumanBone LeftHandItemBone = HumanBone.ItemL;

	private EquipmentIndex _equipmentIndexMainHand;

	private EquipmentIndex _equipmentIndexOffHand;

	public int GroupId = -1;

	private string _selectedRightHandItem;

	private string _selectedLeftHandItem;

	public bool IsArriveActionFinished { get; private set; }

	protected string SelectedRightHandItem
	{
		get
		{
			return _selectedRightHandItem;
		}
		set
		{
			if (value != _selectedRightHandItem)
			{
				AnimationPoint.ItemForBone newItem = new AnimationPoint.ItemForBone(RightHandItemBone, value, isVisible: false);
				AssignItemToBone(newItem);
				_selectedRightHandItem = value;
			}
		}
	}

	protected string SelectedLeftHandItem
	{
		get
		{
			return _selectedLeftHandItem;
		}
		set
		{
			if (value != _selectedLeftHandItem)
			{
				AnimationPoint.ItemForBone newItem = new AnimationPoint.ItemForBone(LeftHandItemBone, value, isVisible: false);
				AssignItemToBone(newItem);
				_selectedLeftHandItem = value;
			}
		}
	}

	public override bool PlayerStopsUsingWhenInteractsWithOther => false;

	public override bool DisableCombatActionsOnUse => !base.IsInstantUse;

	public bool IsActive { get; private set; } = true;

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		Tick(dt);
	}

	public override TickRequirement GetTickRequirement()
	{
		if (base.HasUser)
		{
			return base.GetTickRequirement() | TickRequirement.Tick;
		}
		return base.GetTickRequirement();
	}

	protected override bool DoesActionTypeStopUsingGameObject(Agent.ActionCodeType actionType)
	{
		return false;
	}

	public override bool IsUsableByAgent(Agent userAgent)
	{
		if (IsActive)
		{
			return base.IsUsableByAgent(userAgent);
		}
		return false;
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		base.OnUse(userAgent, agentBoneIndex);
		_equipmentIndexMainHand = base.UserAgent.GetPrimaryWieldedItemIndex();
		_equipmentIndexOffHand = base.UserAgent.GetOffhandWieldedItemIndex();
		_state = State.NotUsing;
	}

	public override WorldFrame GetUserFrameForAgent(Agent agent)
	{
		WorldFrame userFrameForAgent = base.GetUserFrameForAgent(agent);
		float agentScale = agent.AgentScale;
		userFrameForAgent.Origin.SetVec2(userFrameForAgent.Origin.AsVec2 + (userFrameForAgent.Rotation.f.AsVec2 * (0f - ForwardDistanceToPivotPoint) + userFrameForAgent.Rotation.s.AsVec2 * SideDistanceToPivotPoint) * (1f - agentScale));
		return userFrameForAgent;
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (base.HasUser && base.UserAgent == agent)
		{
			if (IsActive)
			{
				return base.IsDeactivated;
			}
			return true;
		}
		if (!IsActive || agent.MountAgent != null || base.IsDeactivated || !agent.IsAbleToUseMachine() || (!agent.IsAIControlled && (base.IsDisabledForPlayers || base.HasUser)))
		{
			return true;
		}
		WeakGameEntity parent = base.GameEntity.Parent;
		if (!parent.IsValid || !parent.HasScriptOfType<UsableMachine>() || !base.GameEntity.HasTag("alternative"))
		{
			return base.IsDisabledForAgent(agent);
		}
		if (agent.IsAIControlled && parent.HasTag("reserved"))
		{
			return true;
		}
		string text = ((agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator != null) ? agent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag : string.Empty);
		if (!string.IsNullOrEmpty(text) && !parent.HasTag(text))
		{
			return true;
		}
		foreach (StandingPoint standingPoint in parent.GetFirstScriptOfType<UsableMachine>().StandingPoints)
		{
			if (standingPoint is AnimationPoint animationPoint && GroupId == animationPoint.GroupId && !animationPoint.IsDeactivated && (animationPoint.HasUser || (animationPoint.HasAIMovingTo && !animationPoint.IsAIMovingTo(agent))) && animationPoint.GameEntity.HasTag("alternative"))
			{
				return true;
			}
		}
		return false;
	}

	public override void SimulateTick(float dt)
	{
		Tick(dt, isSimulation: true);
	}

	public override bool HasAlternative()
	{
		return GroupId >= 0;
	}

	protected override void OnInit()
	{
		base.OnInit();
		_itemsForBones = new List<AnimationPoint.ItemForBone>();
		SetActionCodes();
		InitParameters();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected override void OnEditorInit()
	{
		_itemsForBones = new List<AnimationPoint.ItemForBone>();
		SetActionCodes();
		InitParameters();
	}

	public override void OnUserConversationStart()
	{
		_pointRotation = base.UserAgent.Frame.rotation.f;
		_pointRotation.Normalize();
		if (KeepOldVisibility)
		{
			return;
		}
		foreach (AnimationPoint.ItemForBone itemsForBone in _itemsForBones)
		{
			itemsForBone.OldVisibility = itemsForBone.IsVisible;
		}
		SetAgentItemsVisibility(isVisible: false);
	}

	public override void OnUserConversationEnd()
	{
		base.UserAgent.ResetLookAgent();
		base.UserAgent.LookDirection = _pointRotation;
		base.UserAgent.SetActionChannel(0, in LoopStartActionCode, ignorePriority: false, (AnimFlags)0uL);
		foreach (AnimationPoint.ItemForBone itemsForBone in _itemsForBones)
		{
			if (itemsForBone.OldVisibility)
			{
				SetAgentItemVisibility(itemsForBone, isVisible: true);
			}
		}
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		SetAgentItemsVisibility(isVisible: false);
		RevertWeaponWieldSheathState();
		if (base.UserAgent.IsActive())
		{
			if (LeaveActionCode == ActionIndexCache.act_none)
			{
				base.UserAgent.SetActionChannel(0, in LeaveActionCode, ignorePriority: false, (AnimFlags)Math.Min(base.UserAgent.GetCurrentActionPriority(0), 73));
			}
			else if (IsArriveActionFinished)
			{
				ActionIndexCache actionCode = base.UserAgent.GetCurrentAction(0);
				if (actionCode != LeaveActionCode && !base.UserAgent.ActionSet.AreActionsAlternatives(in actionCode, in LeaveActionCode))
				{
					AnimFlags additionalFlags = (AnimFlags)Math.Min(base.UserAgent.GetCurrentActionPriority(0), base.UserAgent.IsSitting() ? 94 : 73);
					base.UserAgent.SetActionChannel(0, in LeaveActionCode, ignorePriority: false, additionalFlags);
				}
			}
			else
			{
				ActionIndexCache actionIndexCache = userAgent.GetCurrentAction(0);
				if (actionIndexCache == ArriveActionCode && ArriveActionCode != ActionIndexCache.act_none)
				{
					MBActionSet actionSet = userAgent.ActionSet;
					float currentActionProgress = userAgent.GetCurrentActionProgress(0);
					float actionBlendOutStartProgress = MBActionSet.GetActionBlendOutStartProgress(actionSet, in actionIndexCache);
					if (currentActionProgress < actionBlendOutStartProgress)
					{
						float num = (actionBlendOutStartProgress - currentActionProgress) / actionBlendOutStartProgress;
						MBActionSet.GetActionBlendOutStartProgress(actionSet, in LeaveActionCode);
					}
				}
			}
		}
		_lastAction = ActionIndexCache.act_none;
		if (base.UserAgent.GetLookAgent() != null)
		{
			base.UserAgent.ResetLookAgent();
		}
		IsArriveActionFinished = false;
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
	}

	private void RevertWeaponWieldSheathState()
	{
		if (_equipmentIndexMainHand != EquipmentIndex.None && AutoSheathWeapons)
		{
			base.UserAgent.TryToWieldWeaponInSlot(_equipmentIndexMainHand, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
		}
		else if (_equipmentIndexMainHand == EquipmentIndex.None && AutoWieldWeapons)
		{
			base.UserAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
		}
		if (_equipmentIndexOffHand != EquipmentIndex.None && AutoSheathWeapons)
		{
			base.UserAgent.TryToWieldWeaponInSlot(_equipmentIndexOffHand, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
		}
		else if (_equipmentIndexOffHand == EquipmentIndex.None && AutoWieldWeapons)
		{
			base.UserAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimation);
		}
	}

	public void SetAgentItemsVisibility(bool isVisible)
	{
		if (base.UserAgent.IsMainAgent)
		{
			return;
		}
		foreach (AnimationPoint.ItemForBone itemsForBone in _itemsForBones)
		{
			SetAgentItemVisibility(itemsForBone, isVisible);
		}
	}

	private void SetAgentItemVisibility(AnimationPoint.ItemForBone item, bool isVisible)
	{
		sbyte realBoneIndex = base.UserAgent.AgentVisuals.GetRealBoneIndex(item.HumanBone);
		base.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetPrefabVisibility(realBoneIndex, item.ItemPrefabName, isVisible);
		item.IsVisible = isVisible;
	}

	private void Tick(float dt, bool isSimulation = false)
	{
		if (!base.HasUser)
		{
			return;
		}
		if (Game.Current != null && Game.Current.IsDevelopmentMode)
		{
			base.UserAgent.GetTargetPosition().IsNonZero();
		}
		ActionIndexCache actionCode = base.UserAgent.GetCurrentAction(0);
		switch (_state)
		{
		case State.NotUsing:
			if (IsTargetReached() && base.UserAgent.MovementVelocity.LengthSquared < 0.1f && base.UserAgent.IsAbleToUseMachine())
			{
				if (ArriveActionCode != ActionIndexCache.act_none)
				{
					Agent userAgent = base.UserAgent;
					ref ActionIndexCache arriveActionCode = ref ArriveActionCode;
					long additionalFlags = 0L;
					float blendInPeriod = (isSimulation ? 0f : (-0.2f));
					userAgent.SetActionChannel(0, in arriveActionCode, ignorePriority: false, (AnimFlags)additionalFlags, 0f, MBRandom.RandomFloatRanged(0.8f, 1f), blendInPeriod);
				}
				_state = State.StartToUse;
			}
			break;
		case State.StartToUse:
			if (ArriveActionCode != ActionIndexCache.act_none && isSimulation)
			{
				SimulateAnimations(0.1f);
			}
			if (ArriveActionCode == ActionIndexCache.act_none || actionCode == ArriveActionCode || base.UserAgent.ActionSet.AreActionsAlternatives(in actionCode, in ArriveActionCode))
			{
				base.UserAgent.ClearTargetFrame();
				WorldFrame userFrameForAgent = GetUserFrameForAgent(base.UserAgent);
				_pointRotation = userFrameForAgent.Rotation.f;
				_pointRotation.Normalize();
				if (base.UserAgent != Agent.Main)
				{
					base.UserAgent.SetScriptedPositionAndDirection(ref userFrameForAgent.Origin, userFrameForAgent.Rotation.f.AsVec2.RotationInRadians, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.DoNotRun);
				}
				_state = State.Using;
			}
			break;
		case State.Using:
			if (isSimulation)
			{
				float dt2 = 0.1f;
				if (actionCode != ArriveActionCode)
				{
					dt2 = 0.01f + MBRandom.RandomFloat * 0.09f;
				}
				SimulateAnimations(dt2);
			}
			if (!IsArriveActionFinished && (ArriveActionCode == ActionIndexCache.act_none || base.UserAgent.GetCurrentAction(0) != ArriveActionCode))
			{
				IsArriveActionFinished = true;
				AddItemsToAgent();
			}
			if (IsRotationCorrectDuringUsage())
			{
				base.UserAgent.SetActionChannel(0, in LoopStartActionCode, ignorePriority: false, (AnimFlags)0uL, 0f, (ActionSpeed < 0.8f) ? ActionSpeed : MBRandom.RandomFloatRanged(0.8f, ActionSpeed), isSimulation ? 0f : (-0.2f), 0.4f, isSimulation ? MBRandom.RandomFloatRanged(0f, 0.5f) : 0f);
			}
			break;
		}
	}

	private void SetActionCodes()
	{
		ArriveActionCode = ActionIndexCache.Create(ArriveAction);
		LoopStartActionCode = ActionIndexCache.Create(LoopStartAction);
		LeaveActionCode = ActionIndexCache.Create(LeaveAction);
		SelectedRightHandItem = RightHandItem;
		SelectedLeftHandItem = LeftHandItem;
	}

	private void InitParameters()
	{
		_pointRotation = Vec3.Zero;
		_state = State.NotUsing;
		LockUserPositions = true;
	}

	protected void AssignItemToBone(AnimationPoint.ItemForBone newItem)
	{
		if (!string.IsNullOrEmpty(newItem.ItemPrefabName) && !_itemsForBones.Contains(newItem))
		{
			_itemsForBones.Add(newItem);
		}
	}

	public bool IsRotationCorrectDuringUsage()
	{
		if (!_pointRotation.IsNonZero)
		{
			return false;
		}
		return Vec2.DotProduct(_pointRotation.AsVec2, base.UserAgent.GetMovementDirection()) > 0.99f;
	}

	protected bool CanAgentUseItem(Agent agent)
	{
		if (agent.GetComponent<CampaignAgentComponent>() != null)
		{
			return agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null;
		}
		return false;
	}

	protected void AddItemsToAgent()
	{
		if (!CanAgentUseItem(base.UserAgent) || !IsArriveActionFinished)
		{
			return;
		}
		if (_itemsForBones.Count != 0)
		{
			base.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.HoldAndHideRecentlyUsedMeshes();
		}
		foreach (AnimationPoint.ItemForBone itemsForBone in _itemsForBones)
		{
			ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(itemsForBone.ItemPrefabName);
			if (itemObject != null)
			{
				EquipmentIndex equipmentIndex = FindProperSlot(itemObject);
				if (!base.UserAgent.Equipment[equipmentIndex].IsEmpty)
				{
					base.UserAgent.DropItem(equipmentIndex);
				}
				MissionWeapon weapon = new MissionWeapon(itemObject, null, base.UserAgent.Origin?.Banner);
				base.UserAgent.EquipWeaponWithNewEntity(equipmentIndex, ref weapon);
				base.UserAgent.TryToWieldWeaponInSlot(equipmentIndex, Agent.WeaponWieldActionType.Instant, isWieldedOnSpawn: false);
			}
			else
			{
				sbyte realBoneIndex = base.UserAgent.AgentVisuals.GetRealBoneIndex(itemsForBone.HumanBone);
				base.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetPrefabVisibility(realBoneIndex, itemsForBone.ItemPrefabName, isVisible: true);
			}
		}
	}

	private EquipmentIndex FindProperSlot(ItemObject item)
	{
		EquipmentIndex result = EquipmentIndex.Weapon3;
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex <= EquipmentIndex.Weapon3; equipmentIndex++)
		{
			if (base.UserAgent.Equipment[equipmentIndex].IsEmpty)
			{
				result = equipmentIndex;
			}
			else if (base.UserAgent.Equipment[equipmentIndex].Item == item)
			{
				return equipmentIndex;
			}
		}
		return result;
	}

	private void SimulateAnimations(float dt)
	{
		base.UserAgent.TickActionChannels(dt);
		Vec3 vec = base.UserAgent.ComputeAnimationDisplacement(dt);
		if (vec.LengthSquared > 0f)
		{
			base.UserAgent.TeleportToPosition(base.UserAgent.Position + vec);
		}
		base.UserAgent.AgentVisuals.GetSkeleton().TickAnimations(dt, base.UserAgent.AgentVisuals.GetGlobalFrame(), tickAnimsForChildren: true);
	}

	private bool IsTargetReached()
	{
		float num = Vec2.DotProduct(base.UserAgent.GetTargetDirection().AsVec2, base.UserAgent.GetMovementDirection());
		if ((base.UserAgent.Position.AsVec2 - base.UserAgent.GetTargetPosition()).LengthSquared < 0.040000003f)
		{
			return num > 0.99f;
		}
		return false;
	}
}
