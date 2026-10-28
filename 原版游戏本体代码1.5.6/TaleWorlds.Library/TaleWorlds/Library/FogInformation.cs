namespace TaleWorlds.Library;

public struct FogInformation
{
	public float Density;

	public Vec3 Color;

	public float Falloff;

	public void DeserializeFrom(IReader reader)
	{
		Density = reader.ReadFloat();
		Color = reader.ReadVec3();
		Falloff = reader.ReadFloat();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(Density);
		writer.WriteVec3(Color);
		writer.WriteFloat(Falloff);
	}
}
