using System;

namespace TaleWorlds.Library.Http;

public interface IHttpRequestTask
{
	HttpRequestTaskState State { get; }

	bool Successful { get; }

	string ResponseData { get; }

	Exception Exception { get; }

	void Start();
}
