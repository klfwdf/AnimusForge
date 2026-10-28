using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public struct CustomBattleId
{
	[JsonIgnore]
	public static CustomBattleId Empty = new CustomBattleId(Guid.Empty);

	[JsonProperty]
	public Guid Guid { get; private set; }

	public CustomBattleId(Guid guid)
	{
		Guid = guid;
	}

	public static CustomBattleId NewGuid()
	{
		return new CustomBattleId(Guid.NewGuid());
	}

	public override string ToString()
	{
		return Guid.ToString();
	}

	public byte[] ToByteArray()
	{
		return Guid.ToByteArray();
	}

	public static bool operator ==(CustomBattleId a, CustomBattleId b)
	{
		return a.Guid == b.Guid;
	}

	public static bool operator !=(CustomBattleId a, CustomBattleId b)
	{
		return a.Guid != b.Guid;
	}

	public override bool Equals(object o)
	{
		if (o != null && o is CustomBattleId customBattleId)
		{
			return Guid.Equals(customBattleId.Guid);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Guid.GetHashCode();
	}
}
