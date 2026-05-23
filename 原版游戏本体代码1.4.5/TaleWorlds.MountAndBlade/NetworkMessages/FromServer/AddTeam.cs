using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NetworkMessages.FromServer;

[DefineGameNetworkMessageType(GameNetworkMessageSendType.FromServer)]
public sealed class AddTeam : GameNetworkMessage
{
	public int TeamIndex { get; private set; }

	public BattleSideEnum Side { get; private set; }

	public uint Color { get; private set; }

	public uint Color2 { get; private set; }

	public string BannerCode { get; private set; }

	public bool IsPlayerGeneral { get; private set; }

	public bool IsPlayerSergeant { get; private set; }

	public AddTeam(int teamIndex, BattleSideEnum side, uint color, uint color2, string bannerCode, bool isPlayerGeneral, bool isPlayerSergeant)
	{
		TeamIndex = teamIndex;
		Side = side;
		Color = color;
		Color2 = color2;
		BannerCode = bannerCode;
		IsPlayerGeneral = isPlayerGeneral;
		IsPlayerSergeant = isPlayerSergeant;
	}

	public AddTeam()
	{
	}

	protected override bool OnRead()
	{
		bool bufferReadValid = true;
		TeamIndex = GameNetworkMessage.ReadTeamIndexFromPacket(ref bufferReadValid);
		Side = (BattleSideEnum)GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref bufferReadValid);
		Color = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		Color2 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
		BannerCode = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
		IsPlayerGeneral = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		IsPlayerSergeant = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
		return bufferReadValid;
	}

	protected override void OnWrite()
	{
		GameNetworkMessage.WriteTeamIndexToPacket(TeamIndex);
		GameNetworkMessage.WriteIntToPacket((int)Side, CompressionMission.TeamSideCompressionInfo);
		GameNetworkMessage.WriteUintToPacket(Color, CompressionBasic.ColorCompressionInfo);
		GameNetworkMessage.WriteUintToPacket(Color2, CompressionBasic.ColorCompressionInfo);
		GameNetworkMessage.WriteStringToPacket(BannerCode);
		GameNetworkMessage.WriteBoolToPacket(IsPlayerGeneral);
		GameNetworkMessage.WriteBoolToPacket(IsPlayerSergeant);
	}

	protected override MultiplayerMessageFilter OnGetLogFilter()
	{
		return MultiplayerMessageFilter.Mission;
	}

	protected override string OnGetLogFormat()
	{
		return "Add team with side: " + Side;
	}
}
