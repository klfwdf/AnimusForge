using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public abstract class UsableMissionObject : SynchedMissionObject, IFocusable, IUsable, IVisible
{
	[DefineSynchedMissionObjectType(typeof(UsableMissionObject))]
	public struct UsableMissionObjectRecord : ISynchedMissionObjectReadableRecord
	{
		public bool IsDeactivated { get; private set; }

		public bool IsDisabledForPlayers { get; private set; }

		public bool IsUserAgentExists { get; private set; }

		public int AgentIndex { get; private set; }

		public UsableMissionObjectRecord(bool isDeactivated, bool isDisabledForPlayers, bool isUserAgentExists, int agentIndex)
		{
			IsDeactivated = isDeactivated;
			IsDisabledForPlayers = isDisabledForPlayers;
			IsUserAgentExists = isUserAgentExists;
			AgentIndex = agentIndex;
		}

		public bool ReadFromNetwork(ref bool bufferReadValid)
		{
			IsDeactivated = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			IsDisabledForPlayers = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			IsUserAgentExists = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			if (IsUserAgentExists)
			{
				AgentIndex = GameNetworkMessage.ReadAgentIndexFromPacket(ref bufferReadValid);
			}
			return bufferReadValid;
		}
	}

	private Agent _userAgent;

	private bool _areUserPositionsUpdatedInTheMachineTick;

	private readonly List<UsableMissionObjectComponent> _components;

	[EditableScriptComponentVariable(false, "")]
	public TextObject DescriptionMessage = TextObject.GetEmpty();

	[EditableScriptComponentVariable(false, "")]
	public TextObject ActionMessage = TextObject.GetEmpty();

	private bool _needsSingleThreadTickOnce;

	private bool _isDeactivated;

	private bool _isDisabledForPlayers;

	public virtual FocusableObjectType FocusableObjectType => FocusableObjectType.Item;

	public virtual bool IsFocusable => true;

	public Agent UserAgent
	{
		get
		{
			return _userAgent;
		}
		private set
		{
			if (_userAgent != value)
			{
				PreviousUserAgent = _userAgent;
				_userAgent = value;
				SetScriptComponentToTickMT(GetTickRequirement());
			}
		}
	}

	public Agent PreviousUserAgent { get; private set; }

	public GameEntityWithWorldPosition GameEntityWithWorldPosition { get; private set; }

	public virtual Agent MovingAgent { get; private set; }

	public List<Agent> DefendingAgents { get; private set; }

	public bool HasDefendingAgent
	{
		get
		{
			if (DefendingAgents != null)
			{
				return GetDefendingAgentCount() > 0;
			}
			return false;
		}
	}

	public virtual bool DisableCombatActionsOnUse => !IsInstantUse;

	public virtual bool LockUserFrames { get; set; }

	public virtual bool LockUserPositions { get; set; }

	public bool IsInstantUse { get; protected set; }

	public bool IsDeactivated
	{
		get
		{
			return _isDeactivated;
		}
		set
		{
			if (value == _isDeactivated)
			{
				return;
			}
			_isDeactivated = value;
			if (_isDeactivated && !GameNetwork.IsClientOrReplay)
			{
				UserAgent?.StopUsingGameObject();
				bool flag = false;
				while (HasAIMovingTo)
				{
					MovingAgent.StopUsingGameObject();
					flag = true;
				}
				while (HasDefendingAgent)
				{
					DefendingAgents[0].StopUsingGameObject();
					flag = true;
				}
				if (flag)
				{
					SetScriptComponentToTick(GetTickRequirement());
				}
			}
		}
	}

	public bool IsDisabledForPlayers
	{
		get
		{
			return _isDisabledForPlayers;
		}
		set
		{
			if (value != _isDisabledForPlayers)
			{
				_isDisabledForPlayers = value;
				if (_isDisabledForPlayers && !GameNetwork.IsClientOrReplay && UserAgent != null && !UserAgent.IsAIControlled)
				{
					UserAgent.StopUsingGameObject();
				}
			}
		}
	}

	public virtual WeakGameEntity InteractionEntity => base.GameEntity;

	public bool HasAIUser
	{
		get
		{
			if (HasUser)
			{
				return UserAgent.IsAIControlled;
			}
			return false;
		}
	}

	public bool HasUser => UserAgent != null;

	public virtual bool HasAIMovingTo => MovingAgent != null;

	public bool IsVisible
	{
		get
		{
			return base.GameEntity.IsVisibleIncludeParents();
		}
		set
		{
			base.GameEntity.SetVisibilityExcludeParents(value);
		}
	}

	protected UsableMissionObject(bool isInstantUse = false)
	{
		_components = new List<UsableMissionObjectComponent>();
		IsInstantUse = isInstantUse;
		GameEntityWithWorldPosition = null;
		_needsSingleThreadTickOnce = false;
	}

	public virtual void OnUserConversationStart()
	{
	}

	public virtual void OnUserConversationEnd()
	{
	}

	public void SetAreUserPositionsUpdatedInTheMachineTick(bool value)
	{
		_areUserPositionsUpdatedInTheMachineTick = value;
	}

	public bool GetIsUserPositionsUpdatedInTheMachineTick()
	{
		return _areUserPositionsUpdatedInTheMachineTick;
	}

	public void SetIsDeactivatedSynched(bool value)
	{
		if (IsDeactivated != value)
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetUsableMissionObjectIsDeactivated(base.Id, value));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
			IsDeactivated = value;
		}
	}

	public void SetIsDisabledForPlayersSynched(bool value)
	{
		if (IsDisabledForPlayers != value)
		{
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetUsableMissionObjectIsDisabledForPlayers(base.Id, value));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
			IsDisabledForPlayers = value;
		}
	}

	public virtual bool IsDisabledForAgent(Agent agent)
	{
		if (!IsDeactivated && agent.MountAgent == null && (!IsDisabledForPlayers || agent.IsAIControlled))
		{
			return !agent.IsAbleToUseMachine();
		}
		return true;
	}

	public void AddComponent(UsableMissionObjectComponent component)
	{
		_components.Add(component);
		component.OnAdded(base.Scene);
		SetScriptComponentToTick(GetTickRequirement());
	}

	public void RemoveComponent(UsableMissionObjectComponent component)
	{
		component.OnRemoved();
		_components.Remove(component);
		SetScriptComponentToTick(GetTickRequirement());
	}

	public T GetComponent<T>() where T : UsableMissionObjectComponent
	{
		return _components.Find((UsableMissionObjectComponent c) => c is T) as T;
	}

	private void CollectChildEntities()
	{
		CollectChildEntitiesAux(base.GameEntity);
	}

	private void CollectChildEntitiesAux(WeakGameEntity entity)
	{
		foreach (WeakGameEntity child in entity.GetChildren())
		{
			CollectChildEntity(child);
			if (child.GetScriptCount() == 0)
			{
				CollectChildEntitiesAux(child);
			}
		}
	}

	public void RefreshGameEntityWithWorldPosition()
	{
		GameEntityWithWorldPosition = new GameEntityWithWorldPosition(base.GameEntity);
	}

	protected virtual void CollectChildEntity(WeakGameEntity childEntity)
	{
	}

	protected virtual bool VerifyChildEntities(ref string errorMessage)
	{
		return true;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		CollectChildEntities();
		LockUserFrames = !IsInstantUse;
		RefreshGameEntityWithWorldPosition();
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		CollectChildEntities();
	}

	protected internal override void OnMissionReset()
	{
		base.OnMissionReset();
		foreach (UsableMissionObjectComponent component in _components)
		{
			component.OnMissionReset();
		}
	}

	public virtual void OnFocusGain(Agent userAgent)
	{
		foreach (UsableMissionObjectComponent component in _components)
		{
			component.OnFocusGain(userAgent);
		}
	}

	public virtual void OnFocusLose(Agent userAgent)
	{
		foreach (UsableMissionObjectComponent component in _components)
		{
			component.OnFocusLose(userAgent);
		}
	}

	public virtual TextObject GetInfoTextForBeingNotInteractable(Agent userAgent)
	{
		return TextObject.GetEmpty();
	}

	public virtual void SetUserForClient(Agent userAgent)
	{
		UserAgent?.SetUsedGameObjectForClient(null);
		UserAgent = userAgent;
		userAgent?.SetUsedGameObjectForClient(this);
	}

	public virtual void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		if (!GameNetwork.IsClientOrReplay)
		{
			if (!userAgent.IsAIControlled && HasAIUser)
			{
				UserAgent.StopUsingGameObject(isSuccessful: false);
			}
			if (IsAIMovingTo(userAgent))
			{
				userAgent.Formation?.Team.DetachmentManager.RemoveAgentAsMovingToDetachment(userAgent);
				RemoveMovingAgent(userAgent);
				SetScriptComponentToTick(GetTickRequirement());
			}
			while (HasAIMovingTo && !IsInstantUse)
			{
				MovingAgent.StopUsingGameObject(isSuccessful: false);
			}
			foreach (UsableMissionObjectComponent component in _components)
			{
				component.OnUse(userAgent);
			}
			UserAgent = userAgent;
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new UseObject(userAgent.Index, base.Id));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}
		else if (LockUserFrames)
		{
			WorldFrame userFrameForAgent = GetUserFrameForAgent(userAgent);
			userAgent.SetTargetPositionAndDirection(userFrameForAgent.Origin.AsVec2, in userFrameForAgent.Rotation.f);
		}
		else if (LockUserPositions)
		{
			userAgent.SetTargetPosition(GetUserFrameForAgent(userAgent).Origin.AsVec2);
		}
	}

	public virtual void OnAIMoveToUse(Agent userAgent, IDetachment detachment)
	{
		AddMovingAgent(userAgent);
		userAgent.Formation?.Team.DetachmentManager.AddAgentAsMovingToDetachment(userAgent, detachment);
		SetScriptComponentToTick(GetTickRequirement());
	}

	public virtual void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		foreach (UsableMissionObjectComponent component in _components)
		{
			component.OnUseStopped(userAgent, isSuccessful);
		}
		UserAgent = null;
	}

	public virtual void OnMoveToStopped(Agent movingAgent)
	{
		movingAgent.Formation?.Team.DetachmentManager.RemoveAgentAsMovingToDetachment(movingAgent);
		RemoveMovingAgent(movingAgent);
		SetScriptComponentToTick(GetTickRequirement());
	}

	public virtual int GetMovingAgentCount()
	{
		if (MovingAgent == null)
		{
			return 0;
		}
		return 1;
	}

	public virtual Agent GetMovingAgentWithIndex(int index)
	{
		return MovingAgent;
	}

	public virtual void RemoveMovingAgent(Agent movingAgent)
	{
		MovingAgent = null;
	}

	public virtual void AddMovingAgent(Agent movingAgent)
	{
		MovingAgent = movingAgent;
	}

	public void OnAIDefendBegin(Agent agent, IDetachment detachment)
	{
		AddDefendingAgent(agent);
		agent.Formation?.Team.DetachmentManager.AddAgentAsDefendingToDetachment(agent, detachment);
		SetScriptComponentToTick(GetTickRequirement());
	}

	public void OnAIDefendEnd(Agent agent)
	{
		agent.Formation?.Team.DetachmentManager.RemoveAgentAsDefendingToDetachment(agent);
		RemoveDefendingAgent(agent);
		SetScriptComponentToTick(GetTickRequirement());
	}

	public void InitializeDefendingAgents()
	{
		if (DefendingAgents == null)
		{
			DefendingAgents = new List<Agent>();
		}
	}

	public int GetDefendingAgentCount()
	{
		return DefendingAgents.Count;
	}

	public void AddDefendingAgent(Agent agent)
	{
		DefendingAgents.Add(agent);
	}

	public void RemoveDefendingAgent(Agent agent)
	{
		DefendingAgents.Remove(agent);
	}

	public bool IsAgentDefending(Agent agent)
	{
		return DefendingAgents.Contains(agent);
	}

	public virtual void SimulateTick(float dt)
	{
	}

	public override TickRequirement GetTickRequirement()
	{
		if (HasUser || HasAIMovingTo)
		{
			return base.GetTickRequirement() | TickRequirement.Tick | TickRequirement.TickParallel2;
		}
		if (HasDefendingAgent)
		{
			return base.GetTickRequirement() | TickRequirement.Tick;
		}
		foreach (UsableMissionObjectComponent component in _components)
		{
			if (component.IsOnTickRequired())
			{
				return base.GetTickRequirement() | TickRequirement.Tick;
			}
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTickParallel2(float dt)
	{
		for (int num = GetMovingAgentCount() - 1; num >= 0; num--)
		{
			if (!GetMovingAgentWithIndex(num).IsActive())
			{
				_needsSingleThreadTickOnce = true;
			}
		}
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		foreach (UsableMissionObjectComponent component in _components)
		{
			component.OnTick(dt);
		}
		if (!_areUserPositionsUpdatedInTheMachineTick && HasUser && HasUserPositionsChanged(UserAgent))
		{
			if (LockUserFrames)
			{
				WorldFrame userFrameForAgent = GetUserFrameForAgent(UserAgent);
				UserAgent.SetTargetPositionAndDirection(userFrameForAgent.Origin.AsVec2, in userFrameForAgent.Rotation.f);
			}
			else if (LockUserPositions)
			{
				UserAgent.SetTargetPosition(GetUserFrameForAgent(UserAgent).Origin.AsVec2);
			}
		}
		if (!_needsSingleThreadTickOnce)
		{
			return;
		}
		_needsSingleThreadTickOnce = false;
		for (int num = GetMovingAgentCount() - 1; num >= 0; num--)
		{
			Agent movingAgentWithIndex = GetMovingAgentWithIndex(num);
			if (!movingAgentWithIndex.IsActive())
			{
				movingAgentWithIndex.Formation?.Team.DetachmentManager.RemoveAgentAsMovingToDetachment(movingAgentWithIndex);
				RemoveMovingAgent(movingAgentWithIndex);
				SetScriptComponentToTick(GetTickRequirement());
			}
		}
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		foreach (UsableMissionObjectComponent component in _components)
		{
			component.OnEditorTick(dt);
		}
	}

	protected internal override void OnEditorValidate()
	{
		base.OnEditorValidate();
		foreach (UsableMissionObjectComponent component in _components)
		{
			component.OnEditorValidate();
		}
		string errorMessage = null;
		if (!VerifyChildEntities(ref errorMessage))
		{
			MBDebug.ShowWarning(errorMessage);
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		foreach (UsableMissionObjectComponent component in _components)
		{
			component.OnRemoved();
		}
	}

	public virtual WorldFrame GetUserFrameForAgent(Agent agent)
	{
		return GameEntityWithWorldPosition.WorldFrame;
	}

	public override string ToString()
	{
		string text = string.Concat(GetType(), " with Components:");
		foreach (UsableMissionObjectComponent component in _components)
		{
			text = string.Concat(text, "[", component, "]");
		}
		return text;
	}

	public virtual bool IsAIMovingTo(Agent agent)
	{
		return MovingAgent == agent;
	}

	public virtual bool HasUserPositionsChanged(Agent agent)
	{
		return base.GameEntity.GetHasFrameChanged();
	}

	public override void WriteToNetwork()
	{
		base.WriteToNetwork();
		GameNetworkMessage.WriteBoolToPacket(IsDeactivated);
		GameNetworkMessage.WriteBoolToPacket(IsDisabledForPlayers);
		GameNetworkMessage.WriteBoolToPacket(UserAgent != null);
		if (UserAgent != null)
		{
			GameNetworkMessage.WriteAgentIndexToPacket(UserAgent.Index);
		}
	}

	public virtual bool IsUsableByAgent(Agent userAgent)
	{
		return true;
	}

	public void SetCustomLocalFrame(in MatrixFrame customLocalFrame)
	{
		GameEntityWithWorldPosition.SetCustomLocalFrame(in customLocalFrame);
	}

	public override void OnEndMission()
	{
		UserAgent = null;
		for (int num = GetMovingAgentCount() - 1; num >= 0; num--)
		{
			RemoveMovingAgent(GetMovingAgentWithIndex(num));
		}
		if (HasDefendingAgent)
		{
			for (int num2 = GetDefendingAgentCount() - 1; num2 >= 0; num2--)
			{
				DefendingAgents.RemoveAt(num2);
			}
		}
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override void OnAfterReadFromNetwork((BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) synchedMissionObjectReadableRecord, bool allowVisibilityUpdate = true)
	{
		base.OnAfterReadFromNetwork(synchedMissionObjectReadableRecord, allowVisibilityUpdate);
		UsableMissionObjectRecord usableMissionObjectRecord = (UsableMissionObjectRecord)(object)synchedMissionObjectReadableRecord.Item2;
		IsDeactivated = usableMissionObjectRecord.IsDeactivated;
		IsDisabledForPlayers = usableMissionObjectRecord.IsDisabledForPlayers;
		if (usableMissionObjectRecord.IsUserAgentExists)
		{
			Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(usableMissionObjectRecord.AgentIndex);
			if (agentFromIndex != null)
			{
				SetUserForClient(agentFromIndex);
			}
		}
	}

	public abstract TextObject GetDescriptionText(WeakGameEntity gameEntity);
}
