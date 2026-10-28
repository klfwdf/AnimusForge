namespace TaleWorlds.Library;

public struct NauticalInformation
{
	public float WaveStrength;

	public Vec2 WindVector;

	public int CanUseLowAltitudeAtmosphere;

	public int UseSceneWindDirection;

	public int IsRiverBattle;

	public int IsInsideStorm;

	public int UsesNavalSimulatedWater;

	public void DeserializeFrom(IReader reader)
	{
		WaveStrength = reader.ReadFloat();
		WindVector = reader.ReadVec2();
		CanUseLowAltitudeAtmosphere = reader.ReadInt();
		UseSceneWindDirection = reader.ReadInt();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(WaveStrength);
		writer.WriteVec2(WindVector);
		writer.WriteInt(CanUseLowAltitudeAtmosphere);
		writer.WriteInt(UseSceneWindDirection);
	}
}
