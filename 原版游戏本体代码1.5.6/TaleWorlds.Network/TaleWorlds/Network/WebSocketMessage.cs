using System;
using System.IO;
using System.Text;

namespace TaleWorlds.Network;

[Obsolete]
public class WebSocketMessage
{
	public static Encoding Encoding = Encoding.UTF8;

	public byte[] Payload { get; set; }

	public MessageInfo MessageInfo { get; set; }

	public int Cursor { get; set; }

	public MessageTypes MessageType { get; set; }

	public WebSocketMessage()
	{
		MessageInfo = new MessageInfo();
	}

	public void SetTextPayload(string payload)
	{
		Payload = Encoding.GetBytes(payload);
	}

	public void WriteTo(bool fromServer, Stream stream)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		MessageInfo.WriteTo(stream, fromServer);
		binaryWriter.Write(Payload.Length);
		binaryWriter.Write(Payload, 0, Payload.Length);
		binaryWriter.Write(Cursor);
		binaryWriter.Write((byte)MessageType);
	}

	public static WebSocketMessage ReadFrom(bool fromServer, byte[] payload)
	{
		using MemoryStream stream = new MemoryStream(payload);
		return ReadFrom(fromServer, stream);
	}

	public static WebSocketMessage ReadFrom(bool fromServer, Stream stream)
	{
		WebSocketMessage webSocketMessage = new WebSocketMessage();
		BinaryReader binaryReader = new BinaryReader(stream);
		webSocketMessage.MessageInfo = MessageInfo.ReadFrom(stream, fromServer);
		int count = binaryReader.ReadInt32();
		webSocketMessage.Payload = binaryReader.ReadBytes(count);
		webSocketMessage.Cursor = binaryReader.ReadInt32();
		webSocketMessage.MessageType = (MessageTypes)binaryReader.ReadByte();
		return webSocketMessage;
	}

	public static WebSocketMessage CreateCursorMessage(int cursor)
	{
		return new WebSocketMessage
		{
			MessageType = MessageTypes.Cursor,
			MessageInfo = 
			{
				DestinationPostBox = ""
			},
			Payload = BitConverter.GetBytes(cursor)
		};
	}

	public static WebSocketMessage CreateCloseMessage()
	{
		return new WebSocketMessage
		{
			MessageType = MessageTypes.Close
		};
	}

	public int GetCursor()
	{
		if (MessageType == MessageTypes.Cursor)
		{
			return BitConverter.ToInt32(Payload, 0);
		}
		throw new Exception("not a cursor message");
	}
}
