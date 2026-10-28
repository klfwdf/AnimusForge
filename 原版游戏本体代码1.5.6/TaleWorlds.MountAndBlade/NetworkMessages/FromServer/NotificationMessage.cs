using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class NotificationMessage : GameNetworkMessage
{
	public int Message { get; private set; }

	public int ParameterOne { get; private set; }

	public int ParameterTwo { get; private set; }

	private bool HasParameterOne => ParameterOne != -1;

	private bool HasParameterTwo => ParameterOne != -1;

	public NotificationMessage(int message, int param1, int param2)
	{
		Message = message;
		ParameterOne = param1;
		ParameterTwo = param2;
	}

	public NotificationMessage()
	{
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(Message, CompressionMission.MultiplayerNotificationCompressionInfo);
		GameNetworkMessage.WriteBoolToPacket(HasParameterOne);
		if (HasParameterOne)
		{
			GameNetworkMessage.WriteIntToPacket(ParameterOne, CompressionMission.MultiplayerNotificationParameterCompressionInfo);
			GameNetworkMessage.WriteBoolToPacket(HasParameterTwo);
			if (HasParameterTwo)
			{
				GameNetworkMessage.WriteIntToPacket(ParameterTwo, CompressionMission.MultiplayerNotificationParameterCompressionInfo);
			}
		}
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		int parameterOne = (ParameterTwo = -1);
		ParameterOne = parameterOne;
		Message = GameNetworkMessage.ReadIntFromPacket(CompressionMission.MultiplayerNotificationCompressionInfo, ref bufferReadValid);
		if (GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid))
		{
			ParameterOne = GameNetworkMessage.ReadIntFromPacket(CompressionMission.MultiplayerNotificationParameterCompressionInfo, ref bufferReadValid);
			if (GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid))
			{
				ParameterTwo = GameNetworkMessage.ReadIntFromPacket(CompressionMission.MultiplayerNotificationParameterCompressionInfo, ref bufferReadValid);
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
		return "Receiving message: " + Message + (HasParameterOne ? (" With first parameter: " + ParameterOne) : "") + (HasParameterTwo ? (" and second parameter: " + ParameterTwo) : "");
	}
}
