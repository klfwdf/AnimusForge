using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

[ScriptComponentParams("ship_visual_only", "")]
internal class BurningNode : ScriptComponentBehavior
{
	private const string LightEntityTag = "light_entity";

	private const string ParticleEntityTag = "particle_entity";

	[EditableScriptComponentVariable(true, "Node Index")]
	private int _nodeIndex = -1;

	private Light _light;

	private ParticleSystem _particle;

	private ParticleSystem _sparkParticle;

	private bool _lightEnabled;

	private bool _sparksEnabled;

	private float _currentFireProgress;

	public Vec2 SailStripLocation { get; private set; }

	public float ExternalFlameMultiplier { get; private set; }

	public float BurningTimer { get; set; }

	public int NodeIndex => _nodeIndex;

	public float CurrentFireProgress
	{
		get
		{
			return _currentFireProgress;
		}
		set
		{
			_currentFireProgress = MathF.Clamp(value, 0f, 1f);
		}
	}

	public BurningNode()
	{
		SailStripLocation = Vec2.Zero;
		ExternalFlameMultiplier = 1f;
		BurningTimer = 0f;
	}

	public void SetSailStripLocation(Vec2 sailStripLocation)
	{
		SailStripLocation = sailStripLocation;
	}

	public void SetExternalFlameMultiplier(float externalFlameMultiplier)
	{
		ExternalFlameMultiplier = externalFlameMultiplier;
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		FetchEntities();
	}

	protected override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		FetchEntities();
		TickAux();
	}

	protected override void OnInit()
	{
		base.OnInit();
		FetchEntities();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel;
	}

	protected override void OnTickParallel(float dt)
	{
		TickAux();
	}

	private void FetchEntities()
	{
		_light = null;
		_particle = null;
		WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("light_entity");
		if (firstChildEntityWithTag != null)
		{
			firstChildEntityWithTag.SetVisibilityExcludeParents(visible: true);
			_light = (Light)firstChildEntityWithTag.GetComponentAtIndex(0, TaleWorlds.Engine.GameEntity.ComponentType.Light);
		}
		WeakGameEntity firstChildEntityWithTag2 = base.GameEntity.GetFirstChildEntityWithTag("particle_entity");
		if (firstChildEntityWithTag2 != null)
		{
			firstChildEntityWithTag2.SetVisibilityExcludeParents(visible: true);
			_particle = (ParticleSystem)firstChildEntityWithTag2.GetComponentAtIndex(0, TaleWorlds.Engine.GameEntity.ComponentType.ParticleSystemInstanced);
		}
	}

	private void TickAux()
	{
		bool flag = _currentFireProgress > 0f;
		if (_particle != null)
		{
			_particle.SetEnable(flag);
		}
		if (_light != null)
		{
			_light.SetVisibility(flag && _lightEnabled);
		}
		if (_sparkParticle != null)
		{
			_sparkParticle.SetEnable(flag && _sparksEnabled);
		}
		if (flag)
		{
			if (_particle != null)
			{
				_particle.SetRuntimeEmissionRateMultiplier(_currentFireProgress * ExternalFlameMultiplier);
			}
			if (_sparkParticle != null)
			{
				_sparkParticle.SetRuntimeEmissionRateMultiplier(_currentFireProgress * ExternalFlameMultiplier);
			}
		}
	}

	public void EnableSparks()
	{
		_sparksEnabled = true;
		MatrixFrame boneLocalFrame = MatrixFrame.Identity;
		_sparkParticle = ParticleSystem.CreateParticleSystemAttachedToEntity("psys_dripping_flame", base.GameEntity, ref boneLocalFrame);
	}

	public void CheckWater()
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		float waterLevelAtPosition = base.GameEntity.GetWaterLevelAtPosition(globalFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false);
		if (globalFrame.origin.z < waterLevelAtPosition)
		{
			CurrentFireProgress = 0f;
		}
	}
}
