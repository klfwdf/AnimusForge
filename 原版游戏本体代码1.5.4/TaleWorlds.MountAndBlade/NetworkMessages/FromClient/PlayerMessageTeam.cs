using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class PlayerMessageTeam : GameNetworkMessage
{
	public string Message { get; private set; }

	public List<VirtualPlayer> ReceiverList { get; private set; }

	public bool HasReceiverList { get; private set; }

	public PlayerMessageTeam(string message, List<VirtualPlayer> receiverList)
	{
		Message = message;
		ReceiverList = receiverList;
		HasReceiverList = true;
	}

	public PlayerMessageTeam(string message)
	{
		Message = message;
	}

	public PlayerMessageTeam()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteStringToPacket(Message);
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
		Message = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
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
		return MultiplayerMessageFilter.Messaging;
	}

	protected override string OnGetLogFormat()
	{
		return "Receiving Player message to team: " + Message;
	}
}
