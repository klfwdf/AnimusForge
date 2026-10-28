using System;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
[DataContract]
[JsonConverter(typeof(PeerIdJsonConverter))]
public struct PeerId
{
	[DataMember]
	private readonly ulong _chunk1;

	[DataMember]
	private readonly ulong _chunk2;

	[DataMember]
	private readonly ulong _chunk3;

	[DataMember]
	private readonly ulong _chunk4;

	public bool IsValid
	{
		get
		{
			if (_chunk1 == 0L && _chunk2 == 0L && _chunk3 == 0L)
			{
				return _chunk4 != 0;
			}
			return true;
		}
	}

	public static PeerId Empty => new PeerId(0uL, 0uL, 0uL, 0uL);

	public PeerId(Guid guid)
	{
		byte[] value = guid.ToByteArray();
		_chunk1 = 0uL;
		_chunk2 = 0uL;
		_chunk3 = BitConverter.ToUInt64(value, 0);
		_chunk4 = BitConverter.ToUInt64(value, 8);
	}

	public PeerId(byte[] data)
	{
		_chunk1 = BitConverter.ToUInt64(data, 0);
		_chunk2 = BitConverter.ToUInt64(data, 8);
		_chunk3 = BitConverter.ToUInt64(data, 16);
		_chunk4 = BitConverter.ToUInt64(data, 24);
	}

	public PeerId(string peerIdAsString)
	{
		int num = peerIdAsString.Length * 2;
		byte[] array = new byte[(num < 32) ? 32 : num];
		Encoding.Unicode.GetBytes(peerIdAsString, 0, peerIdAsString.Length, array, 0);
		_chunk1 = BitConverter.ToUInt64(array, 0);
		_chunk2 = BitConverter.ToUInt64(array, 8);
		_chunk3 = BitConverter.ToUInt64(array, 16);
		_chunk4 = BitConverter.ToUInt64(array, 24);
	}

	public PeerId(ulong chunk1, ulong chunk2, ulong chunk3, ulong chunk4)
	{
		_chunk1 = chunk1;
		_chunk2 = chunk2;
		_chunk3 = chunk3;
		_chunk4 = chunk4;
	}

	public byte[] ToByteArray()
	{
		byte[] array = new byte[32];
		byte[] bytes = BitConverter.GetBytes(_chunk1);
		byte[] bytes2 = BitConverter.GetBytes(_chunk2);
		byte[] bytes3 = BitConverter.GetBytes(_chunk3);
		byte[] bytes4 = BitConverter.GetBytes(_chunk4);
		for (int i = 0; i < 8; i++)
		{
			array[i] = bytes[i];
			array[8 + i] = bytes2[i];
			array[16 + i] = bytes3[i];
			array[24 + i] = bytes4[i];
		}
		return array;
	}

	public override string ToString()
	{
		return _chunk1 + "." + _chunk2 + "." + _chunk3 + " ." + _chunk4;
	}

	public static PeerId FromString(string peerIdAsString)
	{
		string[] array = peerIdAsString.Split(new char[1] { '.' });
		return new PeerId(ulong.Parse(array[0]), ulong.Parse(array[1]), ulong.Parse(array[2]), ulong.Parse(array[3]));
	}

	public static bool operator ==(PeerId a, PeerId b)
	{
		if (a._chunk1 == b._chunk1 && a._chunk2 == b._chunk2 && a._chunk3 == b._chunk3)
		{
			return a._chunk4 == b._chunk4;
		}
		return false;
	}

	public static bool operator !=(PeerId a, PeerId b)
	{
		if (a._chunk1 == b._chunk1 && a._chunk2 == b._chunk2 && a._chunk3 == b._chunk3)
		{
			return a._chunk4 != b._chunk4;
		}
		return true;
	}

	public override bool Equals(object o)
	{
		if (o != null && o is PeerId peerId)
		{
			if (_chunk1 == peerId._chunk1 && _chunk2 == peerId._chunk2 && _chunk3 == peerId._chunk3)
			{
				return _chunk4 == peerId._chunk4;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		ulong chunk = _chunk1;
		int hashCode = chunk.GetHashCode();
		chunk = _chunk2;
		int hashCode2 = chunk.GetHashCode();
		chunk = _chunk3;
		int hashCode3 = chunk.GetHashCode();
		chunk = _chunk4;
		int hashCode4 = chunk.GetHashCode();
		return hashCode ^ hashCode2 ^ hashCode3 ^ hashCode4;
	}
}
