using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class MapWeatherCampaignBehavior : CampaignBehaviorBase
{
	private WeatherNode[] _weatherNodes;

	private MBCampaignEvent _weatherTickEvent;

	private int[] _weatherNodeDataShuffledIndices;

	private int _lastUpdatedNodeIndex;

	public WeatherNode[] AllWeatherNodes => _weatherNodes;

	private int DimensionSquared => Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunchedEvent);
	}

	private void OnSessionLaunchedEvent(CampaignGameStarter obj)
	{
		InitializeTheBehavior();
		for (int i = 0; i < DimensionSquared; i++)
		{
			UpdateWeatherNodeWithIndex(i);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_lastUpdatedNodeIndex", ref _lastUpdatedNodeIndex);
	}

	private void CreateAndShuffleDataIndicesDeterministic()
	{
		_weatherNodeDataShuffledIndices = new int[DimensionSquared];
		for (int i = 0; i < DimensionSquared; i++)
		{
			_weatherNodeDataShuffledIndices[i] = i;
		}
		MBFastRandom mBFastRandom = new MBFastRandom((uint)Campaign.Current.UniqueGameId.GetDeterministicHashCode());
		for (int j = 0; j < 20; j++)
		{
			for (int k = 0; k < DimensionSquared; k++)
			{
				int num = mBFastRandom.Next(DimensionSquared);
				int num2 = _weatherNodeDataShuffledIndices[k];
				_weatherNodeDataShuffledIndices[k] = _weatherNodeDataShuffledIndices[num];
				_weatherNodeDataShuffledIndices[num] = num2;
			}
		}
	}

	private void InitializeTheBehavior()
	{
		CreateAndShuffleDataIndicesDeterministic();
		_weatherNodes = new WeatherNode[DimensionSquared];
		Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
		int defaultWeatherNodeDimension = Campaign.Current.DefaultWeatherNodeDimension;
		int num = defaultWeatherNodeDimension;
		int num2 = defaultWeatherNodeDimension;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float a = (float)i / (float)defaultWeatherNodeDimension * terrainSize.X;
				float b = (float)j / (float)defaultWeatherNodeDimension * terrainSize.Y;
				Vec2 pos = new Vec2(a, b);
				CampaignVec2 position = new CampaignVec2(pos, isOnLand: true);
				if (!position.IsValid())
				{
					position = new CampaignVec2(pos, isOnLand: false);
				}
				_weatherNodes[i * defaultWeatherNodeDimension + j] = new WeatherNode(position);
			}
		}
		AddEventHandler();
	}

	private void AddEventHandler()
	{
		long numTicks = Campaign.Current.Models.MapWeatherModel.WeatherUpdateFrequency.NumTicks - CampaignTime.Now.NumTicks % Campaign.Current.Models.MapWeatherModel.WeatherUpdateFrequency.NumTicks;
		_weatherTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(Campaign.Current.Models.MapWeatherModel.WeatherUpdateFrequency, new CampaignTime(numTicks));
		_weatherTickEvent.AddHandler(WeatherUpdateTick);
	}

	private void WeatherUpdateTick(MBCampaignEvent campaignEvent, params object[] delegateParams)
	{
		UpdateWeatherNodeWithIndex(_weatherNodeDataShuffledIndices[_lastUpdatedNodeIndex]);
		_lastUpdatedNodeIndex++;
		if (_lastUpdatedNodeIndex == _weatherNodes.Length)
		{
			_lastUpdatedNodeIndex = 0;
		}
	}

	private void UpdateWeatherNodeWithIndex(int index)
	{
		WeatherNode weatherNode = _weatherNodes[index];
		MapWeatherModel.WeatherEvent currentWeatherEvent = weatherNode.CurrentWeatherEvent;
		MapWeatherModel.WeatherEvent weatherEvent = Campaign.Current.Models.MapWeatherModel.UpdateWeatherForPosition(weatherNode.Position, CampaignTime.Now);
		MapWeatherModel.WeatherEventEffectOnTerrain weatherEffectOnTerrainForPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(weatherNode.Position.ToVec2());
		if (currentWeatherEvent != weatherEvent || weatherEffectOnTerrainForPosition == MapWeatherModel.WeatherEventEffectOnTerrain.Wet)
		{
			weatherNode.SetVisualDirty();
		}
		else if (currentWeatherEvent == MapWeatherModel.WeatherEvent.Clear && MBRandom.NondeterministicRandomFloat < 0.1f)
		{
			weatherNode.SetVisualDirty();
		}
	}
}
