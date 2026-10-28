namespace TaleWorlds.Network;

internal class IncomingServerSessionMessage
{
	internal NetworkMessage NetworkMessage { get; set; }

	internal ServersideSession Peer { get; set; }
}
