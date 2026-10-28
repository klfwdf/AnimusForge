using System;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace TaleWorlds.PlayerServices;

[Serializable]
[JsonConverter(typeof(PlayerIdJsonConverter))]
public struct PlayerId : IComparable<PlayerId>, IEquatable<PlayerId>
{
	private byte _providedType;

	private byte _reserved1;

	private byte _reserved2;

	private byte _reserved3;

	private byte _reserved4;

	private byte _reserved5;

	private byte _reserved6;

	private byte _reserved7;

	private ulong _reservedBig;

	private ulong _id1;

	private ulong _id2;

	public ulong Id1 => _id1;

	public ulong Id2 => _id2;

	public bool IsValid
	{
		get
		{
			if (_id1 == 0L)
			{
				return _id2 != 0;
			}
			return true;
		}
	}

	public PlayerIdProvidedTypes ProvidedType => (PlayerIdProvidedTypes)_providedType;

	public ulong Part1 => _providedType | ((ulong)_reserved1 << 8) | ((ulong)_reserved2 << 16) | ((ulong)_reserved3 << 24) | ((ulong)_reserved4 << 32) | ((ulong)_reserved5 << 40) | ((ulong)_reserved6 << 48) | ((ulong)_reserved7 << 56);

	public ulong Part2 => _reservedBig;

	public ulong Part3 => _id1;

	public ulong Part4 => _id2;

	public static PlayerId Empty => new PlayerId(0, 0uL, 0uL, 0uL);

	public PlayerId(byte providedType, ulong id1, ulong id2)
	{
		_providedType = providedType;
		_reserved1 = 0;
		_reserved2 = 0;
		_reserved3 = 0;
		_reserved4 = 0;
		_reserved5 = 0;
		_reserved6 = 0;
		_reserved7 = 0;
		_reservedBig = 0uL;
		_id1 = id1;
		_id2 = id2;
	}

	public PlayerId(byte providedType, ulong reservedBig, ulong id1, ulong id2)
	{
		_providedType = providedType;
		_reserved1 = 0;
		_reserved2 = 0;
		_reserved3 = 0;
		_reserved4 = 0;
		_reserved5 = 0;
		_reserved6 = 0;
		_reserved7 = 0;
		_reservedBig = reservedBig;
		_id1 = id1;
		_id2 = id2;
	}

	public PlayerId(byte providedType, string guid)
	{
		byte[] value = Guid.Parse(guid).ToByteArray();
		_providedType = providedType;
		_reserved1 = 0;
		_reserved2 = 0;
		_reserved3 = 0;
		_reserved4 = 0;
		_reserved5 = 0;
		_reserved6 = 0;
		_reserved7 = 0;
		_reservedBig = 0uL;
		_id1 = BitConverter.ToUInt64(value, 0);
		_id2 = BitConverter.ToUInt64(value, 8);
	}

	public PlayerId(ulong part1, ulong part2, ulong part3, ulong part4)
	{
		byte[] bytes = BitConverter.GetBytes(part1);
		_providedType = bytes[0];
		_reserved1 = bytes[1];
		_reserved2 = bytes[2];
		_reserved3 = bytes[3];
		_reserved4 = bytes[4];
		_reserved5 = bytes[5];
		_reserved6 = bytes[6];
		_reserved7 = bytes[7];
		_reservedBig = part2;
		_id1 = part3;
		_id2 = part4;
	}

	public PlayerId(byte[] data)
	{
		_providedType = data[0];
		_reserved1 = data[1];
		_reserved2 = data[2];
		_reserved3 = data[3];
		_reserved4 = data[4];
		_reserved5 = data[5];
		_reserved6 = data[6];
		_reserved7 = data[7];
		_reservedBig = BitConverter.ToUInt64(data, 8);
		_id1 = BitConverter.ToUInt64(data, 16);
		_id2 = BitConverter.ToUInt64(data, 24);
	}

