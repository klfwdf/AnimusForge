using System.Collections.Generic;
using NavalDLC.Map;
using NavalDLC.View.Map.Visuals;
using SandBox.View;
using SandBox.View.Map;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;

namespace NavalDLC.View.Map.Managers;

public class StormVisualManager : EntityVisualManagerBase<Storm>
{
	private readonly List<StormVisual> _allStormVisuals;

	public static StormVisualManager Current => SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<StormVisualManager>();

	public override int Priority => 80;

	public StormVisualManager()
	{
		_allStormVisuals = new List<StormVisual>();
		NavalDLCEvents.OnStormCreatedEvent.AddNonSerializedListener(this, StormCreated);
		foreach (Storm spawnedStorm in NavalDLCManager.Instance.StormManager.SpawnedStorms)
		{
			StormVisual item = new StormVisual(spawnedStorm);
			_allStormVisuals.Add(item);
		}
	}

	private void StormCreated(Storm storm)
	{
		_allStormVisuals.Add(new StormVisual(storm));
	}

	public override MapEntityVisual<Storm> GetVisualOfEntity(Storm entity)
	{
		foreach (StormVisual allStormVisual in _allStormVisuals)
		{
			if (allStormVisual.MapEntity == entity)
			{
				return allStormVisual;
			}
		}
		return null;
	}

	public override void OnVisualTick(MapScreen screen, float realDt, float dt)
	{
		for (int num = _allStormVisuals.Count - 1; num >= 0; num--)
		{
			StormVisual stormVisual = _allStormVisuals[num];
			stormVisual.Tick();
			if (stormVisual.IsReadyToBeReleased)
			{
				_allStormVisuals.RemoveAt(num);
			}
		}
	}
}
