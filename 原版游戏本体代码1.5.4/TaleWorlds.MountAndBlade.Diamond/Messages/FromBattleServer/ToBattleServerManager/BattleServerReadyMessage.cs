using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.Library;

namespace Messages.FromBattleServer.ToBattleServerManager;

[Serializable]
[MessageDescription("BattleServer", "BattleServerManager", true)]
public class BattleServerReadyMessage : LoginMessage
{
	[JsonProperty]
	public ApplicationVersion ApplicationVersion { get; private set; }

	[JsonProperty]
	public string AssignedAddress { get; private set; }

	[JsonProperty]
	public ushort AssignedPort { get; private set; }

	[JsonProperty]
	public string Region { get; private set; }

	[JsonProperty]
	public sbyte Priority { get; private set; }

	[JsonProperty]
	public string Password { get; private set; }

	[JsonProperty]
	public string GameType { get; private set; }

	public BattleServerReadyMessage()
	{
	}

	public BattleServerReadyMessage(PeerId peerId, ApplicationVersion applicationVersion, string assignedAddress, ushort assignedPort, string region, sbyte priority, string password, string gameType)
		: base(peerId, null)
	{
		ApplicationVersion = applicationVersion;
		AssignedAddress = assignedAddress;
		AssignedPort = assignedPort;
		Region = region;
		Priority = priority;
		Password = password;
		GameType = gameType;
	}
}
