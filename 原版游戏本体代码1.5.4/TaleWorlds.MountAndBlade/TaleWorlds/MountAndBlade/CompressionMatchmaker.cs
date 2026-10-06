namespace TaleWorlds.MountAndBlade;

public static class CompressionMatchmaker
{
	public static CompressionInfo.Integer KillDeathAssistCountCompressionInfo;

	public static CompressionInfo.Float MissionTimeCompressionInfo;

	public static CompressionInfo.Float MissionTimeLowPrecisionCompressionInfo;

	public static CompressionInfo.Integer MissionCurrentStateCompressionInfo;

	public static CompressionInfo.Integer ScoreCompressionInfo;

	static CompressionMatchmaker()
	{
		KillDeathAssistCountCompressionInfo = new CompressionInfo.Integer(-1000, 100000, maximumValueGiven: true);
		MissionTimeCompressionInfo = new CompressionInfo.Float(-5f, 86400f, 20);
		MissionTimeLowPrecisionCompressionInfo = new CompressionInfo.Float(-5f, 12, 4f);
		MissionCurrentStateCompressionInfo = new CompressionInfo.Integer(0, 6);
		ScoreCompressionInfo = new CompressionInfo.Integer(-1000000, 21);
	}
}
