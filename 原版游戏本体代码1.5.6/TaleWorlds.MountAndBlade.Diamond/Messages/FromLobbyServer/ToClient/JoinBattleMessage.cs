using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class JoinBattleMessage : Message
{
	[JsonProperty]
	public BattleServerInformationForClient BattleServerInformation { get; private set; }

	public JoinBattleMessage()
	{
	}

	public JoinBattleMessage(BattleServerInformationForClient battleServerInformation)
	{
		BattleServerInformation = battleServerInformation;
	}
}
