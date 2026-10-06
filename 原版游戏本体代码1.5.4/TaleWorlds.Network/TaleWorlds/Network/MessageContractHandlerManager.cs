using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Network;

public class MessageContractHandlerManager
{
	private Dictionary<byte, MessageContractHandler> MessageHandlers { get; set; }

	private Dictionary<byte, Type> MessageContractTypes { get; set; }

	public MessageContractHandlerManager()
	{
		MessageHandlers = new Dictionary<byte, MessageContractHandler>();
		MessageContractTypes = new Dictionary<byte, Type>();
	}

	public void AddMessageHandler<T>(MessageContractHandlerDelegate<T> handler) where T : MessageContract
	{
		MessageContractHandler<T> value = new MessageContractHandler<T>(handler);
		Type typeFromHandle = typeof(T);
		byte contractId = MessageContract.GetContractId(typeFromHandle);
		MessageContractTypes.Add(contractId, typeFromHandle);
		MessageHandlers.Add(contractId, value);
	}

	public void HandleMessage(MessageContract messageContract)
	{
		MessageHandlers[messageContract.MessageId].Invoke(messageContract);
	}

	public void HandleNetworkMessage(NetworkMessage networkMessage)
	{
		byte b = networkMessage.ReadByte();
		Type type = MessageContractTypes[b];
		MessageContract messageContract = MessageContract.CreateMessageContract(type);
		Debug.Print(string.Concat("Message with id: ", b, " / contract:", type, "received and processing..."));
		messageContract.DeserializeFromNetworkMessage(networkMessage);
		HandleMessage(messageContract);
	}

	internal Type GetMessageContractType(byte id)
	{
		return MessageContractTypes[id];
	}

	public bool ContainsMessageHandler(byte id)
	{
		return MessageContractTypes.ContainsKey(id);
	}
}
