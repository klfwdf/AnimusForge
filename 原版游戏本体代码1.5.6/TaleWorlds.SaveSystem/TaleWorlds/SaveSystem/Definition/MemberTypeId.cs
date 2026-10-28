namespace TaleWorlds.SaveSystem.Definition;

public struct MemberTypeId
{
	public byte TypeLevel;

	public short LocalSaveId;

	public short SaveId => (short)((short)(TypeLevel << 8) + LocalSaveId);

	public static MemberTypeId Invalid => new MemberTypeId(0, -1);

	public override string ToString()
	{
		return "(" + TypeLevel + "," + LocalSaveId + ")";
	}

	public MemberTypeId(byte typeLevel, short localSaveId)
	{
		TypeLevel = typeLevel;
		LocalSaveId = localSaveId;
	}

	public override bool Equals(object obj)
	{
		if (obj is MemberTypeId memberTypeId)
		{
			if (memberTypeId.TypeLevel == TypeLevel)
			{
				return memberTypeId.LocalSaveId == LocalSaveId;
			}
			return false;
		}
		return false;
	}

	public static bool operator ==(MemberTypeId m1, MemberTypeId m2)
	{
		if ((object)m1 == null)
		{
			return (object)m2 == null;
		}
		return m1.Equals(m2);
	}

	public static bool operator !=(MemberTypeId m1, MemberTypeId m2)
	{
		return !(m1 == m2);
	}

	public override int GetHashCode()
	{
		return (17 * 31 + TypeLevel) * 31 + LocalSaveId;
	}
}
