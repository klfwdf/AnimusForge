using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TaleWorlds.Diamond.ClientApplication;
using TaleWorlds.Library;

namespace TaleWorlds.Diamond;

public abstract class Client : DiamondClientApplicationObject, IClient
{
	private enum ConnectionState
	{
		Idle,
		ReadyToConnect,
		Connecting,
		Connected,
		SleepingToConnectAgain
	}

	private IClientSession _clientSession;

	private Dictionary<Type, Delegate> _messageHandlers;

	private ConnectionState _connectionState;

	private Stopwatch _timer;

	private const long ReconnectTime = 5000L;

	private bool _autoReconnect;

	private bool _isInCriticalState;

	public bool IsInCriticalState
	{
		get
		{
			return _isInCriticalState;
		}
		set
		{
			_isInCriticalState = value;
			UpdateAliveCheckInterval();
		}
	}

	public virtual int AliveCheckTimeInMilliSeconds => 2000;

	public virtual int MaxConsecutiveFailuresBeforeDisconnect => 3;

	protected string SessionAddress
	{
		get
		{
			return _clientSession.Address;
		}
		set
		{
			_clientSession.Address = value;
		}
	}

	public ILoginAccessProvider AccessProvider { get; protected set; }

	protected void UpdateAliveCheckInterval()
	{
		_clientSession.AliveCheckInterval = AliveCheckTimeInMilliSeconds;
	}

	private void ClientSessionConnected()
	{
		OnConnected();
	}

	private void ClientSessionDisconnected()
	{
		OnDisconnected();
	}

	private void ClientSessionConnectionFailed()
	{
		OnCantConnect();
	}

	private void ClientSessionMessageReceived(Message message)
	{
		HandleMessage(message);
	}

	protected Client(DiamondClientApplication diamondClientApplication, IClientSessionFactory sessionProvider, bool autoReconnect)
		: base(diamondClientApplication)
	{
		_clientSession = sessionProvider.CreateSession(AliveCheckTimeInMilliSeconds);
		_clientSession.MaxConsecutiveFailuresBeforeDisconnect = MaxConsecutiveFailuresBeforeDisconnect;
		_clientSession.Connected += ClientSessionConnected;
		_clientSession.ConnectionFailed += ClientSessionConnectionFailed;
		_clientSession.Disconnected += ClientSessionDisconnected;
		_clientSession.MessageReceived += ClientSessionMessageReceived;
		_messageHandlers = new Dictionary<Type, Delegate>();
		_autoReconnect = autoReconnect;
		if (autoReconnect)
		{
			Reset();
			_connectionState = ConnectionState.ReadyToConnect;
		}
	}

	public void Update()
	{
		_clientSession.Tick();
		if (_connectionState == ConnectionState.SleepingToConnectAgain)
		{
			if (_timer.ElapsedMilliseconds > 5000)
			{
				_connectionState = ConnectionState.ReadyToConnect;
				_timer.Stop();
				_timer = null;
			}
		}
		else if (_connectionState == ConnectionState.ReadyToConnect)
		{
			_connectionState = ConnectionState.Connecting;
			_clientSession.Connect();
		}
		else
		{
			_ = _connectionState;
			_ = 3;
		}
		OnTick();
	}

	protected abstract void OnTick();

	protected void SendMessage(Message message)
	{
		_clientSession.SendMessage(message);
	}

	protected async Task<LoginResult> Login(LoginMessage message)
	{
		TaleWorlds.Library.Debug.Print("Logging in");
		return await _clientSession.Login(message);
	}

	protected async Task<CallResult> CallFunction<TResult>(Message message) where TResult : FunctionResult
	{
		return await _clientSession.CallFunction<TResult>(message);
	}

	protected void AddMessageHandler<TMessage>(ClientMessageHandler<TMessage> messageHandler) where TMessage : Message
	{
		_messageHandlers.Add(typeof(TMessage), messageHandler);
	}

	public void HandleMessage(Message message)
	{
		_messageHandlers[message.GetType()].DynamicInvokeWithLog(message);
	}

	public virtual void OnConnected()
	{
		_connectionState = ConnectionState.Connected;
	}

	public virtual void OnCantConnect()
	{
		if (_autoReconnect)
		{
			Reset();
		}
		else
		{
			_connectionState = ConnectionState.Idle;
		}
	}

	public virtual void OnDisconnected()
	{
		if (_autoReconnect)
		{
			Reset();
		}
		else
		{
			_connectionState = ConnectionState.Idle;
		}
	}

	protected void BeginConnect()
	{
		_connectionState = ConnectionState.ReadyToConnect;
	}

	protected void BeginDisconnect()
	{
		_clientSession.Disconnect();
	}

	protected void SetAliveCheckTime(long time)
	{
	}

	private void Reset()
	{
		_connectionState = ConnectionState.SleepingToConnectAgain;
		_timer = new Stopwatch();
		_timer.Start();
		TaleWorlds.Library.Debug.Print("Waiting " + 5000L + " milliseconds for another connection attempt");
	}

	public Task<bool> CheckConnection()
	{
		return _clientSession.CheckConnection();
	}
}
