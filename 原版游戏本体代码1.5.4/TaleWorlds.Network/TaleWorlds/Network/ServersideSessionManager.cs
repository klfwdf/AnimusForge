using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TaleWorlds.Network;

public abstract class ServersideSessionManager
{
	public enum ThreadType
	{
		Single,
		MultipleIOAndListener,
		MultipleSeperateIOAndListener
	}

	private int _readWriteThreadCount = 1;

	private ThreadType _threadType;

	private ushort _listenPort;

	private TcpSocket _serverSocket;

	private int _lastUniqueClientId;

	private Thread _serverThread;

	private long _lastPeerAliveCheck;

	private List<ConcurrentQueue<IncomingServerSessionMessage>> _incomingMessages;

	private List<ConcurrentDictionary<int, ServersideSession>> _peers;

	private List<ConcurrentDictionary<int, ServersideSession>> _disconnectedPeers;

	private List<Thread> _readerThreads;

	private List<Thread> _writerThreads;

	private List<Thread> _singleThreads;

	public float PeerAliveCoeff { get; set; }

	protected ServersideSessionManager()
	{
		_readerThreads = new List<Thread>();
		_writerThreads = new List<Thread>();
		_singleThreads = new List<Thread>();
		_peers = new List<ConcurrentDictionary<int, ServersideSession>>();
		PeerAliveCoeff = 3f;
		_incomingMessages = new List<ConcurrentQueue<IncomingServerSessionMessage>>();
		_disconnectedPeers = new List<ConcurrentDictionary<int, ServersideSession>>();
	}

	public void Activate(ushort port, ThreadType threadType = ThreadType.Single, int readWriteThreadCount = 1)
	{
		_threadType = threadType;
		_readWriteThreadCount = readWriteThreadCount;
		_listenPort = port;
		_serverSocket = new TcpSocket();
		_serverSocket.Listen(_listenPort);
		if (_threadType == ThreadType.Single)
		{
			_peers.Add(new ConcurrentDictionary<int, ServersideSession>(_readWriteThreadCount * 3, 8192));
			_incomingMessages.Add(new ConcurrentQueue<IncomingServerSessionMessage>());
			_disconnectedPeers.Add(new ConcurrentDictionary<int, ServersideSession>());
			_readWriteThreadCount = 1;
			_serverThread = new Thread(ProcessSingle);
			_serverThread.IsBackground = true;
			_serverThread.Name = ToString() + " - Server Thread";
			_serverThread.Start();
			return;
		}
		for (int i = 0; i < _readWriteThreadCount; i++)
		{
			_peers.Add(new ConcurrentDictionary<int, ServersideSession>(_readWriteThreadCount * 4, 8192));
			_incomingMessages.Add(new ConcurrentQueue<IncomingServerSessionMessage>());
			_disconnectedPeers.Add(new ConcurrentDictionary<int, ServersideSession>());
		}
		for (int j = 0; j < _readWriteThreadCount; j++)
		{
			if (_threadType == ThreadType.MultipleSeperateIOAndListener)
			{
				Thread thread = new Thread(ProcessRead);
				thread.IsBackground = true;
				thread.Name = ToString() + " - Server Reader Thread - " + j;
				thread.IsBackground = true;
				thread.Start(j);
				Thread thread2 = new Thread(ProcessWriter);
				thread2.IsBackground = true;
				thread2.Name = ToString() + " - Server Writer Thread" + j;
				thread2.IsBackground = true;
				thread2.Start(j);
				_readerThreads.Add(thread);
				_writerThreads.Add(thread2);
			}
			else
			{
				Thread thread3 = new Thread(ProcessReaderWriter);
				thread3.Name = ToString() + " - Server ReaderWriter Thread - " + j;
				thread3.IsBackground = true;
				thread3.Start(j);
				_singleThreads.Add(thread3);
			}
		}
		_serverThread = new Thread(ProcessListener);
		_serverThread.IsBackground = true;
		_serverThread.Name = ToString() + " - Server Listener Thread";
		_serverThread.Start();
	}

	private void ProcessRead(object indexObject)
	{
		int index = (int)indexObject;
		TickManager.TickDelegate tickMethod = delegate
		{
			foreach (ServersideSession value in _peers[index].Values)
			{
				value?.Socket.ProcessRead();
			}
		};
		TickManager tickManager = new TickManager(5000, tickMethod);
		while (true)
		{
			tickManager.Tick();
		}
	}

	private void ProcessWriter(object indexObject)
	{
		int index = (int)indexObject;
		TickManager.TickDelegate tickMethod = delegate
		{
			foreach (ServersideSession value in _peers[index].Values)
			{
				value?.Socket.ProcessWrite();
			}
		};
		TickManager tickManager = new TickManager(5000, tickMethod);
		while (true)
		{
			tickManager.Tick();
		}
	}

