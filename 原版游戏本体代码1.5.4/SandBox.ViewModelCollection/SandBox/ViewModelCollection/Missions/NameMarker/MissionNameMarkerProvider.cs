using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker;

public abstract class MissionNameMarkerProvider
{
	private Action _onSetMarkersDirty;

	private bool _initialized;

	public MissionNameMarkerProvider()
	{
	}

	public abstract void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers);

	public void Initialize(Mission mission, Action onSetMarkersDirty)
	{
		OnInitialize(mission);
		_initialized = true;
		_onSetMarkersDirty = onSetMarkersDirty;
	}

	public void Destroy(Mission mission)
	{
		OnDestroy(mission);
		_initialized = false;
	}

	public void Tick(float dt)
	{
		OnTick(dt);
	}

	protected virtual void OnInitialize(Mission mission)
	{
	}

	protected virtual void OnDestroy(Mission mission)
	{
	}

	protected virtual void OnTick(float dt)
	{
	}

	protected void SetMarkersDirty()
	{
		_onSetMarkersDirty();
	}
}
