namespace TaleWorlds.Library;

public struct AreaInformation
{
	public float Temperature;

	public float Humidity;

	public void DeserializeFrom(IReader reader)
	{
		Temperature = reader.ReadFloat();
		Humidity = reader.ReadFloat();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(Temperature);
		writer.WriteFloat(Humidity);
	}
}
