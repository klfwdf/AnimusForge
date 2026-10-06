using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
[KnownType("GetKnownTypes")]
[JsonConverter(typeof(FunctionResultJsonConverter))]
public abstract class FunctionResult
{
	static FunctionResult()
	{
	}
}
