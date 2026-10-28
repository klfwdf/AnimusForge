namespace TaleWorlds.MountAndBlade;

public struct MBMusicTrack
{
	private int index;

	private bool IsValid => index >= 0;

	public MBMusicTrack(MBMusicTrack obj)
	{
		index = obj.index;
	}

	internal MBMusicTrack(int i)
	{
		index = i;
	}

	public bool Equals(MBMusicTrack obj)
	{
		return index == obj.index;
	}

	public override int GetHashCode()
	{
		return index;
	}
}
