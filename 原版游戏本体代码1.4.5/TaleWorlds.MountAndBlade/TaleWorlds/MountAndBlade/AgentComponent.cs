using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class AgentComponent
{
	protected readonly Agent Agent;

	protected AgentComponent(Agent agent)
	{
		Agent = agent;
	}

	public virtual void Initialize()
	{
	}

	public virtual void OnTick(float dt)
	{
	}

	public virtual void OnTickParallel(float dt)
	{
	}

	public virtual float GetMoraleAddition()
	{
		return 0f;
	}

	public virtual float GetMoraleDecreaseConstant()
	{
		return 1f;
	}

	public virtual void OnItemPickup(SpawnedItemEntity item)
	{
	}

	public virtual void OnWeaponDrop(MissionWeapon droppedWeapon)
	{
	}

	public virtual void OnStopUsingGameObject()
	{
	}

	public virtual void OnWeaponHPChanged(ItemObject item, int hitPoints)
	{
	}

	public virtual void OnRetreating()
	{
	}

	public virtual void OnMount(Agent mount)
	{
	}

	public virtual void OnDismount(Agent mount)
	{
	}

	public virtual void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon, in Blow b, in AttackCollisionData collisionData)
	{
	}

	public virtual void OnDisciplineChanged()
	{
	}

	public virtual void OnAgentRemoved()
	{
	}

	public virtual void OnAgentTeleported()
	{
	}

	public virtual void OnAIInputSet(ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector)
	{
	}

	public virtual void OnComponentRemoved()
	{
	}

	public virtual void OnFormationSet()
	{
	}
}
