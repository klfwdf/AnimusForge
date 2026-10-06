using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest;

[Serializable]
[DataContract]
public class RestObjectRequestMessage : RestRequestMessage
{
	[DataMember]
	public MessageType MessageType { get; private set; }

	[DataMember]
	public SessionCredentials SessionCredentials { get; private set; }

	[DataMember]
	public Message Message { get; private set; }

	public RestObjectRequestMessage()
	{
	}

	public RestObjectRequestMessage(SessionCredentials sessionCredentials, Message message, MessageType messageType)
	{
		Message = message;
		MessageType = messageType;
		SessionCredentials = sessionCredentials;
	}
}
