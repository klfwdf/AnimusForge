using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

internal class BurningSoundNode : ScriptComponentBehavior
{
	private const int MaxNumberOfCachedBurningNodes = 5;

	private const string _soundPath = "event:/mission/ambient/detail/fire/fire_dynamic";

	private const float FireRadius = 5f;

	private const float FireRadiusSq = 25f;

	private List<BurningNode> _burningNodesAttached = new List<BurningNode>();

	private bool _enabled;

	private float _burningSoundEventIntensityParam;

	private SoundEvent _burningSoundEvent;

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | TickRequirement.TickParallel2;
	}

	protected override void OnTick(float dt)
	{
		if (_enabled)
		{
			_burningSoundEvent.SetPosition(base.GameEntity.GlobalPosition);
			_burningSoundEvent.SetParameter("FireIntensity", _burningSoundEventIntensityParam);
		}
	}

	protected override void OnEditorTick(float dt)
	{
		if (_enabled)
		{
			_burningSoundEvent.SetPosition(base.GameEntity.GlobalPosition);
			float num = 0f;
			foreach (BurningNode item in _burningNodesAttached)
			{
				num += item.CurrentFireProgress;
			}
			_burningSoundEventIntensityParam = num;
			_burningSoundEvent.SetPosition(base.GameEntity.GlobalPosition);
			_burningSoundEvent.SetParameter("FireIntensity", _burningSoundEventIntensityParam);
		}
		base.GameEntity.IsSelectedOnEditor();
	}

	protected override void OnTickParallel2(float dt)
	{
		if (!_enabled)
		{
			return;
		}
		float num = 0f;
		foreach (BurningNode item in _burningNodesAttached)
		{
			num += item.CurrentFireProgress;
		}
		_burningSoundEventIntensityParam = num;
	}

	public void AddBurningNode(BurningNode node)
	{
		if (node.GameEntity.GlobalPosition.DistanceSquared(base.GameEntity.GlobalPosition) < 25f)
		{
			_burningNodesAttached.Add(node);
		}
	}

	public void StartFire()
	{
		_enabled = true;
		_burningSoundEvent = SoundEvent.CreateEventFromString("event:/mission/ambient/detail/fire/fire_dynamic", Mission.Current?.Scene);
		_burningSoundEvent.SetPosition(base.GameEntity.GlobalPosition);
		_burningSoundEvent.Play();
		_burningSoundEvent.SetParameter("FireIntensity", _burningSoundEventIntensityParam);
	}

	public void StopFire()
	{
		_enabled = false;
		_burningSoundEvent.Stop();
		_burningSoundEvent = null;
		_burningNodesAttached.Clear();
	}
}
