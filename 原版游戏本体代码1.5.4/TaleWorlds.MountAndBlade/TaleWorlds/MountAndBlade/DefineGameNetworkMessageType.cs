using System;

namespace TaleWorlds.MountAndBlade;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class DefineGameNetworkMessageType : Attribute
{
	public readonly GameNetworkMessageSendType SendType;

	public DefineGameNetworkMessageType(GameNetworkMessageSendType sendType)
	{
		SendType = sendType;
	}
}
