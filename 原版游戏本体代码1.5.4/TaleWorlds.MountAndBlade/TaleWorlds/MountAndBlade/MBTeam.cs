namespace TaleWorlds.MountAndBlade;

public struct MBTeam
{
	public readonly int Index;

	private readonly Mission _mission;

	public static MBTeam InvalidTeam => new MBTeam(null, -1);

	public bool IsValid => Index >= 0;

	internal MBTeam(Mission mission, int index)
	{
		_mission = mission;
		Index = index;
	}

	public override int GetHashCode()
	{
		return Index;
	}

	public override bool Equals(object obj)
	{
		return ((MBTeam)obj).Index == Index;
	}

	public static bool operator ==(MBTeam team1, MBTeam team2)
	{
		return team1.Index == team2.Index;
	}

	public static bool operator !=(MBTeam team1, MBTeam team2)
	{
		return team1.Index != team2.Index;
	}

	public bool IsEnemyOf(MBTeam otherTeam)
	{
		return MBAPI.IMBTeam.IsEnemy(_mission.Pointer, Index, otherTeam.Index);
	}

	public void SetIsEnemyOf(MBTeam otherTeam, bool isEnemyOf)
	{
		MBAPI.IMBTeam.SetIsEnemy(_mission.Pointer, Index, otherTeam.Index, isEnemyOf);
	}

	public override string ToString()
	{
		return "Mission Team: " + Index;
	}
}
