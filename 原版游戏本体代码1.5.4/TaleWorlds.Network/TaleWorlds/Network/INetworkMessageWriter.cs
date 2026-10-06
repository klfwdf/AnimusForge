using System;

namespace TaleWorlds.Network;

public interface INetworkMessageWriter
{
	void Write(string data);

	void Write(int data);

	void Write(short data);

	void Write(bool data);

	void Write(byte data);

	void Write(float data);

	void Write(long data);

	void Write(ulong data);

	void Write(Guid data);

	void Write(byte[] data);
}
