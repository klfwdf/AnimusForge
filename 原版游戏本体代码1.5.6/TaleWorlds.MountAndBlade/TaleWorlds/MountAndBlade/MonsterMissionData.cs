using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class MonsterMissionData : IMonsterMissionData
{
	private MBActionSet _actionSet;

	private MBActionSet _femaleActionSet;

	public Monster Monster { get; private set; }

	public CapsuleData BodyCapsule => new CapsuleData(Monster.BodyCapsuleRadius, Monster.BodyCapsulePoint1, Monster.BodyCapsulePoint2);

	public CapsuleData CrouchedBodyCapsule => new CapsuleData(Monster.CrouchedBodyCapsuleRadius, Monster.CrouchedBodyCapsulePoint1, Monster.CrouchedBodyCapsulePoint2);

	public MBActionSet ActionSet
	{
		get
		{
			if (!_actionSet.IsValid && !string.IsNullOrEmpty(Monster.ActionSetCode))
			{
				_actionSet = MBActionSet.GetActionSet(Monster.ActionSetCode);
			}
			return _actionSet;
		}
	}

	public MBActionSet FemaleActionSet
	{
		get
		{
			if (!_femaleActionSet.IsValid && !string.IsNullOrEmpty(Monster.FemaleActionSetCode))
			{
				_femaleActionSet = MBActionSet.GetActionSet(Monster.FemaleActionSetCode);
			}
			return _femaleActionSet;
		}
	}

	public MonsterMissionData(Monster monster)
	{
		_actionSet = MBActionSet.InvalidActionSet;
		_femaleActionSet = MBActionSet.InvalidActionSet;
		Monster = monster;
	}
}
