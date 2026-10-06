namespace TaleWorlds.Network;

internal enum TcpStatus
{
	Connecting,
	WaitingDataLength,
	WaitingData,
	SocketClosed,
	DataReady,
	ConnectionClosed
}
