using System.Collections.Generic;
using NavalDLC.Missions.NavalPhysics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class NavalFloatsamLogic : MissionLogic
{
	private struct FloatSamRecord
	{
		internal GameEntity FloatsamEntity;

		internal Timer DeSpawnTimer;
	}

	private struct FadingOutRecord
	{
		internal GameEntity FloatsamEntity;

		internal Timer FadeOutTimer;
	}

	private const int MaxNumberOfFloatsam = 40;

	private const float MinFloatsamAliveDuration = 10f;

	private const float MaxFloatsamAliveDuration = 15f;

	private const float FadeOutDuration = 5f;

	private Queue<FloatSamRecord> _orderedEntities = new Queue<FloatSamRecord>();

	private Queue<FadingOutRecord> _fadingOutEntities = new Queue<FadingOutRecord>();

	public override void OnBehaviorInitialize()
	{
		Mission.Current.OnMissileRemovedEvent += OnMissileRemoved;
	}

	public override void AfterStart()
	{
	}

	public override void OnMissionTick(float dt)
	{
		CheckFloatsamTimers();
		TickFadingOutEntities();
	}

	protected override void OnEndMission()
	{
		_orderedEntities.Clear();
		_fadingOutEntities.Clear();
		_orderedEntities = null;
		_fadingOutEntities = null;
	}

	public void RegisterFloatsamInstance(GameEntity entity)
	{
		if (_orderedEntities.Count >= 40)
		{
			FloatSamRecord floatSamRecord = _orderedEntities.Dequeue();
			FadingOutRecord item = default(FadingOutRecord);
			item.FloatsamEntity = floatSamRecord.FloatsamEntity;
			item.FloatsamEntity.GetFirstScriptOfType<NavalDLC.Missions.NavalPhysics.NavalPhysics>().ForceSink();
			item.FadeOutTimer = new Timer(Mission.Current.CurrentTime, 5f);
			_fadingOutEntities.Enqueue(item);
		}
		FloatSamRecord item2 = new FloatSamRecord
		{
			FloatsamEntity = entity,
			DeSpawnTimer = new Timer(Mission.Current.CurrentTime, MBRandom.RandomFloatRanged(10f, 15f))
		};
		_orderedEntities.Enqueue(item2);
	}

	private void CheckFloatsamTimers()
	{
		while (_orderedEntities.Count > 0)
		{
			FloatSamRecord floatSamRecord = _orderedEntities.Peek();
			if (floatSamRecord.DeSpawnTimer.Check(Mission.Current.CurrentTime))
			{
				FadingOutRecord item = default(FadingOutRecord);
				item.FloatsamEntity = floatSamRecord.FloatsamEntity;
				item.FloatsamEntity.GetFirstScriptOfType<NavalDLC.Missions.NavalPhysics.NavalPhysics>().ForceSink();
				item.FadeOutTimer = new Timer(Mission.Current.CurrentTime, 5f);
				_fadingOutEntities.Enqueue(item);
				_orderedEntities.Dequeue();
				continue;
			}
			break;
		}
	}

	private void TickFadingOutEntities()
	{
		float currentTime = Mission.Current.CurrentTime;
		while (_fadingOutEntities.Count > 0)
		{
			FadingOutRecord fadingOutRecord = _fadingOutEntities.Peek();
			if (!fadingOutRecord.FadeOutTimer.Check(currentTime))
			{
				break;
			}
			if (fadingOutRecord.FloatsamEntity.HasScene())
			{
				Mission.Current.Scene.RemoveEntity(fadingOutRecord.FloatsamEntity, 35);
			}
			_fadingOutEntities.Dequeue();
		}
		foreach (FadingOutRecord fadingOutEntity in _fadingOutEntities)
		{
			float alpha = 1f - MBMath.ClampFloat((fadingOutEntity.FadeOutTimer.StartTime - currentTime) / 5f, 0f, 1f);
			fadingOutEntity.FloatsamEntity.SetAlpha(alpha);
		}
	}
}
