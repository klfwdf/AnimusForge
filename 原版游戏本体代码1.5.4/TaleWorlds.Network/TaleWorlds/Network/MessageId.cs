using System;

namespace TaleWorlds.Network;

public class MessageId : Attribute
{
	public byte Id { get; private set; }

	public MessageId(byte id)
	{
		Id = id;
	}
}
