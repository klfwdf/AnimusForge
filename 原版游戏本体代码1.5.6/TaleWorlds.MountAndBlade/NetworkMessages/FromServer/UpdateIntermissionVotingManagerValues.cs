using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class UpdateIntermissionVotingManagerValues : GameNetworkMessage
{
	public bool IsAutomatedBattleSwitchingEnabled { get; private set; }

	public bool IsMapVoteEnabled { get; private set; }

	public bool IsCultureVoteEnabled { get; private set; }

	public UpdateIntermissionVotingManagerValues()
	{
		IsAutomatedBattleSwitchingEnabled = MultiplayerIntermissionVotingManager.Instance.IsAutomatedBattleSwitchingEnabled;
		IsMapVoteEnabled = MultiplayerIntermissionVotingManager.Instance.IsMapVoteEnabled;
		IsCultureVoteEnabled = MultiplayerIntermissionVotingManager.Instance.IsCultureVoteEnabled;
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Administration;
	}

	protected override string OnGetLogFormat()
	{
		return $"IsAutomatedBattleSwitchingEnabled: {IsAutomatedBattleSwitchingEnabled}, IsMapVoteEnabled: {IsMapVoteEnabled}, IsCultureVoteEnabled: {IsCultureVoteEnabled}";
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		IsAutomatedBattleSwitchingEnabled = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsMapVoteEnabled = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsCultureVoteEnabled = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteBoolToPacket(IsAutomatedBattleSwitchingEnabled);
		GameNetworkMessage.WriteBoolToPacket(IsMapVoteEnabled);
		GameNetworkMessage.WriteBoolToPacket(IsCultureVoteEnabled);
	}
}
