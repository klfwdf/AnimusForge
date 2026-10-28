using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

internal sealed class NetworkStatusReplicationComponent : UdpNetworkComponent
{
	private class NetworkStatusData
	{
		public float NextPingForceSendTime;

		public float NextPingTrySendTime;

		public int LastSentPingValue = -1;

		public float NextLossTrySendTime;

		public int LastSentLossValue;
	}

	private List<NetworkStatusData> _peerData = new List<NetworkStatusData>();

	private float _nextPerformanceStateTrySendTime;

	private ServerPerformanceState _lastSentPerformanceState;

	public override void OnUdpNetworkHandlerTick(float dt)
	{
		if (!GameNetwork.IsServer)
		{
			return;
		}
		float totalMissionTime = MBCommon.GetTotalMissionTime();
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			if (!networkPeer.IsSynchronized)
			{
				continue;
			}
			while (_peerData.Count <= networkPeer.Index)
			{
				NetworkStatusData item = new NetworkStatusData();
				_peerData.Add(item);
			}
			double f = networkPeer.RefreshAndGetAveragePingInMilliseconds();
			NetworkStatusData networkStatusData = _peerData[networkPeer.Index];
			bool flag = networkStatusData.NextPingForceSendTime <= totalMissionTime;
			if (flag || networkStatusData.NextPingTrySendTime <= totalMissionTime)
			{
				int num = MathF.Round(f);
				if (flag || networkStatusData.LastSentPingValue != num)
				{
					networkStatusData.LastSentPingValue = num;
					networkStatusData.NextPingForceSendTime = totalMissionTime + 10f + MBRandom.RandomFloatRanged(1.5f, 2.5f);
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new PingReplication(networkPeer, num));
					GameNetwork.EndBroadcastModuleEventUnreliable(GameNetwork.EventBroadcastFlags.None);
				}
				networkStatusData.NextPingTrySendTime = totalMissionTime + MBRandom.RandomFloatRanged(1.5f, 2.5f);
			}
			if (!networkPeer.IsServerPeer && networkStatusData.NextLossTrySendTime <= totalMissionTime)
			{
				networkStatusData.NextLossTrySendTime = totalMissionTime + MBRandom.RandomFloatRanged(1.5f, 2.5f);
				int num2 = (int)networkPeer.RefreshAndGetAverageLossPercent();
				if (networkStatusData.LastSentLossValue != num2)
				{
					networkStatusData.LastSentLossValue = num2;
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new LossReplicationMessage(num2));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
		if (_nextPerformanceStateTrySendTime <= totalMissionTime)
		{
			_nextPerformanceStateTrySendTime = totalMissionTime + MBRandom.RandomFloatRanged(1.5f, 2.5f);
			ServerPerformanceState serverPerformanceState = GetServerPerformanceState();
			if (serverPerformanceState != _lastSentPerformanceState)
			{
				_lastSentPerformanceState = serverPerformanceState;
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new ServerPerformanceStateReplicationMessage(serverPerformanceState));
				GameNetwork.EndBroadcastModuleEventUnreliable(GameNetwork.EventBroadcastFlags.None);
			}
		}
	}

	public NetworkStatusReplicationComponent()
	{
		if (GameNetwork.IsClientOrReplay)
		{
			AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
		}
	}

	public override void OnUdpNetworkHandlerClose()
	{
		base.OnUdpNetworkHandlerClose();
		if (GameNetwork.IsClientOrReplay)
		{
			AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
		}
	}

	private static void HandleServerMessagePingReplication(PingReplication message)
	{
		message.Peer?.SetAveragePingInMillisecondsAsClient(message.PingValue);
	}

	private static void HandleServerMessageLossReplication(LossReplicationMessage message)
	{
		if (GameNetwork.IsMyPeerReady)
		{
			GameNetwork.MyPeer.SetAverageLossPercentAsClient(message.LossValue);
		}
	}

	private static void HandleServerMessageServerPerformanceStateReplication(ServerPerformanceStateReplicationMessage message)
	{
		if (GameNetwork.IsMyPeerReady)
		{
			GameNetwork.MyPeer.SetServerPerformanceProblemStateAsClient(message.ServerPerformanceProblemState);
		}
	}

	private static void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode mode)
	{
		GameNetwork.NetworkMessageHandlerRegisterer networkMessageHandlerRegisterer = new GameNetwork.NetworkMessageHandlerRegisterer(mode);
		networkMessageHandlerRegisterer.Register<PingReplication>(HandleServerMessagePingReplication);
		networkMessageHandlerRegisterer.Register<LossReplicationMessage>(HandleServerMessageLossReplication);
		networkMessageHandlerRegisterer.Register<ServerPerformanceStateReplicationMessage>(HandleServerMessageServerPerformanceStateReplication);
	}

	private ServerPerformanceState GetServerPerformanceState()
	{
		if (Mission.Current != null)
		{
			float averageFps = Mission.Current.GetAverageFps();
			if (averageFps >= 50f)
			{
				return ServerPerformanceState.High;
			}
			if (averageFps >= 30f)
			{
				return ServerPerformanceState.Medium;
			}
			return ServerPerformanceState.Low;
		}
		return ServerPerformanceState.High;
	}
}
