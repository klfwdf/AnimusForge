namespace psai.net;

public class AudioData
{
	public string filePathRelativeToProjectDir;

	public string moduleId;

	public int sampleCountTotal;

	public int sampleCountPreBeat;

	public int sampleCountPostBeat;

	public int sampleRateHz;

	public float bpm;

	public int GetFullLengthInMilliseconds()
	{
		return (int)((long)sampleCountTotal * 1000L / sampleRateHz);
	}

	public int GetPreBeatZoneInMilliseconds()
	{
		return (int)((long)sampleCountPreBeat * 1000L / sampleRateHz);
	}

	public int GetPostBeatZoneInMilliseconds()
	{
		return (int)((long)sampleCountPostBeat * 1000L / sampleRateHz);
	}

	public int GetSampleCountByMilliseconds(int milliSeconds)
	{
		return (int)((long)sampleRateHz * (long)milliSeconds / 1000);
	}
}
