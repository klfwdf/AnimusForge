namespace TaleWorlds.MountAndBlade;

public struct MBSoundTrack
{
	private int index;

	internal MBSoundTrack(int i)
	{
		index = i;
	}

	public bool Equals(MBSoundTrack a)
	{
		return index == a.index;
	}

	public override int GetHashCode()
	{
		return index;
	}
}
