using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromClient;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromClient)]
public sealed class TeamChange : GameNetworkMessage
{
	public bool AutoAssign { get; private set; }

	public int TeamIndex { get; private set; }

	public TeamChange(bool autoAssign, int teamIndex)
	{
		AutoAssign = autoAssign;
		TeamIndex = teamIndex;
	}

	public TeamChange()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		AutoAssign = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		if (!AutoAssign)
		{
			TeamIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamCompressionInfo, ref bufferReadValid);
		}
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteBoolToPacket(AutoAssign);
		if (!AutoAssign)
		{
			GameNetworkMessage.WriteIntToPacket(TeamIndex, CompressionMission.TeamCompressionInfo);
		}
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Changed team to: " + TeamIndex;
	}
}
