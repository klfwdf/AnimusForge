using System;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class ClanCreationFailedMessage : Message
{
}
