using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class RandomParticleSpawner : ScriptComponentBehavior
{
	private float _timeUntilNextParticleSpawn;

	public float spawnInterval = 3f;

	private void InitScript()
	{
		_timeUntilNextParticleSpawn = spawnInterval;
	}

	private void CheckSpawnParticle(float dt)
	{
		_timeUntilNextParticleSpawn -= dt;
		if (!(_timeUntilNextParticleSpawn <= 0f))
		{
			return;
		}
		int childCount = base.GameEntity.ChildCount;
		if (childCount > 0)
		{
			int index = MBRandom.RandomInt(childCount);
			WeakGameEntity child = base.GameEntity.GetChild(index);
			int componentCount = child.GetComponentCount(TaleWorlds.Engine.GameEntity.ComponentType.ParticleSystemInstanced);
			for (int i = 0; i < componentCount; i++)
			{
				((ParticleSystem)child.GetComponentAtIndex(i, TaleWorlds.Engine.GameEntity.ComponentType.ParticleSystemInstanced)).Restart();
			}
		}
		_timeUntilNextParticleSpawn += spawnInterval;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		InitScript();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		OnInit();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		CheckSpawnParticle(dt);
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		CheckSpawnParticle(dt);
	}

	protected internal override bool MovesEntity()
	{
		return true;
	}
}
