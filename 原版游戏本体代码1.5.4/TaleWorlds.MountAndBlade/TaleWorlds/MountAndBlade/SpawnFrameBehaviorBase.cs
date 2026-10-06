using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class SpawnFrameBehaviorBase
{
	private struct WeightCache
	{
		private const int Length = 3;

		private float _value1;

		private float _value2;

		private float _value3;

		private float this[int index]
		{
			get
			{
				return index switch
				{
					0 => _value1, 
					1 => _value2, 
					2 => _value3, 
					_ => throw new ArgumentOutOfRangeException(), 
				};
			}
			set
			{
				switch (index)
				{
				case 0:
					_value1 = value;
					break;
				case 1:
					_value2 = value;
					break;
				case 2:
					_value3 = value;
					break;
				}
			}
		}

		private WeightCache(float value1, float value2, float value3)
		{
			_value1 = value1;
			_value2 = value2;
			_value3 = value3;
		}

		public static WeightCache CreateDecreasingCache()
		{
			return new WeightCache(float.NaN, float.NaN, float.NaN);
		}

		public bool CheckAndInsertNewValueIfLower(float value, out float valueDifference)
		{
			int index = 0;
			for (int i = 1; i < 3; i++)
			{
				if (this[i] > this[index])
				{
					index = i;
				}
			}
			if (float.IsNaN(this[index]) || value < this[index])
			{
				valueDifference = (float.IsNaN(this[index]) ? TaleWorlds.Library.MathF.Abs(value) : (this[index] - value));
				this[index] = value;
				return true;
			}
			valueDifference = float.NaN;
			return false;
		}
	}

	private const string ExcludeMountedTag = "exclude_mounted";

	private const string ExcludeFootmenTag = "exclude_footmen";

	protected const string SpawnPointTag = "spawnpoint";

	public IEnumerable<GameEntity> SpawnPoints;

	public virtual void Initialize()
	{
		SpawnPoints = Mission.Current.Scene.FindEntitiesWithTag("spawnpoint");
	}

	public abstract MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn);

	protected MatrixFrame GetSpawnFrameFromSpawnPoints(IList<GameEntity> spawnPointsList, Team team, bool hasMount)
	{
		float num = float.MinValue;
		int index = -1;
		for (int i = 0; i < spawnPointsList.Count; i++)
		{
			float num2 = MBRandom.RandomFloat * 0.2f;
			float num3 = 0f;
			if (hasMount && spawnPointsList[i].HasTag("exclude_mounted"))
			{
				num2 -= 1000f;
			}
			if (!hasMount && spawnPointsList[i].HasTag("exclude_footmen"))
			{
				num2 -= 1000f;
			}
			WeightCache weightCache = WeightCache.CreateDecreasingCache();
			WeightCache weightCache2 = WeightCache.CreateDecreasingCache();
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent.IsMount)
				{
					continue;
				}
				float length = (agent.Position - spawnPointsList[i].GlobalPosition).Length;
				float num5;
				if (team == null || agent.Team.IsEnemyOf(team))
				{
					float num4 = 3.75f - length * 0.125f;
					num5 = TaleWorlds.Library.MathF.Tanh(num4 * num4) * -2f + 3.1f - length * 0.0125f - 1f / ((length + 0.0001f) * 0.05f);
				}
				else
				{
					float num6 = 1.8f - length * 0.1f;
					num5 = 0f - TaleWorlds.Library.MathF.Tanh(num6 * num6) + 1.7f - length * 0.01f - 1f / ((length + 0.0001f) * 0.1f);
				}
				float valueDifference2;
				if (num5 >= 0f)
				{
					if (weightCache.CheckAndInsertNewValueIfLower(num5, out var valueDifference))
					{
						num3 -= valueDifference;
					}
				}
				else if (weightCache2.CheckAndInsertNewValueIfLower(num5, out valueDifference2))
				{
					num3 -= valueDifference2;
				}
			}
			if (num3 > 0f)
			{
				num3 /= (float)Mission.Current.Agents.Count;
			}
			num2 += num3;
			if (num2 > num)
			{
				num = num2;
				index = i;
			}
		}
		MatrixFrame globalFrame = spawnPointsList[index].GetGlobalFrame();
		globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return globalFrame;
	}

	public void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
	}
}
