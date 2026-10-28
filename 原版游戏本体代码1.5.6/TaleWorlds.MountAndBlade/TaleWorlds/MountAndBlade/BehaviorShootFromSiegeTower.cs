using System.Linq;

namespace TaleWorlds.MountAndBlade;

public class BehaviorShootFromSiegeTower : BehaviorComponent
{
	private SiegeTower _siegeTower;

	public BehaviorShootFromSiegeTower(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		_siegeTower = Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeTower>().FirstOrDefault((SiegeTower st) => st.WeaponSide == _behaviorSide);
	}

	public override void TickOccasionally()
	{
		base.TickOccasionally();
		if (base.Formation.AI.Side != _behaviorSide)
		{
			_behaviorSide = base.Formation.AI.Side;
			_siegeTower = Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeTower>().FirstOrDefault((SiegeTower st) => st.WeaponSide == _behaviorSide);
		}
		if (_siegeTower != null && !_siegeTower.IsDestroyed)
		{
			base.Formation.SetMovementOrder(base.CurrentOrder);
		}
	}

	protected override float GetAiWeight()
	{
		return 0f;
	}
}
