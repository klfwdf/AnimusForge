using NetworkMessages.FromServer;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MultiplayerMissionServerStatusVM : ViewModel
{
	private enum StatusTypes
	{
		Good,
		Average,
		Poor
	}

	private int _packetLossState;

	private int _pingState;

	private int _serverPerformanceState;

	[DataSourceProperty]
	public int PacketLossState
	{
		get
		{
			return _packetLossState;
		}
		set
		{
			if (value != _packetLossState)
			{
				_packetLossState = value;
				OnPropertyChangedWithValue(value, "PacketLossState");
			}
		}
	}

	[DataSourceProperty]
	public int PingState
	{
		get
		{
			return _pingState;
		}
		set
		{
			if (value != _pingState)
			{
				_pingState = value;
				OnPropertyChangedWithValue(value, "PingState");
			}
		}
	}

	[DataSourceProperty]
	public int ServerPerformanceState
	{
		get
		{
			return _serverPerformanceState;
		}
		set
		{
			if (value != _serverPerformanceState)
			{
				_serverPerformanceState = value;
				OnPropertyChangedWithValue(value, "ServerPerformanceState");
			}
		}
	}

	public void UpdatePacketLossRatio(float v)
	{
		if (v >= 0.02f)
		{
			PacketLossState = 2;
		}
		else if (v >= 0.01f)
		{
			PacketLossState = 1;
		}
		else
		{
			PacketLossState = 0;
		}
	}

	public void UpdatePeerPing(double averagePingInMilliseconds)
	{
		if (averagePingInMilliseconds >= 110.0)
		{
			PingState = 2;
		}
		else if (averagePingInMilliseconds >= 90.0)
		{
			PingState = 1;
		}
		else
		{
			PingState = 0;
		}
	}

	public void UpdateServerPerformanceState(ServerPerformanceState serverPerformanceState)
	{
		switch (serverPerformanceState)
		{
		default:
			ServerPerformanceState = 0;
			break;
		case NetworkMessages.FromServer.ServerPerformanceState.Medium:
			ServerPerformanceState = 1;
			break;
		case NetworkMessages.FromServer.ServerPerformanceState.Low:
		case NetworkMessages.FromServer.ServerPerformanceState.Count:
			ServerPerformanceState = 2;
			break;
		}
	}

	public void ResetStates()
	{
		PacketLossState = 0;
		PingState = 0;
		ServerPerformanceState = 0;
	}
}