	public PlayerId(Guid guid)
	{
		byte[] value = guid.ToByteArray();
		_providedType = 0;
		_reserved1 = 0;
		_reserved2 = 0;
		_reserved3 = 0;
		_reserved4 = 0;
		_reserved5 = 0;
		_reserved6 = 0;
		_reserved7 = 0;
		_reservedBig = 0uL;
		_id1 = BitConverter.ToUInt64(value, 0);
		_id2 = BitConverter.ToUInt64(value, 8);
	}

	public byte[] ToByteArray()
	{
		byte[] array = new byte[32]
		{
			_providedType, _reserved1, _reserved2, _reserved3, _reserved4, _reserved5, _reserved6, _reserved7, 0, 0,
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0
		};
		byte[] bytes = BitConverter.GetBytes(_reservedBig);
		byte[] bytes2 = BitConverter.GetBytes(_id1);
		byte[] bytes3 = BitConverter.GetBytes(_id2);
		for (int i = 0; i < 8; i++)
		{
			array[8 + i] = bytes[i];
			array[16 + i] = bytes2[i];
			array[24 + i] = bytes3[i];
		}
		return array;
	}

	public void Serialize(IWriter writer)
	{
		writer.WriteULong(Part1);
		writer.WriteULong(Part2);
		writer.WriteULong(Part3);
		writer.WriteULong(Part4);
	}

	public void Deserialize(IReader reader)
	{
		ulong value = reader.ReadULong();
		ulong reservedBig = reader.ReadULong();
		ulong id = reader.ReadULong();
		ulong id2 = reader.ReadULong();
		byte[] bytes = BitConverter.GetBytes(value);
		_providedType = bytes[0];
		_reserved1 = bytes[1];
		_reserved2 = bytes[2];
		_reserved3 = bytes[3];
		_reserved4 = bytes[4];
		_reserved5 = bytes[5];
		_reserved6 = bytes[6];
		_reserved7 = bytes[7];
		_reservedBig = reservedBig;
		_id1 = id;
		_id2 = id2;
	}

	public override string ToString()
	{
		return $"{Part1}.{Part2}.{Part3}.{Part4}";
	}

	public static bool operator ==(PlayerId a, PlayerId b)
	{
		if (a.Part1 == b.Part1 && a.Part2 == b.Part2 && a.Part3 == b.Part3)
		{
			return a.Part4 == b.Part4;
		}
		return false;
	}

	public static bool operator !=(PlayerId a, PlayerId b)
	{
		if (a.Part1 == b.Part1 && a.Part2 == b.Part2 && a.Part3 == b.Part3)
		{
			return a.Part4 != b.Part4;
		}
		return true;
	}

	public override bool Equals(object o)
	{
		if (o != null && o is PlayerId playerId && this == playerId)
		{
			return true;
		}
		return false;
	}

	public override int GetHashCode()
	{
		int hashCode = Part1.GetHashCode();
		int hashCode2 = Part2.GetHashCode();
		int hashCode3 = Part3.GetHashCode();
		int hashCode4 = Part4.GetHashCode();
		return hashCode ^ hashCode2 ^ hashCode3 ^ hashCode4;
	}

	public static PlayerId FromString(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			string[] array = id.Split(new char[1] { '.' });
			ulong part = Convert.ToUInt64(array[0]);
			ulong part2 = Convert.ToUInt64(array[1]);
			ulong part3 = Convert.ToUInt64(array[2]);
			ulong part4 = Convert.ToUInt64(array[3]);
			return new PlayerId(part, part2, part3, part4);
		}
		return default(PlayerId);
	}

	public int CompareTo(PlayerId other)
	{
		if (Part1 != other.Part1)
		{
			return Part1.CompareTo(other.Part1);
		}
		if (Part2 != other.Part2)
		{
			return Part2.CompareTo(other.Part2);
		}
		if (Part3 != other.Part3)
		{
			return Part3.CompareTo(other.Part3);
		}
		return Part4.CompareTo(other.Part4);
	}

	public bool Equals(PlayerId other)
	{
		return this == other;
	}
}
