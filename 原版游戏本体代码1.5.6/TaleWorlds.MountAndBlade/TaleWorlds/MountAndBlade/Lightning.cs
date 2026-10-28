using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class Lightning : ScriptComponentBehavior
{
	public float LightIntensity = 1000f;

	public float LightningRate = 5f;

	public float BoltSpeed = 1f;

	public float LightTravelRadius = 5f;

	public float BoltLife = 1f;

	public bool IsBoltEnabled = true;

	private float _boltTimer;

	private float _lightningTimer;

	private bool _spawnLightning;

	private float _boltIntensity = 1f;

	private int _currentNumberOfLightning;

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | base.GetTickRequirement();
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		_lightningTimer = MBRandom.RandomFloat * LightningRate;
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected internal override bool MovesEntity()
	{
		return false;
	}

	protected internal override void OnEditorTick(float dt)
	{
		TickAux(dt);
	}

	protected internal override void OnTick(float dt)
	{
		TickAux(dt);
	}

	private void SpawnBolt()
	{
		_boltIntensity = 1f;
		Random random = new Random();
		float x = (random.NextFloat() * 2f - 1f) * LightTravelRadius;
		float y = (random.NextFloat() * 2f - 1f) * LightTravelRadius;
		Vec3 localPosition = new Vec3(x, y);
		base.GameEntity.SetLocalPosition(localPosition);
		SoundManager.StartOneShotEvent("event:/map/ambient/node/thunder", base.GameEntity.GlobalPosition);
	}

	private void SetLightIntensity(float intensity)
	{
		Light light = base.GameEntity.GetComponentAtIndex(0, TaleWorlds.Engine.GameEntity.ComponentType.Light) as Light;
		Light light2 = base.GameEntity.GetComponentAtIndex(1, TaleWorlds.Engine.GameEntity.ComponentType.Light) as Light;
		float timeOfDay = base.Scene.TimeOfDay;
		if (light != null && light2 != null)
		{
			if (timeOfDay < 3.5f || timeOfDay > 21f)
			{
				float num = ((timeOfDay > 20f) ? MBMath.Map(timeOfDay, 20f, 24f, 4f, 1f) : MBMath.Map(timeOfDay, 0f, 6f, 1f, 4f));
				light.Intensity = LightIntensity * intensity * num;
				light2.Intensity = LightIntensity * intensity * num / 4f;
			}
			else
			{
				float num2 = ((timeOfDay < 12f) ? MBMath.Map(timeOfDay, 6f, 12f, 100f, 200f) : MBMath.Map(timeOfDay, 12f, 20f, 200f, 100f));
				light.Intensity = LightIntensity * intensity * num2;
				light2.Intensity = LightIntensity * intensity * num2 / 2f;
			}
		}
	}

	private float CalculateBoltIntensity(float dt)
	{
		_boltIntensity -= dt * BoltSpeed;
		if (_boltIntensity < 0f)
		{
			_boltIntensity = 0f;
		}
		return _boltIntensity;
	}

	private void TickAux(float dt)
	{
		_lightningTimer += dt;
		if (IsBoltEnabled)
		{
			base.GameEntity.ResumeParticleSystem(doChildren: true);
		}
		if (_lightningTimer > LightningRate)
		{
			_lightningTimer = 0f;
			_spawnLightning = true;
		}
		if (!_spawnLightning)
		{
			return;
		}
		_boltTimer += dt * BoltSpeed;
		SetLightIntensity(CalculateBoltIntensity(dt));
		if (_boltTimer > BoltLife)
		{
			if (IsBoltEnabled)
			{
				base.GameEntity.PauseParticleSystem(doChildren: false);
			}
			SpawnBolt();
			if (IsBoltEnabled)
			{
				base.GameEntity.BurstEntityParticle(doChildren: false);
			}
			SetLightIntensity(0f);
			_currentNumberOfLightning++;
			if (_currentNumberOfLightning == 2)
			{
				_spawnLightning = false;
				_currentNumberOfLightning = 0;
			}
			_boltTimer = 0f;
		}
	}
}
