namespace TaleWorlds.Library;

public struct TimeInformation
{
	public float TimeOfDay;

	public float NightTimeFactor;

	public float DrynessFactor;

	public float WinterTimeFactor;

	public int Season;

	public void DeserializeFrom(IReader reader)
	{
		TimeOfDay = reader.ReadFloat();
		NightTimeFactor = reader.ReadFloat();
		DrynessFactor = reader.ReadFloat();
		WinterTimeFactor = reader.ReadFloat();
		Season = reader.ReadInt();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(TimeOfDay);
		writer.WriteFloat(NightTimeFactor);
		writer.WriteFloat(DrynessFactor);
		writer.WriteFloat(WinterTimeFactor);
		writer.WriteInt(Season);
	}
}
