using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Missions.Objectives;

public abstract class MissionObjective
{
	public struct GenericMissionObjectiveBuilder
	{
		internal GenericMissionObjective Objective;

		public GenericMissionObjectiveBuilder SetName(TextObject name)
		{
			Objective.IName = name;
			return this;
		}

		public GenericMissionObjectiveBuilder SetDescription(TextObject description)
		{
			Objective.IDescription = description;
			return this;
		}

		public GenericMissionObjectiveBuilder SetObjectiveGiver(BasicCharacterObject objectiveGiver)
		{
			Objective.SetObjectiveGiver(objectiveGiver);
			return this;
		}

		public GenericMissionObjectiveBuilder SetInitialTargets(params MissionObjectiveTarget[] targets)
		{
			Objective.ClearTargets();
			if (targets != null)
			{
				for (int i = 0; i < targets.Length; i++)
				{
					Objective.AddTarget(targets[i]);
				}
			}
			return this;
		}

		public GenericMissionObjectiveBuilder SetIsActivationRequirementsMetCallback(Func<MissionObjective, bool> callback)
		{
			Objective.IsActivationRequirementsMetCallback = callback;
			return this;
		}

		public GenericMissionObjectiveBuilder SetIsCompletionRequirementsMetCallback(Func<MissionObjective, bool> callback)
		{
			Objective.IsCompletionRequirementsMetCallback = callback;
			return this;
		}

		public GenericMissionObjectiveBuilder SetOnStartCallback(Action<MissionObjective> callback)
		{
			Objective.OnStartCallback = callback;
			return this;
		}

		public GenericMissionObjectiveBuilder SetOnCompleteCallback(Action<MissionObjective> callback)
		{
			Objective.OnCompleteCallback = callback;
			return this;
		}

		public GenericMissionObjectiveBuilder SetOnTickCallback(Action<MissionObjective, float> callback)
		{
			Objective.OnTickCallback = callback;
			return this;
		}

		public GenericMissionObjectiveBuilder SetProgressCallback(Func<MissionObjective, MissionObjectiveProgressInfo> callback)
		{
			Objective.GetProgressCallback = callback;
			return this;
		}

		public MissionObjective Build()
		{
			return Objective;
		}
	}

	public struct GenericMissionObjectiveTargetBuilder<T>
	{
		internal GenericMissionObjectiveTarget<T> Target;

		public GenericMissionObjectiveTargetBuilder<T> SetIsActiveCallback(Func<T, bool> callback)
		{
			Target.IsActiveCallback = callback;
			return this;
		}

		public GenericMissionObjectiveTargetBuilder<T> SetGetGlobalPositionCallback(Func<T, Vec3> callback)
		{
			Target.GetGlobalPositionCallback = callback;
			return this;
		}

		public GenericMissionObjectiveTargetBuilder<T> SetGetNameCallback(Func<T, TextObject> callback)
		{
			Target.GetNameCallback = callback;
			return this;
		}

		public MissionObjectiveTarget<T> Build()
		{
			return Target;
		}
	}

	private MBList<MissionObjectiveTarget> _targets;

	private TextObject _cachedName;

	private TextObject _cachedDescription;

	public abstract string UniqueId { get; }

	public abstract TextObject Name { get; }

	public abstract TextObject Description { get; }

	public bool IsActive
	{
		get
		{
			if (IsStarted)
			{
				return !IsCompleted;
			}
			return false;
		}
	}

	public bool IsStarted { get; private set; }

	public bool IsCompleted { get; private set; }

	public Mission Mission { get; private set; }

	public BasicCharacterObject ObjectiveGiver { get; private set; }

	public event Action OnUpdated;

	public MissionObjective(Mission mission)
	{
		_targets = new MBList<MissionObjectiveTarget>();
		Mission = mission;
	}

	internal void Start()
	{
		if (IsStarted)
		{
			Debug.FailedAssert("Trying to start an objective that was already started.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjective.cs", "Start", 38);
			return;
		}
		if (IsCompleted)
		{
			Debug.FailedAssert("Trying to start a completed objective. This is not allowed, create a new objective instead.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjective.cs", "Start", 44);
			return;
		}
		IsStarted = true;
		OnStart();
	}

	internal void Tick(float dt)
	{
		CheckNameUpdates();
		OnTick(dt);
	}

	internal void Complete()
	{
		if (!IsStarted)
		{
			Debug.FailedAssert("Trying to complete an objective that was not started yet.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjective.cs", "Complete", 62);
			return;
		}
		if (IsCompleted)
		{
			Debug.FailedAssert("Trying to complete an objective more than once.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjective.cs", "Complete", 68);
			return;
		}
		IsCompleted = true;
		OnComplete();
	}

