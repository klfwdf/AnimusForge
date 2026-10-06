using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.Library.Http;

namespace TaleWorlds.Diamond.Rest;

internal class ClientRestSessionTask
{
	private static readonly int RequestRetryTimeout = 1000;

	private readonly Type[] RetryableExceptions = new Type[5]
	{
		typeof(HttpRequestException),
		typeof(TaskCanceledException),
		typeof(IOException),
		typeof(SocketException),
		typeof(InvalidOperationException)
	};

	public bool _willTryAgain;

	private string _requestAddress;

	private string _postData;

	private string _messageName;

	private int _maxIterationCount = 5;

	private int _currentIterationCount;

	private Stopwatch _sw;

	private TaskCompletionSource<bool> _taskCompletionSource;

	private IHttpDriver _networkClient;

	private bool _resultExamined;

	public RestRequestMessage RestRequestMessage { get; private set; }

	public bool Finished { get; private set; }

	public bool Successful { get; private set; }

	public IHttpRequestTask Request { get; private set; }

	public CancellationToken CancellationToken { get; private set; }

	public RestResponse RestResponse { get; private set; }

	public int RetryCount { get; private set; }

	public int TotalHttpAttempts => _currentIterationCount + 1;

	public bool IsCompletelyFinished
	{
		get
		{
			if (_willTryAgain)
			{
				return false;
			}
			if (!_resultExamined)
			{
				return false;
			}
			return Request.State == HttpRequestTaskState.Finished;
		}
	}

	public ClientRestSessionTask(RestRequestMessage restRequestMessage, CancellationToken cancellationToken, bool retry = true)
	{
		if (!retry)
		{
			_maxIterationCount = 0;
		}
		CancellationToken = cancellationToken;
		RestRequestMessage = restRequestMessage;
		_taskCompletionSource = new TaskCompletionSource<bool>();
		_sw = new Stopwatch();
		_messageName = RestRequestMessage.TypeName;
	}

	public void SetRequestData(byte[] userCertificate, string address, IHttpDriver networkClient)
	{
		RestRequestMessage.UserCertificate = userCertificate;
		_requestAddress = address;
		_postData = RestRequestMessage.SerializeAsJson();
		_networkClient = networkClient;
		CreateAndSetRequest();
	}

	private void DetermineNextTry()
	{
		if (_sw.ElapsedMilliseconds >= RequestRetryTimeout)
		{
			_willTryAgain = false;
			TaleWorlds.Library.Debug.Print($"[Resilience][Transport] {_messageName} — issuing transport retry {_currentIterationCount}/{_maxIterationCount}.");
			CreateAndSetRequest();
		}
	}

	private static string GetCode(WebException webException)
	{
		if (webException.Response != null && webException.Response is HttpWebResponse)
		{
			return ((HttpWebResponse)webException.Response).StatusCode.ToString();
		}
		return "NoCode";
	}

	private void ExamineResult()
	{
		if (!Request.Successful)
		{
			string text = Request.Exception?.GetType().Name ?? "null";
			if (Request.Exception != null && RetryableExceptions.Any((Type e) => e == Request.Exception.GetType()))
			{
				if (_currentIterationCount < _maxIterationCount)
				{
					_sw.Restart();
					_willTryAgain = true;
					_currentIterationCount++;
					TaleWorlds.Library.Debug.Print($"[Resilience][Transport] {_messageName} failed ({text}) — retryable, transport retry {_currentIterationCount}/{_maxIterationCount} in {RequestRetryTimeout}ms.");
				}
				else
				{
					_willTryAgain = false;
					TaleWorlds.Library.Debug.Print($"[Resilience][Transport] {_messageName} — transport retries exhausted ({_maxIterationCount}/{_maxIterationCount}), surfacing to Layer 2.");
				}
			}
			else
			{
				_willTryAgain = false;
				TaleWorlds.Library.Debug.Print("[Resilience][Transport] " + _messageName + " failed (" + text + ") — non-retryable exception type, surfacing to Layer 2.");
			}
			if (Request.Exception != null)
			{
				PrintExceptions(Request.Exception);
			}
		}
		else if (_currentIterationCount > 0)
		{
			TaleWorlds.Library.Debug.Print($"[Resilience][Transport] {_messageName} — succeeded after {_currentIterationCount} transport retries.");
		}
		_resultExamined = true;
	}

	public void Tick()
	{
		switch (Request.State)
		{
		case HttpRequestTaskState.NotStarted:
			Request.Start();
			break;
		case HttpRequestTaskState.Finished:
			if (!_resultExamined)
			{
				ExamineResult();
			}
			else
			{
				DetermineNextTry();
			}
			break;
		case HttpRequestTaskState.Working:
			break;
		}
	}

	public async Task WaitUntilFinished()
	{
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::WaitUntilFinished::" + _messageName);
		await _taskCompletionSource.Task;
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::WaitUntilFinished::" + _messageName + " done");
	}

	public void SetFinishedAsSuccessful(RestResponse restResponse)
	{
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::SetFinishedAsSuccessful::" + _messageName);
		RestResponse = restResponse;
		Successful = true;
		Finished = true;
		_taskCompletionSource.SetResult(result: true);
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::SetFinishedAsSuccessful::" + _messageName + " done");
	}

	public void ResetForRetry()
	{
		RetryCount++;
		_resultExamined = false;
		CreateAndSetRequest();
	}

	public void SetFinishedAsFailed()
	{
		SetFinishedAsFailed(null);
	}

	public void SetFinishedAsFailed(RestResponse restResponse)
	{
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::SetFinishedAsFailed::" + _messageName);
		RestResponse = restResponse;
		Successful = false;
		Finished = true;
		_taskCompletionSource.SetResult(result: true);
		TaleWorlds.Library.Debug.Print("ClientRestSessionTask::SetFinishedAsFailed:: " + _messageName + " done");
	}

	private void CreateAndSetRequest()
	{
		bool flag = RestRequestMessage is RestObjectRequestMessage restObjectRequestMessage && restObjectRequestMessage.MessageType == MessageType.Login;
		string address = _requestAddress + (flag ? "/Data/Login" : "/Data/ProcessMessage");
		Request = _networkClient.CreateHttpPostRequestTask(address, _postData, withUserToken: true, CancellationToken);
		_resultExamined = false;
	}

	private void PrintExceptions(Exception e)
	{
		if (e != null)
		{
			Exception ex = e;
			int num = 0;
			while (ex != null)
			{
				TaleWorlds.Library.Debug.Print("Exception #" + num + ": " + ex.Message + " ||| StackTrace: " + ex.InnerException);
				ex = ex.InnerException;
				num++;
			}
		}
	}
}
