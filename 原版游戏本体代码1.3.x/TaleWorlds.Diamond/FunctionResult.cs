using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000008 RID: 8
	[KnownType("GetKnownTypes")]
	[JsonConverter(typeof(FunctionResultJsonConverter))]
	[Serializable]
	public abstract class FunctionResult
	{
	}
}
