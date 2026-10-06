using System.Linq;
using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5CutLooseObjective : MissionObjective
{
	private class CutLooseObjectiveTarget : MissionObjectiveTarget
	{
		private readonly ShipAttachmentMachine _attachmentMachine;

		private readonly ShipAttachmentPointMachine _attachmentPointMachine;

		public CutLooseObjectiveTarget(ShipAttachmentMachine attachmentMachine)
		{
			_attachmentMachine = attachmentMachine;
		}

		public CutLooseObjectiveTarget(ShipAttachmentPointMachine attachmentPointMachine)
		{
			_attachmentPointMachine = attachmentPointMachine;
		}

		public override bool IsActive()
		{
			return !IsCutLoose();
		}

		public bool IsCutLoose()
		{
			if (_attachmentMachine != null)
			{
				return _attachmentMachine.CurrentAttachment == null;
			}
			if (_attachmentPointMachine != null)
			{
				return _attachmentPointMachine.CurrentAttachment == null;
			}
			return true;
		}

		public override Vec3 GetGlobalPosition()
		{
			if (_attachmentMachine != null)
			{
				return _attachmentMachine.GameEntity.GlobalPosition;
			}
			if (_attachmentPointMachine != null)
			{
				return _attachmentPointMachine.GameEntity.GlobalPosition;
			}
			return Vec3.Zero;
		}

		public override TextObject GetName()
		{
			return new TextObject("{=Cx5qU2jG}Ties");
		}
	}

	private readonly MBReadOnlyList<ShipAttachmentMachine> _attachmentMachines;

	private readonly MBReadOnlyList<ShipAttachmentPointMachine> _attachmentPointMachines;

	private MissionObjectiveProgressInfo _cachedProgress;

	public override string UniqueId => "naval_storyline_quest_5_cut_loose_objective";

	public override TextObject Name => new TextObject("{=1IpNoNL4}Cut Loose");

	public override TextObject Description => new TextObject("{=2cCuu7kv}Cut the prisoner ship loose, so you can sail it to safety.");

	public Quest5CutLooseObjective(Mission mission, MBReadOnlyList<ShipAttachmentMachine> attachmentMachines, MBReadOnlyList<ShipAttachmentPointMachine> attachmentPointMachines)
		: base(mission)
	{
		_attachmentMachines = attachmentMachines;
		_attachmentPointMachines = attachmentPointMachines;
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null)
			{
				CutLooseObjectiveTarget target = new CutLooseObjectiveTarget(attachmentMachine);
				AddTarget(target);
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null)
			{
				CutLooseObjectiveTarget target2 = new CutLooseObjectiveTarget(attachmentPointMachine);
				AddTarget(target2);
			}
		}
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		foreach (ShipAttachmentMachine attachmentMachine in _attachmentMachines)
		{
			if (attachmentMachine.CurrentAttachment != null)
			{
				continue;
			}
			foreach (StandingPoint standingPoint in attachmentMachine.StandingPoints)
			{
				standingPoint.IsDisabledForPlayers = true;
			}
		}
		foreach (ShipAttachmentPointMachine attachmentPointMachine in _attachmentPointMachines)
		{
			if (attachmentPointMachine.CurrentAttachment != null)
			{
				continue;
			}
			foreach (StandingPoint standingPoint2 in attachmentPointMachine.StandingPoints)
			{
				standingPoint2.IsDisabledForPlayers = true;
			}
		}
		MBReadOnlyList<CutLooseObjectiveTarget> targetsCopy = GetTargetsCopy<CutLooseObjectiveTarget>();
		_cachedProgress.RequiredProgressAmount = targetsCopy.Count;
		_cachedProgress.CurrentProgressAmount = targetsCopy.Count((CutLooseObjectiveTarget t) => t.IsCutLoose());
	}

	public override MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return _cachedProgress;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return _cachedProgress.CurrentProgressAmount == _cachedProgress.RequiredProgressAmount;
	}
}
