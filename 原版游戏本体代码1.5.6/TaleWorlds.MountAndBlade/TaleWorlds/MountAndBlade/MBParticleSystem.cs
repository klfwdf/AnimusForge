namespace TaleWorlds.MountAndBlade;

public struct MBParticleSystem
{
	private int index;

	internal MBParticleSystem(int i)
	{
		index = i;
	}

	public bool Equals(MBParticleSystem a)
	{
		return index == a.index;
	}

	public override int GetHashCode()
	{
		return index;
	}
}
