using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest;

[Serializable]
[DataContract]
public class RestObjectResponseMessage : RestResponseMessage
{
	[DataMember]
	private Message _message;

	public override Message GetMessage()
	{
		return _message;
	}

	public RestObjectResponseMessage()
	{
	}

	public RestObjectResponseMessage(Message message)
	{
		_message = message;
	}
}
