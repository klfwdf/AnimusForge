using System;

namespace TaleWorlds.Network;

public interface INetworkMessageReader
{
	int ReadInt32();

	short ReadInt16();

	bool ReadBoolean();

	byte ReadByte();

	string ReadString();

	float ReadFloat();

	long ReadInt64();

	ulong ReadUInt64();

	Guid ReadGuid();

	byte[] ReadByteArray();
}
