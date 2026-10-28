using System;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class BattleServerLostMessage : Message
{
}
