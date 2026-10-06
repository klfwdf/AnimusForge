namespace TaleWorlds.Library;

public struct SunInformation
{
	public float Altitude;

	public float Angle;

	public Vec3 Color;

	public float Brightness;

	public float MaxBrightness;

	public float Size;

	public float RayStrength;

	public void DeserializeFrom(IReader reader)
	{
		Altitude = reader.ReadFloat();
		Angle = reader.ReadFloat();
		Color = reader.ReadVec3();
		Brightness = reader.ReadFloat();
		MaxBrightness = reader.ReadFloat();
		Size = reader.ReadFloat();
		RayStrength = reader.ReadFloat();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(Altitude);
		writer.WriteFloat(Angle);
		writer.WriteVec3(Color);
		writer.WriteFloat(Brightness);
		writer.WriteFloat(MaxBrightness);
		writer.WriteFloat(Size);
		writer.WriteFloat(RayStrength);
	}
}
