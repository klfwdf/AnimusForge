using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionMainAgentInteractionComponent
{
	public delegate void MissionFocusGainedEventDelegate(Agent agent, IFocusable focusableObject, bool isInteractable);

	public delegate void MissionFocusLostEventDelegate(Agent agent, IFocusable focusableObject);

	public delegate void MissionFocusHealthChangeDelegate(IFocusable focusable, float healthPercentage, bool hideHealthbarWhenFull);

	private IFocusable _currentInteractableObject;

	private sbyte _currentInteractableObjectBoneIndex;

	private readonly MissionMainAgentController _mainAgentController;

	public IFocusable CurrentFocusedObject { get; private set; }

	public IFocusable CurrentFocusedMachine { get; private set; }

	private Mission CurrentMission => _mainAgentController.Mission;

	private MissionScreen CurrentMissionScreen => _mainAgentController.MissionScreen;

	private Scene CurrentMissionScene => _mainAgentController.Mission.Scene;

	public event MissionFocusGainedEventDelegate OnFocusGained;

	public event MissionFocusLostEventDelegate OnFocusLost;

	public event MissionFocusHealthChangeDelegate OnFocusHealthChanged;

	public void SetCurrentFocusedObject(IFocusable focusedObject, IFocusable focusedMachine, sbyte focusedObjectBoneIndex, bool isInteractable)
	{
		if (CurrentFocusedObject != null && (CurrentFocusedObject != focusedObject || (_currentInteractableObject != null && !isInteractable) || (_currentInteractableObject == null && isInteractable)))
		{
			FocusLost(CurrentFocusedObject, CurrentFocusedMachine);
			_currentInteractableObject = null;
			_currentInteractableObjectBoneIndex = -1;
			CurrentFocusedObject = null;
			CurrentFocusedMachine = null;
		}
		if (CurrentFocusedObject == null && focusedObject != null)
		{
			if (focusedObject != CurrentFocusedObject)
			{
				FocusGained(focusedObject, focusedMachine, isInteractable);
			}
			if (isInteractable)
			{
				_currentInteractableObject = focusedObject;
			}
			CurrentFocusedObject = focusedObject;
			CurrentFocusedMachine = focusedMachine;
		}
		if (_currentInteractableObject != null && _currentInteractableObject == focusedObject)
		{
			_currentInteractableObjectBoneIndex = focusedObjectBoneIndex;
		}
	}

	public void ClearFocus()
	{
		if (CurrentFocusedObject != null)
		{
			FocusLost(CurrentFocusedObject, CurrentFocusedMachine);
		}
		_currentInteractableObject = null;
		_currentInteractableObjectBoneIndex = -1;
		CurrentFocusedObject = null;
		CurrentFocusedMachine = null;
	}

	public void OnClearScene()
	{
		ClearFocus();
	}

	public MissionMainAgentInteractionComponent(MissionMainAgentController mainAgentController)
	{
		_mainAgentController = mainAgentController;
	}

	private static float GetCollisionDistanceSquaredOfIntersectionFromMainAgentEye(Vec3 rayStartPoint, Vec3 rayDirection, float rayLength)
	{
		float result = rayLength * rayLength;
		Vec3 v = rayStartPoint + rayDirection * rayLength;
		Vec3 position = Agent.Main.Position;
		float eyeGlobalHeight = Agent.Main.GetEyeGlobalHeight();
		Vec3 vec = new Vec3(position.x, position.y, position.z + eyeGlobalHeight);
		float num = v.z - vec.z;
		if (num < 0f)
		{
			num = MBMath.ClampFloat(0f - num, 0f, (Agent.Main.HasMount ? (eyeGlobalHeight - Agent.Main.MountAgent.GetEyeGlobalHeight()) : eyeGlobalHeight) * 0.75f);
			vec.z -= num;
			result = vec.DistanceSquared(v);
		}
		return result;
	}

	private void FocusGained(IFocusable focusedObject, IFocusable focusedMachine, bool isInteractable)
	{
		focusedObject.OnFocusGain(Agent.Main);
		focusedMachine?.OnFocusGain(Agent.Main);
		foreach (MissionBehavior missionBehavior in CurrentMission.MissionBehaviors)
		{
			missionBehavior.OnFocusGained(Agent.Main, focusedObject, isInteractable);
		}
		this.OnFocusGained?.Invoke(Agent.Main, focusedObject, isInteractable);
	}

	private void FocusLost(IFocusable focusedObject, IFocusable focusedMachine)
	{
		focusedObject.OnFocusLose(Agent.Main);
		focusedMachine?.OnFocusLose(Agent.Main);
		foreach (MissionBehavior missionBehavior in CurrentMission.MissionBehaviors)
		{
			missionBehavior.OnFocusLost(Agent.Main, focusedObject);
		}
		this.OnFocusLost?.Invoke(Agent.Main, focusedObject);
	}

	public void FocusTick()
	{
		IFocusable focusable = null;
		sbyte focusedObjectBoneIndex = -1;
		UsableMachine usableMachine = null;
		bool flag = false;
		bool flag2 = false;
		if (Mission.Current.Mode != MissionMode.Conversation && Mission.Current.Mode != MissionMode.CutScene)
		{
			Agent main = Agent.Main;
			if (main != null && (CurrentMission.IsMainAgentItemInteractionEnabled || IsFocusMountable()) && !CurrentMission.IsOrderMenuOpen && !CurrentMissionScreen.SceneLayer.Input.IsGameKeyDown(25) && main.IsAbleToUseMachine())
			{
				float num = 10f;
				Vec3 direction = CurrentMissionScreen.CombatCamera.Direction;
				Vec3 vec = direction;
				Vec3 position = CurrentMissionScreen.CombatCamera.Position;
				Vec3 position2 = main.Position;
				Vec3 closestPoint = new Vec3(position.x, position.y);
				float num2 = closestPoint.Distance(new Vec3(position2.x, position2.y));
				Vec3 vec2 = position * (1f - num2) + (position + direction) * num2;
				if (CurrentMissionScene.FocusRayCastForFixedPhysics(vec2, vec2 + vec * num, out var collisionDistance, out closestPoint, out var _, 0.01f, BodyFlags.CommonFlagsThatDoNotBlockRay))
				{
					num = collisionDistance;
				}
				if (CurrentMissionScene.RayCastForClosestEntityOrTerrain(vec2, vec2 + vec * num, out collisionDistance, 0.01f, BodyFlags.CommonFlagsThatDoNotBlockRay) && collisionDistance < num)
				{
					num = collisionDistance;
				}
				float num3 = float.MaxValue;
				Agent agent = null;
				float collisionDistance2;
				Agent agent2 = CurrentMission.RayCastForClosestAgent(vec2, vec2 + vec * (num + 0.01f), main.Index, 0.3f, out collisionDistance2);
				if (agent2 != null && agent2.State != AgentState.Killed && agent2.State != AgentState.Unconscious && (!agent2.IsMount || (agent2.RiderAgent == null && main.MountAgent == null && main.CanReachAgent(agent2))))
				{
					flag2 = main.CanInteractWithAgent(agent2, CurrentMissionScreen.CameraElevation);
					if (flag2 || agent2.IsEnemyOf(main))
					{
						num3 = collisionDistance2;
						focusable = agent2;
						focusedObjectBoneIndex = -1;
					}
					else
					{
						agent = agent2;
					}
				}
				float collisionDistance3;
				sbyte boneIndex;
				Agent agent3 = CurrentMission.RayCastForClosestAgentsLimbs(vec2, vec2 + vec * (num + 0.01f), main.Index, 0.3f, out collisionDistance3, out boneIndex);
				if (agent3 != null && (agent3.State == AgentState.Killed || agent3.State == AgentState.Unconscious) && (!agent3.IsMount || (agent3.RiderAgent == null && main.MountAgent == null && main.CanReachAgent(agent3))) && num3 > collisionDistance3)
				{
					flag2 = main.CanInteractWithAgent(agent3, CurrentMissionScreen.CameraElevation);
					if (flag2 || agent3.IsEnemyOf(main))
					{
						num3 = collisionDistance3;
						focusable = agent3;
						focusedObjectBoneIndex = boneIndex;
					}
				}
				float valueTo = 3f;
				num += 0.1f;
				WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
				float rayLength = 0f;
				bool flag3 = false;
				if (CurrentMissionScene.FocusRayCastForFixedPhysics(vec2, vec2 + vec * num, out var collisionDistance4, out closestPoint, out var collidedEntity2, 0.2f) && collisionDistance4 < num && collisionDistance4 < num3)
				{
					num = collisionDistance4;
					rayLength = collisionDistance4;
					weakGameEntity = collidedEntity2;
					flag3 = weakGameEntity.IsValid;
				}
				bool flag4 = false;
				for (int i = 0; i < 2; i++)
				{
					float num4 = MathF.Lerp(1f, valueTo, (float)i / 1f);
					float num5 = 0.2f * (num4 - 1f);
					if (!CurrentMissionScene.RayCastForClosestEntityOrTerrain(vec2 + vec * num5, vec2 + vec * num, out collisionDistance4, out collidedEntity2, 0.2f * num4, BodyFlags.CommonFocusRayCastExcludeFlags) || !(collisionDistance4 + num5 < num) || !(collisionDistance4 + num5 < num3))
					{
						continue;
					}
					bool flag5 = false;
					WeakGameEntity weakGameEntity2 = collidedEntity2;
					while (weakGameEntity2.IsValid)
					{
						if (weakGameEntity2.HasScriptWithInterfaceOfType<IFocusable>() && weakGameEntity2.GetFirstScriptWithInterfaceOfType<IFocusable>().IsFocusable)
						{
							flag5 = true;
							break;
						}
						weakGameEntity2 = weakGameEntity2.Parent;
					}
					if (!flag4 || flag5)
					{
						num = collisionDistance4 + num5;
						rayLength = collisionDistance4 + num5;
						weakGameEntity = collidedEntity2;
						flag3 = weakGameEntity.IsValid;
						flag4 = true;
						if (flag5)
						{
							break;
						}
					}
				}
				if (flag3)
				{
					while ((!weakGameEntity.HasScriptWithInterfaceOfType<IFocusable>() || !weakGameEntity.GetFirstScriptWithInterfaceOfType<IFocusable>().IsFocusable) && weakGameEntity.Parent.IsValid)
					{
						weakGameEntity = weakGameEntity.Parent;
					}
					usableMachine = weakGameEntity.GetFirstScriptOfType<UsableMachine>();
					if (usableMachine != null && !usableMachine.IsDisabled)
					{
						WeakGameEntity validVacantReachableStandingPointForAgent = usableMachine.GetValidVacantReachableStandingPointForAgent(main);
						if (validVacantReachableStandingPointForAgent.IsValid)
						{
							weakGameEntity = validVacantReachableStandingPointForAgent;
						}
					}
					UsableMissionObject firstScriptOfType = weakGameEntity.GetFirstScriptOfType<UsableMissionObject>();
					if (firstScriptOfType is SpawnedItemEntity)
					{
						if (CurrentMission.IsMainAgentItemInteractionEnabled && firstScriptOfType.IsFocusable && !main.IsInWater() && main.CanReachObject(firstScriptOfType, GetCollisionDistanceSquaredOfIntersectionFromMainAgentEye(vec2, vec, rayLength)))
						{
							focusable = firstScriptOfType;
							focusedObjectBoneIndex = -1;
							if (main.CanUseObject(firstScriptOfType))
							{
								flag = true;
							}
						}
					}
					else if (firstScriptOfType != null)
					{
						if (firstScriptOfType.IsFocusable)
						{
							focusable = firstScriptOfType;
							focusedObjectBoneIndex = -1;
							if (CurrentMission.IsMainAgentObjectInteractionEnabled && !main.IsUsingGameObject && main.IsAbleToUseMachine() && main.ObjectHasVacantPosition(firstScriptOfType) && main.CanUseObject(firstScriptOfType))
							{
								flag = true;
							}
						}
					}
					else if (usableMachine != null)
					{
						if (usableMachine.IsFocusable)
						{
							focusable = usableMachine;
							focusedObjectBoneIndex = -1;
							flag = !usableMachine.IsDeactivated;
						}
					}
					else
					{
						IFocusable firstScriptWithInterfaceOfType = weakGameEntity.GetFirstScriptWithInterfaceOfType<IFocusable>();
						if (firstScriptWithInterfaceOfType != null && firstScriptWithInterfaceOfType.IsFocusable)
						{
							focusable = firstScriptWithInterfaceOfType;
							focusedObjectBoneIndex = -1;
						}
					}
				}
				if ((focusable == null || !flag) && main.MountAgent != null && main.CanInteractWithAgent(main.MountAgent, CurrentMissionScreen.CameraElevation))
				{
					focusable = main.MountAgent;
					focusedObjectBoneIndex = -1;
					flag2 = true;
				}
				if (focusable == null && agent != null)
				{
					focusable = agent;
					flag2 = true;
				}
			}
			if (focusable == null)
			{
				ClearFocus();
				return;
			}
			bool isInteractable = ((focusable is Agent) ? flag2 : flag);
			SetCurrentFocusedObject(focusable, usableMachine, focusedObjectBoneIndex, isInteractable);
		}
		else if (CurrentFocusedObject != null && Mission.Current.Mode != MissionMode.Conversation)
		{
			ClearFocus();
		}
	}

	public void FocusStateCheckTick()
	{
		if (!CurrentMissionScreen.SceneLayer.Input.IsGameKeyPressed(13) || (!CurrentMission.IsMainAgentItemInteractionEnabled && !IsFocusMountable()) || CurrentMissionScreen.IsRadialMenuActive || CurrentMission.IsOrderMenuOpen)
		{
			return;
		}
		Agent main = Agent.Main;
		if (main.IsUsingGameObject && !(main.CurrentlyUsedGameObject is SpawnedItemEntity) && (!(_currentInteractableObject is Agent) || !(main.CurrentlyUsedGameObject is StandingPoint { PlayerStopsUsingWhenInteractsWithOther: false })))
		{
			main.HandleStopUsingAction();
			ClearFocus();
			return;
		}
		if (_currentInteractableObject is UsableMissionObject usableMissionObject)
		{
			if (!main.IsUsingGameObject && main.IsAbleToUseMachine() && !(usableMissionObject is SpawnedItemEntity) && main.ObjectHasVacantPosition(usableMissionObject))
			{
				main.HandleStartUsingAction(usableMissionObject, -1);
			}
			return;
		}
		Agent agent = _currentInteractableObject as Agent;
		if (main.IsAbleToUseMachine())
		{
			agent?.OnUse(main, _currentInteractableObjectBoneIndex);
		}
	}

	private bool IsFocusMountable()
	{
		if (_currentInteractableObject is Agent agent)
		{
			return agent.IsMount;
		}
		return false;
	}

	public void FocusedItemHealthTick()
	{
		if (CurrentFocusedObject is UsableMissionObject { GameEntity: var weakGameEntity })
		{
			while (weakGameEntity.IsValid && !weakGameEntity.HasScriptOfType<UsableMachine>())
			{
				weakGameEntity = weakGameEntity.Parent;
			}
			if (weakGameEntity.IsValid)
			{
				UsableMachine firstScriptOfType = weakGameEntity.GetFirstScriptOfType<UsableMachine>();
				if (firstScriptOfType?.DestructionComponent != null)
				{
					this.OnFocusHealthChanged?.Invoke(CurrentFocusedObject, firstScriptOfType.DestructionComponent.HitPoint / firstScriptOfType.DestructionComponent.MaxHitPoint, hideHealthbarWhenFull: true);
				}
			}
		}
		else if (CurrentFocusedObject is UsableMachine usableMachine)
		{
			if (usableMachine.DestructionComponent != null)
			{
				this.OnFocusHealthChanged?.Invoke(CurrentFocusedObject, usableMachine.DestructionComponent.HitPoint / usableMachine.DestructionComponent.MaxHitPoint, hideHealthbarWhenFull: true);
			}
		}
		else if (CurrentFocusedObject is DestructableComponent destructableComponent)
		{
			this.OnFocusHealthChanged?.Invoke(CurrentFocusedObject, destructableComponent.HitPoint / destructableComponent.MaxHitPoint, hideHealthbarWhenFull: true);
		}
	}
}
