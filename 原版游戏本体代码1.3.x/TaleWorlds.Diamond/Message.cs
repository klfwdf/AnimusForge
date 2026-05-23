using System;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x0200001A RID: 26
	[JsonConverter(typeof(MessageJsonConverter))]
	[Serializable]
	public abstract class Message
	{
	}
}
