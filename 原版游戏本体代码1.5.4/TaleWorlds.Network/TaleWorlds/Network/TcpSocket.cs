using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TaleWorlds.Library;

namespace TaleWorlds.Network;

internal class TcpSocket
{
	public const int MaxMessageSize = 16777216;

	internal const int PeerAliveCode = -1234;

	internal const int DisconnectCode = -9999;

	private int _uniqueSocketId;

	private static int _socketCount;

	private Socket _dotNetSocket;

	private SocketAsyncEventArgs _socketAsyncEventArgsWrite;

	private SocketAsyncEventArgs _socketAsyncEventArgsListener;

	private SocketAsyncEventArgs _socketAsyncEventArgsRead;

	internal MessageBuffer LastReadMessage;

	private ConcurrentQueue<MessageBuffer> _writeNetworkMessageQueue;

	private MessageBuffer _currentlySendingMessage;

	private bool _currentlyAcceptingClients;

	private ConcurrentQueue<TcpSocket> _incomingConnections;

	private int _lastMessageTotalRead;

	private TcpStatus _status;

	private bool _processingReceive;

	private string _remoteEndComputerName;

	private readonly MessageBuffer _peerAliveMessageBuffer;

	private readonly MessageBuffer _disconnectMessageBuffer;

	internal int LastMessageSentTime { get; set; }

	internal int LastMessageArrivalTime { get; set; }

	internal TcpStatus Status
	{
		get
		{
			return _status;
		}
		private set
		{
			if (value == TcpStatus.ConnectionClosed && _status != value && this.Closed != null)
			{
				this.Closed();
			}
			_status = value;
		}
	}

	internal string RemoteEndComputerName
	{
		get
		{
			if (_remoteEndComputerName == "")
			{
				try
				{
					_remoteEndComputerName = Dns.GetHostEntry(((IPEndPoint)_dotNetSocket.RemoteEndPoint).Address).HostName;
				}
				catch (Exception)
				{
					_remoteEndComputerName = "Unknown";
				}
			}
			return _remoteEndComputerName;
		}
	}

	internal string IPAddress => ((IPEndPoint)_dotNetSocket.RemoteEndPoint).Address.ToString();

	internal bool IsConnected
	{
		get
		{
			if ((Status == TcpStatus.DataReady || Status == TcpStatus.WaitingDataLength || Status == TcpStatus.WaitingData) && (_dotNetSocket == null || !_dotNetSocket.Connected))
			{
				Status = TcpStatus.ConnectionClosed;
			}
			if (Status != TcpStatus.SocketClosed && Status != TcpStatus.ConnectionClosed)
			{
				return Status != TcpStatus.Connecting;
			}
			return false;
		}
	}

	internal event TcpMessageReceiverDelegate MessageReceived;

	internal event TcpCloseDelegate Closed;

	internal TcpSocket()
	{
		_uniqueSocketId = _socketCount;
		_socketCount++;
		LastReadMessage = null;
		LastMessageSentTime = 0;
		_writeNetworkMessageQueue = new ConcurrentQueue<MessageBuffer>();
		_socketAsyncEventArgsWrite = new SocketAsyncEventArgs();
		_socketAsyncEventArgsWrite.Completed += ProcessIO;
		_socketAsyncEventArgsRead = new SocketAsyncEventArgs();
		_socketAsyncEventArgsRead.Completed += ProcessIO;
		_socketAsyncEventArgsListener = new SocketAsyncEventArgs();
		_socketAsyncEventArgsListener.Completed += ProcessIO;
		byte[] array = new byte[4] { 46, 251, 255, 255 };
		_peerAliveMessageBuffer = new MessageBuffer(array, array.Length);
		byte[] array2 = new byte[4] { 241, 216, 255, 255 };
		_disconnectMessageBuffer = new MessageBuffer(array2, array2.Length);
	}

	internal TcpSocket GetLastIncomingConnection()
	{
		if (_incomingConnections.TryDequeue(out var result))
		{
			result.LastMessageArrivalTime = Environment.TickCount;
			result.LastMessageSentTime = Environment.TickCount;
		}
		return result;
	}