	private void ProcessReaderWriter(object indexObject)
	{
		int index = (int)indexObject;
		TickManager.TickDelegate tickMethod = delegate
		{
			foreach (ServersideSession value in _peers[index].Values)
			{
				if (value != null)
				{
					value.Socket.ProcessWrite();
					value.Socket.ProcessRead();
				}
			}
		};
		TickManager tickManager = new TickManager(5000, tickMethod);
		while (true)
		{
			tickManager.Tick();
		}
	}

	private void ProcessListener()
	{
		TickManager.TickDelegate tickMethod = delegate
		{
			_serverSocket.CheckAcceptClient();
		};
		TickManager tickManager = new TickManager(500, tickMethod);
		while (true)
		{
			tickManager.Tick();
		}
	}

	private void ProcessSingle()
	{
		TickManager.TickDelegate tickMethod = delegate
		{
			foreach (ServersideSession value in _peers[0].Values)
			{
				if (value != null)
				{
					value.Socket.ProcessWrite();
					value.Socket.ProcessRead();
				}
			}
			_serverSocket.CheckAcceptClient();
		};
		TickManager tickManager = new TickManager(5000, tickMethod);
		while (true)
		{
			tickManager.Tick();
		}
	}

	private void RemovePeer(int peerNo)
	{
		ServersideSession value = null;
		if (_peers[peerNo % _readWriteThreadCount].TryRemove(peerNo, out value))
		{
			value.Socket.Close();
			value.OnDisconnected();
			OnRemoveConnection(value);
			_disconnectedPeers[peerNo % _readWriteThreadCount].TryRemove(value.Index, out value);
		}
	}

	public ServersideSession GetPeer(int peerIndex)
	{
		ServersideSession value = null;
		_peers[peerIndex % _readWriteThreadCount].TryGetValue(peerIndex, out value);
		return value;
	}

	public virtual void Tick()
	{
		IncomingConnectionsTick();
		MessagingTick();
		PeerAliveCheckTick();
		HandleRemovedPeersTick();
	}

	private void IncomingConnectionsTick()
	{
		TcpSocket lastIncomingConnection = _serverSocket.GetLastIncomingConnection();
		if (lastIncomingConnection != null)
		{
			ServersideSession serversideSession = OnNewConnection();
			_lastUniqueClientId++;
			serversideSession.InitializeSocket(_lastUniqueClientId, lastIncomingConnection);
			_peers[_lastUniqueClientId % _readWriteThreadCount].TryAdd(_lastUniqueClientId, serversideSession);
		}
	}

	private void MessagingTick()
	{
		foreach (ConcurrentQueue<IncomingServerSessionMessage> incomingMessage in _incomingMessages)
		{
			int count = incomingMessage.Count;
			for (int i = 0; i < count; i++)
			{
				IncomingServerSessionMessage result = null;
				incomingMessage.TryDequeue(out result);
				ServersideSession peer = result.Peer;
				NetworkMessage networkMessage = result.NetworkMessage;
				try
				{
					networkMessage.BeginRead();
					byte id = networkMessage.ReadByte();
					if (!peer.ContainsMessageHandler(id))
					{
						networkMessage.ResetRead();
						peer.OnMessageReceived(networkMessage);
					}
					else
					{
						MessageContract messageContract = MessageContract.CreateMessageContract(peer.GetMessageContractType(id));
						messageContract.DeserializeFromNetworkMessage(networkMessage);
						peer.HandleMessage(messageContract);
					}
				}
				catch (Exception)
				{
					RemovePeer(i);
				}
			}
		}
	}

	private void PeerAliveCheckTick()
	{
		if (Environment.TickCount <= _lastPeerAliveCheck + 3000)
		{
			return;
		}
		foreach (ConcurrentDictionary<int, ServersideSession> peer in _peers)
		{
			foreach (KeyValuePair<int, ServersideSession> item in peer)
			{
				int key = item.Key;
				ServersideSession value = item.Value;
				if ((double)((Environment.TickCount - value.Socket.LastMessageArrivalTime) / 1000) > (double)PeerAliveCoeff * 5.0)
				{
					RemovePeer(key);
				}
			}
		}
		_lastPeerAliveCheck = Environment.TickCount;
	}

	private void HandleRemovedPeersTick()
	{
		foreach (ConcurrentDictionary<int, ServersideSession> disconnectedPeer in _disconnectedPeers)
		{
			foreach (ServersideSession value in disconnectedPeer.Values)
			{
				RemovePeer(value.Index);
			}
		}
	}

	internal void AddIncomingMessage(IncomingServerSessionMessage incomingMessage)
	{
		_incomingMessages[incomingMessage.Peer.Index % _readWriteThreadCount].Enqueue(incomingMessage);
	}

	internal void AddDisconnectedPeer(ServersideSession peer)
	{
		_disconnectedPeers[peer.Index % _readWriteThreadCount].TryAdd(peer.Index, peer);
	}

	protected abstract ServersideSession OnNewConnection();

	protected abstract void OnRemoveConnection(ServersideSession peer);
}
