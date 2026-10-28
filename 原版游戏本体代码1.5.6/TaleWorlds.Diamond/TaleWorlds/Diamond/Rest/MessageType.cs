using System;

namespace TaleWorlds.Diamond.Rest;

[Serializable]
public enum MessageType
{
	Login,
	Message,
	Function,
	SessionlessMessage
}