	internal void Connect(string address, int port)
	{
		try
		{
			LastMessageArrivalTime = Environment.TickCount;
			LastMessageSentTime = 0;
			_dotNetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPAddress[] hostAddresses = Dns.GetHostAddresses(address);
			IPAddress address2 = null;
			for (int i = 0; i < hostAddresses.Length; i++)
			{
				if (hostAddresses[i].AddressFamily == AddressFamily.InterNetwork)
				{
					address2 = hostAddresses[i];
				}
			}
			_socketAsyncEventArgsListener.RemoteEndPoint = new IPEndPoint(address2, port);
			Status = TcpStatus.Connecting;
			if (!_dotNetSocket.ConnectAsync(_socketAsyncEventArgsListener))
			{
				ProcessIO(_dotNetSocket, _socketAsyncEventArgsListener);
			}
		}
		catch (Exception ex)
		{
			Status = TcpStatus.SocketClosed;
			Debug.Print("Tcp Connection Error: " + ex);
			Thread.Sleep(250);
		}
	}

	internal void CheckAcceptClient()
	{
		if (!_currentlyAcceptingClients)
		{
			_currentlyAcceptingClients = true;
			if (!_dotNetSocket.AcceptAsync(_socketAsyncEventArgsListener))
			{
				ProcessIO(_dotNetSocket, _socketAsyncEventArgsListener);
			}
		}
	}

	internal void Listen(int port)
	{
		_incomingConnections = new ConcurrentQueue<TcpSocket>();
		_dotNetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		_dotNetSocket.Bind(new IPEndPoint(System.Net.IPAddress.Any, port));
		_dotNetSocket.Listen(1024);
		CheckAcceptClient();
	}

	internal void ProcessRead()
	{
		if (!IsConnected || _processingReceive || (Status != TcpStatus.WaitingData && Status != TcpStatus.WaitingDataLength))
		{
			return;
		}
		if (LastReadMessage == null)
		{
			LastReadMessage = new MessageBuffer(new byte[16777216], 0);
		}
		if (Status == TcpStatus.WaitingDataLength)
		{
			_processingReceive = true;
			_socketAsyncEventArgsRead.SetBuffer(LastReadMessage.Buffer, _lastMessageTotalRead, 4 - _lastMessageTotalRead);
			if (!_dotNetSocket.ReceiveAsync(_socketAsyncEventArgsRead))
			{
				ProcessIO(_dotNetSocket, _socketAsyncEventArgsRead);
			}
		}
		else if (Status == TcpStatus.WaitingData)
		{
			_processingReceive = true;
			_socketAsyncEventArgsRead.SetBuffer(LastReadMessage.Buffer, _lastMessageTotalRead, LastReadMessage.DataLength - _lastMessageTotalRead);
			if (!_dotNetSocket.ReceiveAsync(_socketAsyncEventArgsRead))
			{
				ProcessIO(_dotNetSocket, _socketAsyncEventArgsRead);
			}
		}
	}

	internal void ProcessWrite()
	{
		if (!IsConnected || _currentlySendingMessage != null || !_writeNetworkMessageQueue.TryDequeue(out _currentlySendingMessage))
		{
			return;
		}
		try
		{
			_socketAsyncEventArgsWrite.SetBuffer(_currentlySendingMessage.Buffer, 0, _currentlySendingMessage.DataLength);
			if (!_dotNetSocket.SendAsync(_socketAsyncEventArgsWrite))
			{
				ProcessIO(_dotNetSocket, _socketAsyncEventArgsWrite);
			}
			LastMessageSentTime = Environment.TickCount;
		}
		catch (Exception ex)
		{
			Debug.Print("SendMessage Error: " + ex);
			Status = TcpStatus.ConnectionClosed;
		}
	}

