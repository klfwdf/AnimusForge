using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class SendVoiceRecord : GameNetworkMessage
{
	public byte[] Buffer { get; private set; }

	public int BufferLength { get; private set; }

	public List<VirtualPlayer> ReceiverList { get; private set; }

	public bool HasReceiverList { get; private set; }

	public SendVoiceRecord()
	{
	}

	public SendVoiceRecord(byte[] buffer, int bufferLength)
	{
		Buffer = buffer;
		BufferLength = bufferLength;
	}

	public SendVoiceRecord(byte[] buffer, int bufferLength, List<VirtualPlayer> receiverList)
	{
		Buffer = buffer;
		BufferLength = bufferLength;
		ReceiverList = receiverList;
		HasReceiverList = true;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteByteArrayToPacket(Buffer, 0, BufferLength);
		int num = 0;
		if (ReceiverList != null)
		{
			num = ReceiverList.Count;
		}
		GameNetworkMessage.WriteBoolToPacket(HasReceiverList);
		GameNetworkMessage.WriteIntToPacket(num, CompressionBasic.PlayerCompressionInfo);
		for (int i = 0; i < num; i++)
		{
			GameNetworkMessage.WriteVirtualPlayerReferenceToPacket(ReceiverList[i]);
		}
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		Buffer = new byte[1440];
		BufferLength = GameNetworkMessage.ReadByteArrayFromPacket(Buffer, 0, 1440, ref bufferReadValid);
		HasReceiverList = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		int num = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref bufferReadValid);
		if (HasReceiverList)
		{
			ReceiverList = new List<VirtualPlayer>();
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					VirtualPlayer item = GameNetworkMessage.ReadVirtualPlayerReferenceToPacket(ref bufferReadValid);
					ReceiverList.Add(item);
				}
			}
		}
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.None;
	}

	protected override string OnGetLogFormat()
	{
		return string.Empty;
	}
}
