namespace TaleWorlds.MountAndBlade;

public struct MissionObjectId
{
	public readonly int Id;

	public readonly bool CreatedAtRuntime;

	public static readonly MissionObjectId Invalid = new MissionObjectId(-1);

	public MissionObjectId(int id, bool createdAtRuntime = false)
	{
		Id = id;
		CreatedAtRuntime = createdAtRuntime;
	}

	public static bool operator ==(MissionObjectId a, MissionObjectId b)
	{
		if (a.Id == b.Id)
		{
			return a.CreatedAtRuntime == b.CreatedAtRuntime;
		}
		return false;
	}

	public static bool operator !=(MissionObjectId a, MissionObjectId b)
	{
		if (a.Id == b.Id)
		{
			return a.CreatedAtRuntime != b.CreatedAtRuntime;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is MissionObjectId missionObjectId))
		{
			return false;
		}
		if (missionObjectId.Id == Id)
		{
			return missionObjectId.CreatedAtRuntime == CreatedAtRuntime;
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = Id;
		if (CreatedAtRuntime)
		{
			num |= 0x40000000;
		}
		return num.GetHashCode();
	}

	public override string ToString()
	{
		object obj = Id;
		bool createdAtRuntime = CreatedAtRuntime;
		return string.Concat(obj, " - ", createdAtRuntime.ToString());
	}
}
