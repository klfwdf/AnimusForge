using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public class AtmosphereGrid
{
	private struct AtmosphereStateSortData
	{
		public Vec3 Position;

		public int InitialIndex;
	}

	private List<AtmosphereState> states = new List<AtmosphereState>();

	public void Initialize()
	{
		states = Campaign.Current.MapSceneWrapper.GetAtmosphereStates().ToList();
	}

	public AtmosphereState GetInterpolatedStateInfo(Vec3 pos)
	{
		List<AtmosphereStateSortData> list = new List<AtmosphereStateSortData>();
		int num = 0;
		foreach (AtmosphereState state in states)
		{
			list.Add(new AtmosphereStateSortData
			{
				Position = state.Position,
				InitialIndex = num++
			});
		}
		pos.z *= 0.3f;
		list.Sort((AtmosphereStateSortData x, AtmosphereStateSortData y) => x.Position.Distance(pos).CompareTo(y.Position.Distance(pos)));
		AtmosphereState atmosphereState = new AtmosphereState();
		num = 0;
		float num2 = 0f;
		bool flag = true;
		string colorGradeTexture = (atmosphereState.ColorGradeTexture = "color_grade_empire_harsh");
		foreach (AtmosphereStateSortData item in list)
		{
			AtmosphereState atmosphereState2 = states[item.InitialIndex];
			float value = atmosphereState2.Position.Distance(pos);
			float num3 = 1f - MBMath.SmoothStep(atmosphereState2.distanceForMaxWeight, atmosphereState2.distanceForMinWeight, value);
			if (!((double)num3 < 0.001))
			{
				if (flag)
				{
					colorGradeTexture = atmosphereState2.ColorGradeTexture;
				}
				atmosphereState.HumidityAverage += atmosphereState2.HumidityAverage * num3;
				atmosphereState.HumidityVariance += atmosphereState2.HumidityVariance * num3;
				atmosphereState.TemperatureAverage += atmosphereState2.TemperatureAverage * num3;
				atmosphereState.TemperatureVariance += atmosphereState2.TemperatureVariance * num3;
				num2 += num3;
				flag = false;
			}
		}
		if (num2 > 0f)
		{
			atmosphereState.ColorGradeTexture = colorGradeTexture;
			atmosphereState.HumidityAverage /= num2;
			atmosphereState.HumidityVariance /= num2;
			atmosphereState.TemperatureAverage /= num2;
			atmosphereState.TemperatureVariance /= num2;
		}
		return atmosphereState;
	}
}
