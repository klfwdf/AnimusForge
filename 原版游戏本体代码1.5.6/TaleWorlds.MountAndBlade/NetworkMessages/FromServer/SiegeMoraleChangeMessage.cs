using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class SiegeMoraleChangeMessage : GameNetworkMessage
{
	public int AttackerMorale { get; private set; }

	public int DefenderMorale { get; private set; }

	public int[] CapturePointRemainingMoraleGains { get; private set; }

	public SiegeMoraleChangeMessage()
	{
	}

	public SiegeMoraleChangeMessage(int attackerMorale, int defenderMorale, int[] capturePointRemainingMoraleGains)
	{
		AttackerMorale = attackerMorale;
		DefenderMorale = defenderMorale;
		CapturePointRemainingMoraleGains = capturePointRemainingMoraleGains;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteIntToPacket(AttackerMorale, CompressionMission.SiegeMoraleCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(DefenderMorale, CompressionMission.SiegeMoraleCompressionInfo);
		int[] capturePointRemainingMoraleGains = CapturePointRemainingMoraleGains;
		for (int i = 0; i < capturePointRemainingMoraleGains.Length; i++)
		{
			GameNetworkMessage.WriteIntToPacket(capturePointRemainingMoraleGains[i], CompressionMission.SiegeMoralePerFlagCompressionInfo);
		}
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AttackerMorale = GameNetworkMessage.ReadIntFromPacket(CompressionMission.SiegeMoraleCompressionInfo, ref bufferReadValid);
		DefenderMorale = GameNetworkMessage.ReadIntFromPacket(CompressionMission.SiegeMoraleCompressionInfo, ref bufferReadValid);
		CapturePointRemainingMoraleGains = new int[7];
		for (int i = 0; i < CapturePointRemainingMoraleGains.Length; i++)
		{
			CapturePointRemainingMoraleGains[i] = GameNetworkMessage.ReadIntFromPacket(CompressionMission.SiegeMoralePerFlagCompressionInfo, ref bufferReadValid);
		}
		return bufferReadValid;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.GameMode;
	}

	protected override string OnGetLogFormat()
	{
		return "Morale synched. A: " + AttackerMorale + " D: " + DefenderMorale;
	}
}
