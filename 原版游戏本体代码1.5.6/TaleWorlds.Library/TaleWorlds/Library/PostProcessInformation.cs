namespace TaleWorlds.Library;

public struct PostProcessInformation
{
	public float MinExposure;

	public float MaxExposure;

	public float BrightpassThreshold;

	public float MiddleGray;

	public void DeserializeFrom(IReader reader)
	{
		MinExposure = reader.ReadFloat();
		MaxExposure = reader.ReadFloat();
		BrightpassThreshold = reader.ReadFloat();
		MiddleGray = reader.ReadFloat();
	}

	public void SerializeTo(IWriter writer)
	{
		writer.WriteFloat(MinExposure);
		writer.WriteFloat(MaxExposure);
		writer.WriteFloat(BrightpassThreshold);
		writer.WriteFloat(MiddleGray);
	}
}