	private void CheckNameUpdates()
	{
		bool flag = false;
		if (_cachedName != Name)
		{
			_cachedName = Name;
			flag = true;
		}
		if (_cachedDescription != Description)
		{
			_cachedDescription = Description;
			flag = true;
		}
		if (flag)
		{
			this.OnUpdated?.Invoke();
		}
	}

	public virtual MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return default(MissionObjectiveProgressInfo);
	}

	internal bool GetIsActivationRequirementsMet()
	{
		return IsActivationRequirementsMet();
	}

	internal bool GetIsCompletionRequirementsMet()
	{
		return IsCompletionRequirementsMet();
	}

	public void SetObjectiveGiver(BasicCharacterObject objectiveGiver)
	{
		ObjectiveGiver = objectiveGiver;
		this.OnUpdated?.Invoke();
	}

	public void AddTarget(MissionObjectiveTarget target)
	{
		if (target == null)
		{
			Debug.FailedAssert("Cannot add null target to mission objective", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjective.cs", "AddTarget", 123);
		}
		else if (_targets.Contains(target))
		{
			Debug.FailedAssert("Trying to add target (" + target.GetName().ToString() + ") twice to mission objective (" + UniqueId + ")", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjective.cs", "AddTarget", 129);
		}
		else
		{
			_targets.Add(target);
			this.OnUpdated?.Invoke();
		}
	}

	public void RemoveTarget(MissionObjectiveTarget target)
	{
		if (target == null)
		{
			Debug.FailedAssert("Cannot remove null target from mission objective", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjective.cs", "RemoveTarget", 141);
		}
		else if (!_targets.Contains(target))
		{
			Debug.FailedAssert("Trying to remove non-existent target (" + target.GetName().ToString() + ") from objective (" + UniqueId + ")", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Objectives\\MissionObjective.cs", "RemoveTarget", 147);
		}
		else
		{
			_targets.Remove(target);
			this.OnUpdated?.Invoke();
		}
	}

	public void ClearTargets()
	{
		_targets.Clear();
		this.OnUpdated?.Invoke();
	}

	public MBReadOnlyList<MissionObjectiveTarget> GetTargetsCopy()
	{
		return _targets.ToMBList();
	}

	protected MBReadOnlyList<TTarget> GetTargetsCopy<TTarget>() where TTarget : MissionObjectiveTarget
	{
		MBList<TTarget> mBList = new MBList<TTarget>();
		for (int i = 0; i < _targets.Count; i++)
		{
			if (_targets[i] is TTarget item)
			{
				mBList.Add(item);
			}
		}
		return mBList;
	}

	protected virtual bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected virtual bool IsCompletionRequirementsMet()
	{
		return false;
	}

	protected virtual void OnStart()
	{
	}

	protected virtual void OnComplete()
	{
	}

	protected virtual void OnTick(float dt)
	{
	}

	protected virtual void OnTargetAdded(MissionObjectiveTarget target)
	{
	}

	protected virtual void OnTargetRemoved(MissionObjectiveTarget target)
	{
	}

	protected virtual void OnTargetsCleared()
	{
	}

	public static GenericMissionObjectiveBuilder CreateGenericObjectiveBuilder(Mission mission, string id, TextObject name = null, TextObject description = null)
	{
		GenericMissionObjective objective = new GenericMissionObjective(mission, id, name, description);
		return new GenericMissionObjectiveBuilder
		{
			Objective = objective
		};
	}

	public static GenericMissionObjectiveTargetBuilder<T> CreateGenericTargetBuilder<T>(T target, TextObject name, Vec3 staticPosition)
	{
		GenericMissionObjectiveTarget<T> genericMissionObjectiveTarget = new GenericMissionObjectiveTarget<T>(target);
		genericMissionObjectiveTarget.Name = name;
		genericMissionObjectiveTarget.StaticPosition = staticPosition;
		return new GenericMissionObjectiveTargetBuilder<T>
		{
			Target = genericMissionObjectiveTarget
		};
	}

	public static GenericMissionObjectiveTargetBuilder<T> CreateGenericTargetBuilder<T>(T target)
	{
		return CreateGenericTargetBuilder(target, null, Vec3.Invalid);
	}

	public static GenericMissionObjectiveTargetBuilder<T> CreateGenericTargetBuilder<T>(T target, TextObject name)
	{
		return CreateGenericTargetBuilder(target, name, Vec3.Invalid);
	}

	public static GenericMissionObjectiveTargetBuilder<T> CreateGenericTargetBuilder<T>(T target, Vec3 staticPosition)
	{
		return CreateGenericTargetBuilder(target, null, staticPosition);
	}
}
