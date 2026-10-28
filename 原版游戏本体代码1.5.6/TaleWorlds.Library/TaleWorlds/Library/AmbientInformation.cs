namespace TaleWorlds.Library;

public struct AmbientInformation
{
	public float EnvironmentMultiplier;

	public Vec3 AmbientColor;

	public float MieScatterStrength;

	public float RayleighConstant;

	public void DeserializeFrom(IReader reader)
	{
		EnvironmentMultiplier = reader.ReadFloat();
		AmbientColor = reader.ReadVec3();
		MieScatterStrength = reader.ReadFloat();
		RayleighConstant = reader.ReadFloat();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(EnvironmentMultiplier);
		writer.WriteVec3(AmbientColor);
		writer.WriteFloat(MieScatterStrength);
		writer.WriteFloat(RayleighConstant);
	}
}
