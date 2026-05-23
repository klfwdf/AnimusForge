using System.Text;
using TaleWorlds.Library;
using TaleWorlds.Network;

namespace TaleWorlds.Diamond.Socket;

[MessageId(1)]
public class SocketMessage : MessageContract
{
	public Message Message { get; private set; }

	public SocketMessage()
	{
	}

	public SocketMessage(Message message)
	{
		Message = message;
	}

	public override void SerializeToNetworkMessage(INetworkMessageWriter networkMessage)
	{
		byte[] array = Common.SerializeObjectAsJson(Message);
		networkMessage.Write(array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			networkMessage.Write(array[i]);
		}
	}

	public override void DeserializeFromNetworkMessage(INetworkMessageReader networkMessage)
	{
		byte[] array = new byte[networkMessage.ReadInt32()];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = networkMessage.ReadByte();
		}
		string json = Encoding.UTF8.GetString(array);
		Message = Common.DeserializeObjectFromJson<Message>(json);
	}
}
