namespace TaleWorlds.Library;

public struct SkyInformation
{
	public float Brightness;

	public void DeserializeFrom(IReader reader)
	{
		Brightness = reader.ReadFloat();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(Brightness);
	}
}
