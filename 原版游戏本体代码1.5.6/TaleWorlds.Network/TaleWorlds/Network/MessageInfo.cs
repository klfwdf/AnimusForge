using System;
using System.IO;

namespace TaleWorlds.Network;

public class MessageInfo
{
	public bool DestinationIsPostBox = true;

	public string SourceIPAddress { get; set; }

	public Guid SourceClientId { get; set; }

	public string SourceUserName { get; set; }

	public string SourcePlatform { get; set; }

	public string SourcePlatformId { get; set; }

	public string DestinationPostBox { get; set; }

	public Guid DestinationClientId { get; set; }

	public void WriteTo(Stream stream, bool fromServer)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		binaryWriter.Write(DestinationIsPostBox);
		if (DestinationIsPostBox)
		{
			binaryWriter.Write(DestinationPostBox);
		}
		else
		{
			binaryWriter.Write(DestinationClientId.ToByteArray());
		}
		if (fromServer)
		{
			binaryWriter.Write(SourceIPAddress);
			binaryWriter.Write(SourceClientId.ToByteArray());
			binaryWriter.Write(SourceUserName);
			binaryWriter.Write(SourcePlatform);
			binaryWriter.Write(SourcePlatformId);
		}
	}

	public static MessageInfo ReadFrom(Stream stream, bool fromServer)
	{
		MessageInfo messageInfo = new MessageInfo();
		BinaryReader binaryReader = new BinaryReader(stream);
		messageInfo.DestinationIsPostBox = binaryReader.ReadBoolean();
		if (messageInfo.DestinationIsPostBox)
		{
			messageInfo.DestinationPostBox = binaryReader.ReadString();
		}
		else
		{
			messageInfo.DestinationClientId = new Guid(binaryReader.ReadBytes(16));
		}
		if (fromServer)
		{
			messageInfo.SourceIPAddress = binaryReader.ReadString();
			messageInfo.SourceClientId = new Guid(binaryReader.ReadBytes(16));
			messageInfo.SourceUserName = binaryReader.ReadString();
			messageInfo.SourcePlatform = binaryReader.ReadString();
			messageInfo.SourcePlatformId = binaryReader.ReadString();
		}
		return messageInfo;
	}
}