	private void ProcessIO(object sender, SocketAsyncEventArgs e)
	{
		try
		{
			if (e.LastOperation == SocketAsyncOperation.Accept)
			{
				if (e.SocketError == SocketError.Success)
				{
					Status = TcpStatus.WaitingDataLength;
					AddIncomingConnection(e);
				}
			}
			else if (e.LastOperation == SocketAsyncOperation.Connect)
			{
				if (e.SocketError == SocketError.Success)
				{
					Status = TcpStatus.WaitingDataLength;
					return;
				}
				Debug.Print("Connection error: " + e.SocketError);
				Status = TcpStatus.ConnectionClosed;
			}
			else if (e.LastOperation == SocketAsyncOperation.Send)
			{
				if (e.SocketError == SocketError.Success)
				{
					if (_currentlySendingMessage == _disconnectMessageBuffer)
					{
						Status = TcpStatus.ConnectionClosed;
					}
					_currentlySendingMessage = null;
				}
				else
				{
					Debug.Print("Message Send, error: " + e.SocketError);
					Status = TcpStatus.ConnectionClosed;
				}
			}
			else
			{
				if (e.LastOperation == SocketAsyncOperation.Disconnect || e.LastOperation != SocketAsyncOperation.Receive)
				{
					return;
				}
				LastMessageArrivalTime = Environment.TickCount;
				if (Status == TcpStatus.WaitingDataLength)
				{
					if (e.BytesTransferred != 4 - _lastMessageTotalRead)
					{
						return;
					}
					_lastMessageTotalRead += e.BytesTransferred;
					if (_lastMessageTotalRead != 4)
					{
						return;
					}
					int num = BitConverter.ToInt32(LastReadMessage.Buffer, 0);
					if (num == -1234)
					{
						Status = TcpStatus.WaitingDataLength;
						_lastMessageTotalRead = 0;
						LastReadMessage = null;
					}
					else if (num == -9999)
					{
						Status = TcpStatus.ConnectionClosed;
					}
					else
					{
						if (num > 16777216)
						{
							throw new Exception($"Message length too big: {LastReadMessage.DataLength}");
						}
						if (num <= 0)
						{
							throw new Exception($"Message length too small: {LastReadMessage.DataLength}");
						}
						LastReadMessage.DataLength = num + 4;
						Status = TcpStatus.WaitingData;
					}
					_processingReceive = false;
				}
				else
				{
					if (Status != TcpStatus.WaitingData)
					{
						return;
					}
					_lastMessageTotalRead += e.BytesTransferred;
					if (_lastMessageTotalRead == LastReadMessage.DataLength)
					{
						if (this.MessageReceived != null)
						{
							this.MessageReceived(LastReadMessage);
						}
						Status = TcpStatus.WaitingDataLength;
						_lastMessageTotalRead = 0;
						LastReadMessage = null;
					}
					_processingReceive = false;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.Print(string.Concat("Exception on TcpSocket::ProcessIO ", ex, " - ", ex.Message, " ", ex.StackTrace));
		}
	}

	private void AddIncomingConnection(SocketAsyncEventArgs e)
	{
		TcpSocket tcpSocket = new TcpSocket();
		tcpSocket._dotNetSocket = e.AcceptSocket;
		e.AcceptSocket = null;
		tcpSocket.Status = TcpStatus.WaitingDataLength;
		_incomingConnections.Enqueue(tcpSocket);
		_currentlyAcceptingClients = false;
	}

	private void EnqueueMessage(MessageBuffer messageBuffer)
	{
		_writeNetworkMessageQueue.Enqueue(messageBuffer);
	}

	internal void SendDisconnectMessage()
	{
		EnqueueMessage(_disconnectMessageBuffer);
	}

	internal void SendPeerAliveMessage()
	{
		EnqueueMessage(_peerAliveMessageBuffer);
	}

	internal void SendMessage(MessageBuffer messageBuffer)
	{
		EnqueueMessage(messageBuffer);
	}

	internal void Close()
	{
		if (Status == TcpStatus.SocketClosed)
		{
			Debug.Print("Socket already closed.");
			return;
		}
		Status = TcpStatus.SocketClosed;
		if (_dotNetSocket != null)
		{
			try
			{
				if (_dotNetSocket.Available > 0)
				{
					Debug.Print("Closing socket but there were " + _dotNetSocket.Available + " bytes data");
				}
			}
			catch
			{
			}
			_dotNetSocket.Close(0);
			Debug.Print("Socket " + _uniqueSocketId + " closed and destroyed!");
		}
		_dotNetSocket = null;
		if (_socketAsyncEventArgsRead != null)
		{
			_socketAsyncEventArgsRead.Dispose();
			_socketAsyncEventArgsRead = null;
		}
		if (_socketAsyncEventArgsWrite != null)
		{
			_socketAsyncEventArgsWrite.Dispose();
			_socketAsyncEventArgsWrite = null;
		}
		if (_socketAsyncEventArgsListener != null)
		{
			_socketAsyncEventArgsListener.Dispose();
			_socketAsyncEventArgsListener = null;
		}
		if (_dotNetSocket != null)
		{
			_dotNetSocket.Dispose();
			_dotNetSocket = null;
		}
	}
}
