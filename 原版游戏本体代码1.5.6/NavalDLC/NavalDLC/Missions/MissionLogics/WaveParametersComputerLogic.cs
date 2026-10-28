using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class WaveParametersComputerLogic : MissionLogic
{
	public struct WaterParameters
	{
		public float Amplitude;

		public float Wavelength;

		public float WaveNumber;

		public float Omega;

		public float WaveMax;

		public float WaveMin;
	}

	public static WaterParameters AnalyzeHeightMap(Vec2 waveDirection, Scene scene)
	{
		waveDirection = waveDirection.Normalized();
		float num = float.MaxValue;
		float num2 = float.MinValue;
		float a = 0f;
		float b = 0f;
		List<float> list = new List<float>();
		float num3 = 0f;
		bool flag = false;
		float num4 = 0.15f;
		float num5 = 0f;
		Vec2 vec = new Vec2(a, b);
		float num6 = scene.GetWaterLevelAtPosition(vec, useWaterRenderer: true, checkWaterBodyEntities: false);
		float num7 = num6;
		for (int i = 0; i < 1000; i++)
		{
			vec += waveDirection * num4;
			Vec2 position = vec + waveDirection * num4;
			num7 = scene.GetWaterLevelAtPosition(vec, useWaterRenderer: true, checkWaterBodyEntities: false);
			float waterLevelAtPosition = scene.GetWaterLevelAtPosition(position, useWaterRenderer: true, checkWaterBodyEntities: false);
			if (num7 > num6 && num7 > waterLevelAtPosition)
			{
				if (flag)
				{
					float item = num5 - num3;
					list.Add(item);
					num3 = num5;
				}
				else
				{
					flag = true;
					num3 = num5;
				}
			}
			num6 = num7;
			num5 += num4;
			if (num7 < num)
			{
				num = num7;
			}
			if (num7 > num2)
			{
				num2 = num7;
			}
		}
		float num8 = 0f;
		if (list.Count >= 1)
		{
			float num9 = 0f;
			foreach (float item2 in list)
			{
				num9 += item2;
			}
			num8 = num9 / (float)list.Count;
		}
		else
		{
			num8 = 80f;
		}
		float amplitude = (num2 - num) * 0.5f;
		float num10 = System.MathF.PI * 2f / num8;
		float omega = TaleWorlds.Library.MathF.Sqrt(9.806f * num10);
		return new WaterParameters
		{
			Amplitude = amplitude,
			Wavelength = num8,
			WaveNumber = num10,
			Omega = omega,
			WaveMax = num2,
			WaveMin = num
		};
	}
}
