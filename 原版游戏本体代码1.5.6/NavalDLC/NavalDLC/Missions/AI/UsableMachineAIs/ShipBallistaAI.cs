using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.AI.UsableMachineAIs;

public sealed class ShipBallistaAI : BallistaAI
{
	private bool _canAiUpdateAim = true;

	public bool IsUnderDirectControl { get; private set; }

	public ShipBallistaAI(Ballista ballista)
		: base(ballista)
	{
		IsUnderDirectControl = false;
	}

	protected override void UpdateAim(RangedSiegeWeapon rangedSiegeWeapon, float dt)
	{
		if (!IsUnderDirectControl && _canAiUpdateAim)
		{
			base.UpdateAim(rangedSiegeWeapon, dt);
		}
	}

	public void SetCanAiUpdateAim(bool canAiUpdateAim)
	{
		_canAiUpdateAim = canAiUpdateAim;
	}

	public void SetIsUnderDirectControl(bool value)
	{
		IsUnderDirectControl = value;
	}
}
