using System;
using System.Net;
using System.Net.Http;

namespace TaleWorlds.Library.Http;

public class HttpGetRequest : IHttpRequestTask
{
	private const int BufferSize = 1024;

	private HttpClient _httpClient;

	private readonly string _address;

	private Version _versionToUse;

	public HttpRequestTaskState State { get; private set; }

	public bool Successful { get; private set; }

	public string ResponseData { get; private set; }

	public HttpStatusCode ResponseStatusCode { get; private set; }

	public Exception Exception { get; private set; }

	public HttpGetRequest(HttpClient httpClient, string address)
		: this(httpClient, address, new Version("1.1"))
	{
	}

	public HttpGetRequest(HttpClient httpClient, string address, Version version)
	{
		_versionToUse = version;
		_address = address;
		_httpClient = httpClient;
		State = HttpRequestTaskState.NotStarted;
		ResponseData = "";
		ResponseStatusCode = HttpStatusCode.OK;
	}

	private void SetFinishedAsSuccessful(string responseData, HttpStatusCode statusCode)
	{
		Successful = true;
		ResponseData = responseData;
		ResponseStatusCode = statusCode;
		State = HttpRequestTaskState.Finished;
	}

	private void SetFinishedAsUnsuccessful(Exception e)
	{
		Successful = false;
		Exception = e;
		State = HttpRequestTaskState.Finished;
	}

	public void Start()
	{
		DoTask();
	}

	private async void DoTask()
	{
		State = HttpRequestTaskState.Working;
		try
		{
			using HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, _address);
			requestMessage.Version = _versionToUse;
			requestMessage.Headers.Add("Accept", "application/json");
			requestMessage.Headers.Add("UserAgent", "TaleWorlds Client");
			using HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
			_ = response.IsSuccessStatusCode;
			response.EnsureSuccessStatusCode();
			Debug.Print("Protocol version used for get request to " + _address + " is: " + response.Version);
			using HttpContent content = response.Content;
			SetFinishedAsSuccessful(await content.ReadAsStringAsync(), response.StatusCode);
		}
		catch (Exception finishedAsUnsuccessful)
		{
			SetFinishedAsUnsuccessful(finishedAsUnsuccessful);
		}
	}
}
