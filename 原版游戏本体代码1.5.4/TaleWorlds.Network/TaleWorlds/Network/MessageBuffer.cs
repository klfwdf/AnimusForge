using System;

namespace TaleWorlds.Network;

internal class MessageBuffer
{
	internal const int MessageHeaderSize = 4;

	internal byte[] Buffer { get; private set; }

	internal int DataLength { get; set; }

	internal MessageBuffer(byte[] buffer, int dataLength)
	{
		Buffer = buffer;
		DataLength = dataLength;
	}

	internal MessageBuffer(byte[] buffer)
	{
		Buffer = buffer;
	}

	internal string GetDebugText()
	{
		return BitConverter.ToString(Buffer, 0, DataLength);
	}
}
