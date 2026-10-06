using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class AttackEntityOrderSecondaryDetachment
{
	private readonly GameEntity _targetEntity;

	private readonly bool _surroundEntity;

	public AttackEntityOrderSecondaryDetachment(GameEntity targetEntity)
	{
		_targetEntity = targetEntity;
		_surroundEntity = _targetEntity.GetFirstScriptOfType<CastleGate>() == null;
	}

	public void TickOccasionally(Formation formation)
	{
		foreach (Agent allUnit in formation.Arrangement.GetAllUnits())
		{
			allUnit.SetScriptedTargetEntity(_targetEntity.WeakEntity, _surroundEntity ? Agent.AISpecialCombatModeFlags.SurroundAttackEntity : Agent.AISpecialCombatModeFlags.None, ignoreIfAlreadyAttacking: true);
		}
		foreach (Agent detachedUnit in formation.DetachedUnits)
		{
			if (detachedUnit.GetScriptedCombatFlags().HasAnyFlag(Agent.AISpecialCombatModeFlags.AttackEntity))
			{
				detachedUnit.DisableScriptedCombatMovement();
			}
		}
		foreach (Agent looseDetachedUnit in formation.LooseDetachedUnits)
		{
			if (looseDetachedUnit.GetScriptedCombatFlags().HasAnyFlag(Agent.AISpecialCombatModeFlags.AttackEntity))
			{
				looseDetachedUnit.DisableScriptedCombatMovement();
			}
		}
	}

	public void Disband(Formation formation)
	{
		foreach (Agent allUnit in formation.Arrangement.GetAllUnits())
		{
			if (allUnit.GetScriptedCombatFlags().HasAnyFlag(Agent.AISpecialCombatModeFlags.AttackEntity))
			{
				allUnit.DisableScriptedCombatMovement();
			}
		}
		foreach (Agent detachedUnit in formation.DetachedUnits)
		{
			if (detachedUnit.GetScriptedCombatFlags().HasAnyFlag(Agent.AISpecialCombatModeFlags.AttackEntity))
			{
				detachedUnit.DisableScriptedCombatMovement();
			}
		}
		foreach (Agent looseDetachedUnit in formation.LooseDetachedUnits)
		{
			if (looseDetachedUnit.GetScriptedCombatFlags().HasAnyFlag(Agent.AISpecialCombatModeFlags.AttackEntity))
			{
				looseDetachedUnit.DisableScriptedCombatMovement();
			}
		}
	}
}
