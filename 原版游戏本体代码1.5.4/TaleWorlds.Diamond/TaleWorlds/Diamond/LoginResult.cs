using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
public sealed class LoginResult : FunctionResult
{
	[JsonProperty]
	public PeerId PeerId { get; private set; }

	[JsonProperty]
	public SessionKey SessionKey { get; private set; }

	[JsonProperty]
	public bool Successful { get; private set; }

	[JsonProperty]
	public string ErrorCode { get; private set; }

	[JsonProperty]
	public Dictionary<string, string> ErrorParameters { get; private set; }

	[JsonProperty]
	public string ProviderResponse { get; private set; }

	[JsonProperty]
	public LoginResultObject LoginResultObject { get; private set; }

	public LoginResult()
	{
	}

	public LoginResult(PeerId peerId, SessionKey sessionKey, LoginResultObject loginResultObject)
	{
		PeerId = peerId;
		SessionKey = sessionKey;
		Successful = true;
		ErrorCode = "";
		LoginResultObject = loginResultObject;
	}

	public LoginResult(PeerId peerId, SessionKey sessionKey)
		: this(peerId, sessionKey, null)
	{
	}

	public LoginResult(string errorCode, Dictionary<string, string> parameters = null)
	{
		ErrorCode = errorCode;
		Successful = false;
		ErrorParameters = parameters;
	}
}
