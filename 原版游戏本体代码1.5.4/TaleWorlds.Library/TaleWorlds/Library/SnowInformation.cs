namespace TaleWorlds.Library;

public struct SnowInformation
{
	public float Density;

	public void DeserializeFrom(IReader reader)
	{
		Density = reader.ReadFloat();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(Density);
	}
}
