using System;

namespace TaleWorlds.MountAndBlade;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DefineGameNetworkMessageTypeForMod : Attribute
{
	public readonly GameNetworkMessageSendType SendType;

	public DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType sendType)
	{
		SendType = sendType;
	}
}
