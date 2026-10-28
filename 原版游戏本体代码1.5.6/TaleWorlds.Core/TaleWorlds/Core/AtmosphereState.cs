using TaleWorlds.Library;

namespace TaleWorlds.Core;

public class AtmosphereState
{
	public Vec3 Position = Vec3.Zero;

	public float TemperatureAverage;

	public float TemperatureVariance;

	public float HumidityAverage;

	public float HumidityVariance;

	public float distanceForMaxWeight = 1f;

	public float distanceForMinWeight = 1f;

	public string ColorGradeTexture = "";

	public AtmosphereState()
	{
	}

	public AtmosphereState(Vec3 position, float tempAv, float tempVar, float humAv, float humVar, string colorGradeTexture)
	{
		Position = position;
		TemperatureAverage = tempAv;
		TemperatureVariance = tempVar;
		HumidityAverage = humAv;
		HumidityVariance = humVar;
		ColorGradeTexture = colorGradeTexture;
	}
}
