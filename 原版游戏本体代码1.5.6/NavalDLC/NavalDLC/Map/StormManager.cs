using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Handlers;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Map;

public class StormManager : ICustomSystemManager
{
	[SaveableField(10)]
	private MBList<Storm> _spawnedStorms = new MBList<Storm>();

	public bool DebugVisualsEnabled;

	public bool DebugVisualsStopped;

	public MBReadOnlyList<Storm> SpawnedStorms => _spawnedStorms;

	public StormManager()
	{
		CampaignEvents.TickEvent.AddNonSerializedListener(this, CampaignTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
	}

	private void HourlyTick()
	{
		for (int i = 0; i < _spawnedStorms.Count; i++)
		{
			_spawnedStorms[i].HourlyTick();
		}
	}

	private void CampaignTick(float campaignDt)
	{
		if (!(campaignDt > 0f))
		{
			return;
		}
		for (int num = _spawnedStorms.Count - 1; num >= 0; num--)
		{
			Storm storm = _spawnedStorms[num];
			if (storm.IsReadyToBeFinalized)
			{
				storm.SetVisualDirty();
				_spawnedStorms.RemoveAt(num);
			}
			else
			{
				storm.Tick(campaignDt);
			}
		}
		StormCollisionTick();
	}

	private void StormCollisionTick()
	{
		for (int i = 0; i < _spawnedStorms.Count; i++)
		{
			for (int j = i + 1; j < _spawnedStorms.Count; j++)
			{
				Storm storm = _spawnedStorms[i];
				Storm storm2 = _spawnedStorms[j];
				if (storm.CurrentPosition.Distance(storm2.CurrentPosition) < storm.EffectRadius + storm2.EffectRadius)
				{
					((storm.EffectRadius > storm2.EffectRadius) ? storm2 : storm).ForceDeactivate();
				}
			}
		}
	}

	public void CreateStormAtPosition(Vec2 position)
	{
		Storm storm = new Storm(position, NavalDLCManager.Instance.GameModels.MapStormModel.GetSpawnedStormTypeForPosition(position));
		_spawnedStorms.Add(storm);
		NavalDLCEvents.Instance.OnStormCreated(storm);
	}

	public void CreateStormAtPosition(Vec2 position, Storm.StormTypes stormType)
	{
		Storm storm = new Storm(position, stormType);
		_spawnedStorms.Add(storm);
		NavalDLCEvents.Instance.OnStormCreated(storm);
	}

	public void OnAfterLoad()
	{
		CampaignEvents.TickEvent.AddNonSerializedListener(this, CampaignTick);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
		for (int i = 0; i < _spawnedStorms.Count; i++)
		{
			_spawnedStorms[i].OnAfterLoad();
		}
	}
}
