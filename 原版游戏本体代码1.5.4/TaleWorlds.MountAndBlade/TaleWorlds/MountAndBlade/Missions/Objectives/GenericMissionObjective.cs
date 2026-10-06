using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Missions.Objectives;

internal class GenericMissionObjective : MissionObjective
{
	internal string IUniqueId;

	internal TextObject IName;

	internal TextObject IDescription;

	internal Func<MissionObjective, bool> IsActivationRequirementsMetCallback;

	internal Func<MissionObjective, bool> IsCompletionRequirementsMetCallback;

	internal Action<MissionObjective> OnStartCallback;

	internal Action<MissionObjective> OnCompleteCallback;

	internal Action<MissionObjective, float> OnTickCallback;

	internal Func<MissionObjective, MissionObjectiveProgressInfo> GetProgressCallback;

	private List<MissionObjectiveTarget> _targets;

	public override string UniqueId => IUniqueId;

	public override TextObject Name => IName;

	public override TextObject Description => IDescription;

	public GenericMissionObjective(Mission mission, string id, TextObject name, TextObject description)
		: base(mission)
	{
		_targets = new List<MissionObjectiveTarget>();
		IUniqueId = id;
		IName = name;
		IDescription = description;
	}

	public override MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return GetProgressCallback?.Invoke(this) ?? default(MissionObjectiveProgressInfo);
	}

	protected override bool IsActivationRequirementsMet()
	{
		if (IsActivationRequirementsMetCallback != null)
		{
			return IsActivationRequirementsMetCallback(this);
		}
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		if (IsCompletionRequirementsMetCallback != null)
		{
			return IsCompletionRequirementsMetCallback(this);
		}
		return true;
	}

	protected override void OnStart()
	{
		base.OnStart();
		OnStartCallback?.Invoke(this);
	}

	protected override void OnComplete()
	{
		base.OnComplete();
		OnCompleteCallback?.Invoke(this);
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		OnTickCallback?.Invoke(this, dt);
	}
}
