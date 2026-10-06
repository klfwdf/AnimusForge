using System;
using System.Net;

namespace TaleWorlds.PlayerServices;

public class TimeoutWebClient : WebClient
{
	public int Timeout { get; set; }

	public TimeoutWebClient()
	{
		Timeout = 15000;
	}

	public TimeoutWebClient(int timeout)
	{
		Timeout = timeout;
	}

	protected override WebRequest GetWebRequest(Uri address)
	{
		WebRequest webRequest = base.GetWebRequest(address);
		webRequest.Timeout = Timeout;
		return webRequest;
	}
}
