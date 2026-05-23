using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest;

[Serializable]
[DataContract]
public sealed class RestResponse : RestData
{
	[DataMember]
	private List<RestResponseMessage> _responseMessages;

	[DataMember]
	public bool Successful { get; private set; }

	[DataMember]
	public string SuccessfulReason { get; private set; }

	[DataMember]
	public RestFunctionResult FunctionResult { get; set; }

	[DataMember]
	public byte[] UserCertificate { get; set; }

	public int RemainingMessageCount
	{
		get
		{
			if (_responseMessages != null)
			{
				return _responseMessages.Count;
			}
			return 0;
		}
	}

	public RestResponse()
	{
		_responseMessages = new List<RestResponseMessage>();
	}

	public void SetSuccessful(bool successful, string successfulReason)
	{
		Successful = successful;
		SuccessfulReason = successfulReason;
	}

	public static RestResponse Create(bool successful, string successfulReason)
	{
		RestResponse restResponse = new RestResponse();
		restResponse.SetSuccessful(successful, successfulReason);
		return restResponse;
	}

	public RestResponseMessage TryDequeueMessage()
	{
		if (_responseMessages != null && _responseMessages.Count > 0)
		{
			RestResponseMessage result = _responseMessages[0];
			_responseMessages.RemoveAt(0);
			return result;
		}
		return null;
	}

	public void ClearMessageQueue()
	{
		_responseMessages.Clear();
	}

	public void EnqueueMessage(RestResponseMessage message)
	{
		_responseMessages.Add(message);
	}
}
