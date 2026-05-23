using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000416 RID: 1046
	public class MapWeatherCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E1D RID: 3613
		// (get) Token: 0x06004262 RID: 16994 RVA: 0x0013F446 File Offset: 0x0013D646
		public WeatherNode[] AllWeatherNodes
		{
			get
			{
				return this._weatherNodes;
			}
		}

		// Token: 0x17000E1E RID: 3614
		// (get) Token: 0x06004263 RID: 16995 RVA: 0x0013F44E File Offset: 0x0013D64E
		private int DimensionSquared
		{
			get
			{
				return Campaign.Current.DefaultWeatherNodeDimension * Campaign.Current.DefaultWeatherNodeDimension;
			}
		}

		// Token: 0x06004264 RID: 16996 RVA: 0x0013F465 File Offset: 0x0013D665
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunchedEvent));
		}

		// Token: 0x06004265 RID: 16997 RVA: 0x0013F480 File Offset: 0x0013D680
		private void OnSessionLaunchedEvent(CampaignGameStarter obj)
		{
			this.InitializeTheBehavior();
			for (int i = 0; i < this.DimensionSquared; i++)
			{
				this.UpdateWeatherNodeWithIndex(i);
			}
		}

		// Token: 0x06004266 RID: 16998 RVA: 0x0013F4AB File Offset: 0x0013D6AB
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<int>("_lastUpdatedNodeIndex", ref this._lastUpdatedNodeIndex);
		}

		// Token: 0x06004267 RID: 16999 RVA: 0x0013F4C0 File Offset: 0x0013D6C0
		private void CreateAndShuffleDataIndicesDeterministic()
		{
			this._weatherNodeDataShuffledIndices = new int[this.DimensionSquared];
			for (int i = 0; i < this.DimensionSquared; i++)
			{
				this._weatherNodeDataShuffledIndices[i] = i;
			}
			MBFastRandom mbfastRandom = new MBFastRandom((uint)Campaign.Current.UniqueGameId.GetDeterministicHashCode());
			for (int j = 0; j < 20; j++)
			{
				for (int k = 0; k < this.DimensionSquared; k++)
				{
					int num = mbfastRandom.Next(this.DimensionSquared);
					int num2 = this._weatherNodeDataShuffledIndices[k];
					this._weatherNodeDataShuffledIndices[k] = this._weatherNodeDataShuffledIndices[num];
					this._weatherNodeDataShuffledIndices[num] = num2;
				}
			}
		}

		// Token: 0x06004268 RID: 17000 RVA: 0x0013F560 File Offset: 0x0013D760
		private void InitializeTheBehavior()
		{
			this.CreateAndShuffleDataIndicesDeterministic();
			this._weatherNodes = new WeatherNode[this.DimensionSquared];
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
					CampaignVec2 position = new CampaignVec2(pos, true);
					if (!position.IsValid())
					{
						position = new CampaignVec2(pos, false);
					}
					this._weatherNodes[i * defaultWeatherNodeDimension + j] = new WeatherNode(position);
				}
			}
			this.AddEventHandler();
		}

		// Token: 0x06004269 RID: 17001 RVA: 0x0013F628 File Offset: 0x0013D828
		private void AddEventHandler()
		{
			long numTicks = Campaign.Current.Models.MapWeatherModel.WeatherUpdateFrequency.NumTicks - CampaignTime.Now.NumTicks % Campaign.Current.Models.MapWeatherModel.WeatherUpdateFrequency.NumTicks;
			this._weatherTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(Campaign.Current.Models.MapWeatherModel.WeatherUpdateFrequency, new CampaignTime(numTicks));
			this._weatherTickEvent.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.WeatherUpdateTick));
		}

		// Token: 0x0600426A RID: 17002 RVA: 0x0013F6B9 File Offset: 0x0013D8B9
		private void WeatherUpdateTick(MBCampaignEvent campaignEvent, params object[] delegateParams)
		{
			this.UpdateWeatherNodeWithIndex(this._weatherNodeDataShuffledIndices[this._lastUpdatedNodeIndex]);
			this._lastUpdatedNodeIndex++;
			if (this._lastUpdatedNodeIndex == this._weatherNodes.Length)
			{
				this._lastUpdatedNodeIndex = 0;
			}
		}

		// Token: 0x0600426B RID: 17003 RVA: 0x0013F6F4 File Offset: 0x0013D8F4
		private void UpdateWeatherNodeWithIndex(int index)
		{
			WeatherNode weatherNode = this._weatherNodes[index];
			MapWeatherModel.WeatherEvent currentWeatherEvent = weatherNode.CurrentWeatherEvent;
			MapWeatherModel.WeatherEvent weatherEvent = Campaign.Current.Models.MapWeatherModel.UpdateWeatherForPosition(weatherNode.Position, CampaignTime.Now);
			MapWeatherModel.WeatherEventEffectOnTerrain weatherEffectOnTerrainForPosition = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(weatherNode.Position.ToVec2());
			if (currentWeatherEvent != weatherEvent || weatherEffectOnTerrainForPosition == MapWeatherModel.WeatherEventEffectOnTerrain.Wet)
			{
				weatherNode.SetVisualDirty();
				return;
			}
			if (currentWeatherEvent == MapWeatherModel.WeatherEvent.Clear && MBRandom.NondeterministicRandomFloat < 0.1f)
			{
				weatherNode.SetVisualDirty();
			}
		}

		// Token: 0x040012F6 RID: 4854
		private WeatherNode[] _weatherNodes;

		// Token: 0x040012F7 RID: 4855
		private MBCampaignEvent _weatherTickEvent;

		// Token: 0x040012F8 RID: 4856
		private int[] _weatherNodeDataShuffledIndices;

		// Token: 0x040012F9 RID: 4857
		private int _lastUpdatedNodeIndex;
	}
}
