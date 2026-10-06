using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.Library;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.Rest;

public class ClientRestSession : IClientSession
{
	private enum RequestResult
	{
		Success,
		SessionFatal,
		TransientFailure,
		HandlerRejected
	}

	private readonly Queue<ClientRestSessionTask> _messageTaskQueue;

	private volatile string _address;

	private byte[] _userCertificate;

	private ClientRestSessionTask _currentMessageTask;

	private ClientRestSessionTask _aliveTask;

	private Stopwatch _timer;

	private long _lastRequestOperationTime;

	private bool _sessionInitialized;

	private SessionCredentials _sessionCredentials;

	private RestDataJsonConverter _restDataJsonConverter;

	private IHttpDriver _platformNetworkClient;

	private const int MaxMessageRetries = 1;

	private int _consecutiveFailures;

	private static readonly ReadOnlyCollection<string> SessionFatalReasons = new ReadOnlyCollection<string>(new string[6] { "SessionNotFound", "InvalidCredentials", "InvalidCertificate", "UnknownMessageType", "FeatureNotSupported", "PeerTypeMismatch" });

	public bool IsConnected { get; private set; }

	public int AliveCheckInterval { get; set; }

	public string Address
	{
		get
		{
			return _address;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				TaleWorlds.Library.Debug.Print("ClientRestSession.Address: ignoring empty address.");
				return;
			}
			if (!IsIdle)
			{
				TaleWorlds.Library.Debug.Print("ClientRestSession.Address: ignoring assignment while the session is not idle.");
				return;
			}
			_address = value;
			_consecutiveFailures = 0;
		}
	}

	public bool IsIdle
	{
		get
		{
			if (!IsConnected && _currentMessageTask == null && _aliveTask == null)
			{
				return _messageTaskQueue.Count == 0;
			}
			return false;
		}
	}

	public int MaxConsecutiveFailuresBeforeDisconnect { get; set; } = 3;

	public event MessageHandledDelegate MessageReceived;

	public event ConnectedDelegate Connected;

	public event DisconnectedDelegate Disconnected;

	public event OnCantConnectDelegate ConnectionFailed;

	public ClientRestSession(string address, IHttpDriver platformNetworkClient, int aliveCheckInterval)
	{
		AliveCheckInterval = aliveCheckInterval;
		_sessionInitialized = false;
		_platformNetworkClient = platformNetworkClient;
		ResetTimer();
		_address = address;
		_messageTaskQueue = new Queue<ClientRestSessionTask>();
		_restDataJsonConverter = new RestDataJsonConverter();
	}

	private void ResetTimer()
	{
		_timer = new Stopwatch();
		_timer.Start();
	}

	private void AssignRequestJob(ClientRestSessionTask requestMessageTask)
	{
		RestRequestMessage restRequestMessage = requestMessageTask.RestRequestMessage;
		bool flag = false;
		if (restRequestMessage is ConnectMessage)
		{
			if (!IsConnected)
			{
				flag = true;
			}
		}
		else if (restRequestMessage is DisconnectMessage)
		{
			if (IsConnected)
			{
				flag = true;
			}
		}
		else if (IsConnected)
		{
			flag = true;
		}
		if (flag)
		{
			_currentMessageTask = requestMessageTask;
			_currentMessageTask.SetRequestData(_userCertificate, _address, _platformNetworkClient);
		}
		else
		{
			TaleWorlds.Library.Debug.Print("Setting new request message as failed because can't assign it");
			requestMessageTask.SetFinishedAsFailed();
		}
	}

	private void RemoveRequestJob()
	{
		_lastRequestOperationTime = _timer.ElapsedMilliseconds;
		TaleWorlds.Library.Debug.Print($"[AliveChannel] Main-channel response complete ({_currentMessageTask.RestRequestMessage?.TypeName}) — alive timer reset at {_lastRequestOperationTime}ms.");
		_currentMessageTask = null;
	}

	void IClientSession.Tick()
	{
		TryAssignJob();
		if (_currentMessageTask != null)
		{
			_currentMessageTask.Tick();
			if (_currentMessageTask.IsCompletelyFinished)
			{
				ProcessCompletedMessageTask(_currentMessageTask);
				if (_currentMessageTask != null && _currentMessageTask.Finished)
				{
					RemoveRequestJob();
				}
			}
		}
		TryAssignAliveJob();
		if (_aliveTask != null)
		{
			_aliveTask.Tick();
			if (_aliveTask.IsCompletelyFinished)
			{
				ClientRestSessionTask aliveTask = _aliveTask;
				_aliveTask = null;
				ProcessCompletedAliveTask(aliveTask);
			}
		}
	}

	private (RequestResult result, RestResponse response) ClassifyRequestResult(ClientRestSessionTask task)
	{
		if (!task.Request.Successful)
		{
			return (result: RequestResult.TransientFailure, response: null);
		}
		string responseData = task.Request.ResponseData;
		if (string.IsNullOrEmpty(responseData))
		{
			return (result: RequestResult.TransientFailure, response: null);
		}
		RestResponse restResponse = JsonConvert.DeserializeObject<RestResponse>(responseData, new JsonConverter[1] { _restDataJsonConverter });
		if (!restResponse.Successful)
		{
			string successfulReason = restResponse.SuccessfulReason;
			if (IsSessionFatal(successfulReason))
			{
				return (result: RequestResult.SessionFatal, response: restResponse);
			}
			if (successfulReason == "HandlerFailed" && task.RestRequestMessage is RestObjectRequestMessage { MessageType: MessageType.Function })
			{
				return (result: RequestResult.HandlerRejected, response: restResponse);
			}
			return (result: RequestResult.TransientFailure, response: restResponse);
		}
		return (result: RequestResult.Success, response: restResponse);
	}

	private static bool IsSessionFatal(string reason)
	{
		if (string.IsNullOrEmpty(reason))
		{
			return false;
		}
		foreach (string sessionFatalReason in SessionFatalReasons)
		{
			if (sessionFatalReason == reason)
			{
				return true;
			}
		}
		return false;
	}

	private void ProcessCompletedMessageTask(ClientRestSessionTask task)
	{
		if (task.RestRequestMessage is ConnectMessage)
		{
			if (!task.Request.Successful)
			{
				TaleWorlds.Library.Debug.Print("[Resilience] ConnectMessage HTTP transport failure.");
				task.SetFinishedAsFailed();
				_userCertificate = null;
				ResetTimer();
				this.ConnectionFailed?.Invoke();
			}
			else
			{
				task.SetFinishedAsSuccessful(null);
				IsConnected = true;
				this.Connected?.Invoke();
			}
			return;
		}
		if (task.RestRequestMessage is DisconnectMessage)
		{
			task.SetFinishedAsSuccessful(null);
			OnDisconnected();
			return;
		}
		(RequestResult result, RestResponse response) tuple = ClassifyRequestResult(task);
		RequestResult item = tuple.result;
		RestResponse item2 = tuple.response;
		string text = task.RestRequestMessage?.TypeName;
		switch (item)
		{
		case RequestResult.Success:
			_consecutiveFailures = 0;
			_userCertificate = item2.UserCertificate;
			task.SetFinishedAsSuccessful(item2);
			DrainSessionMessages(item2);
			break;
		case RequestResult.SessionFatal:
			TaleWorlds.Library.Debug.Print("[Resilience] Session-fatal (" + item2?.SuccessfulReason + ") for " + text + " — disconnecting.");
			task.SetFinishedAsFailed(item2);
			OnDisconnected();
			break;
		case RequestResult.HandlerRejected:
			TaleWorlds.Library.Debug.Print("[Resilience] Handler rejected " + text + " (" + item2?.SuccessfulReason + ") — caller will handle.");
			task.SetFinishedAsFailed(item2);
			break;
		case RequestResult.TransientFailure:
			if (task.Request.Successful && task.RetryCount < 1)
			{
				TaleWorlds.Library.Debug.Print($"[Resilience] Retrying {text} (app retry {task.RetryCount + 1}/{1}).");
				task.ResetForRetry();
				break;
			}
			task.SetFinishedAsFailed(item2);
			_consecutiveFailures++;
			TaleWorlds.Library.Debug.Print($"[Resilience] Consecutive failures: {_consecutiveFailures}/{MaxConsecutiveFailuresBeforeDisconnect}" + $" — {text}, total HTTP attempts: {task.TotalHttpAttempts}.");
			if (_consecutiveFailures >= MaxConsecutiveFailuresBeforeDisconnect)
			{
				TaleWorlds.Library.Debug.Print("[Resilience] Threshold reached — disconnecting.");
				OnDisconnected();
			}
			break;
		}
	}

	private void ProcessCompletedAliveTask(ClientRestSessionTask task)
	{
		var (requestResult, restResponse) = ClassifyRequestResult(task);
		switch (requestResult)
		{
		case RequestResult.Success:
			_consecutiveFailures = 0;
			_userCertificate = restResponse.UserCertificate;
			if (restResponse.Polled)
			{
				_lastRequestOperationTime = _timer.ElapsedMilliseconds;
				TaleWorlds.Library.Debug.Print($"[AliveChannel] Polled — timer reset. Messages: {restResponse.RemainingMessageCount}.");
			}
			else
			{
				TaleWorlds.Library.Debug.Print("[AliveChannel] Polled=false (old server?) — timer not reset.");
			}
			DrainSessionMessages(restResponse);
			break;
		case RequestResult.SessionFatal:
			TaleWorlds.Library.Debug.Print("[Resilience][AliveChannel] Session-fatal (" + restResponse?.SuccessfulReason + ") — disconnecting.");
			OnDisconnected();
			break;
		case RequestResult.TransientFailure:
		case RequestResult.HandlerRejected:
			_consecutiveFailures++;
			TaleWorlds.Library.Debug.Print($"[Resilience][AliveChannel] Transient failure — consecutive: {_consecutiveFailures}/{MaxConsecutiveFailuresBeforeDisconnect}" + $", total HTTP attempts: {task.TotalHttpAttempts}.");
			if (_consecutiveFailures >= MaxConsecutiveFailuresBeforeDisconnect)
			{
				TaleWorlds.Library.Debug.Print("[Resilience][AliveChannel] Threshold reached — disconnecting.");
				OnDisconnected();
			}
			break;
		}
	}

	private void DrainSessionMessages(RestResponse restResponse)
	{
		int num = 0;
		while (restResponse.RemainingMessageCount > 0)
		{
			RestResponseMessage restResponseMessage = restResponse.TryDequeueMessage();
			try
			{
				Message message = restResponseMessage.GetMessage();
				if (message != null)
				{
					HandleMessage(message);
					num++;
				}
			}
			catch (Exception ex)
			{
				TaleWorlds.Library.Debug.Print("[SessionMessages] Failed to deliver session message (" + ex.Message + "); skipping.");
			}
		}
		if (num > 0)
		{
			TaleWorlds.Library.Debug.Print($"[SessionMessages] Delivered {num} session message(s).");
		}
	}

	private void OnDisconnected()
	{
		IsConnected = false;
		ClearMessageTaskQueueDueToDisconnect();
		_sessionCredentials = null;
		_sessionInitialized = false;
		_userCertificate = null;
		_aliveTask = null;
		ResetTimer();
		this.Disconnected?.Invoke();
	}

	private void TryAssignJob()
	{
		if (_currentMessageTask == null && _messageTaskQueue.Count > 0)
		{
			ClientRestSessionTask requestMessageTask = _messageTaskQueue.Dequeue();
			AssignRequestJob(requestMessageTask);
		}
	}

	private void TryAssignAliveJob()
	{
		if (_aliveTask == null && IsConnected && _sessionInitialized && _userCertificate != null)
		{
			long num = _timer.ElapsedMilliseconds - _lastRequestOperationTime;
			if (num > AliveCheckInterval)
			{
				TaleWorlds.Library.Debug.Print($"[AliveChannel] Firing AliveMessage — idle {num}ms > interval {AliveCheckInterval}ms. MainTask in-flight: {_currentMessageTask != null}. Queue depth: {_messageTaskQueue.Count}.");
				_aliveTask = new ClientRestSessionTask(new AliveMessage(_sessionCredentials), new CancellationTokenSource().Token);
				_aliveTask.SetRequestData(_userCertificate, _address, _platformNetworkClient);
			}
		}
	}

	private void ClearMessageTaskQueueDueToDisconnect()
	{
		foreach (ClientRestSessionTask item in _messageTaskQueue)
		{
			item.SetFinishedAsFailed();
		}
		_messageTaskQueue.Clear();
	}

	public void Connect()
	{
		ResetTimer();
		SendMessage(new ConnectMessage());
	}

	public void Disconnect()
	{
		_messageTaskQueue.Enqueue(new ClientRestSessionTask(new DisconnectMessage(), CancellationToken.None, retry: false));
		ResetTimer();
	}

	private void SendMessage(RestRequestMessage message)
	{
		_messageTaskQueue.Enqueue(new ClientRestSessionTask(message, CancellationToken.None));
	}

	async Task<LoginResult> IClientSession.Login(LoginMessage message)
	{
		ClientRestSessionTask clientRestSessionTask = new ClientRestSessionTask(new RestObjectRequestMessage(null, message, MessageType.Login), CancellationToken.None);
		_messageTaskQueue.Enqueue(clientRestSessionTask);
		await clientRestSessionTask.WaitUntilFinished();
		if (!clientRestSessionTask.Successful && !clientRestSessionTask.Request.Successful)
		{
			return new LoginResult(LoginErrorCode.LoginRequestFailed.ToString());
		}
		RestFunctionResult functionResult = clientRestSessionTask.RestResponse.FunctionResult;
		LoginResult loginResult = null;
		if (functionResult != null)
		{
			loginResult = (LoginResult)functionResult.GetFunctionResult();
			if (clientRestSessionTask.Successful)
			{
				_sessionCredentials = new SessionCredentials(loginResult.PeerId, loginResult.SessionKey);
				_sessionInitialized = true;
			}
		}
		return loginResult;
	}

	void IClientSession.SendMessage(Message message)
	{
		SendMessage(new RestObjectRequestMessage(_sessionCredentials, message, MessageType.Message));
	}

	async Task<CallResult> IClientSession.CallFunction<TResult>(Message message)
	{
		ClientRestSessionTask clientRestSessionTask = new ClientRestSessionTask(new RestObjectRequestMessage(_sessionCredentials, message, MessageType.Function), CancellationToken.None);
		_messageTaskQueue.Enqueue(clientRestSessionTask);
		await clientRestSessionTask.WaitUntilFinished();
		if (clientRestSessionTask.Successful)
		{
			FunctionResult result = clientRestSessionTask.RestResponse.FunctionResult?.GetFunctionResult();
			return new CallResult(success: true, result);
		}
		string successfulReason = clientRestSessionTask.RestResponse?.SuccessfulReason;
		return new CallResult(success: false, null, successfulReason);
	}

	private void HandleMessage(Message message)
	{
		this.MessageReceived?.Invoke(message);
	}

	async Task<bool> IClientSession.CheckConnection()
	{
		try
		{
			string url = _address + "/Data/Ping";
			await _platformNetworkClient.HttpGetString(url, withUserToken: false);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
