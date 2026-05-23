using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000018 RID: 24
	[DataContract]
	[JsonConverter(typeof(LoginResultObjectJsonConverter))]
	[Serializable]
	public abstract class LoginResultObject
	{
	}
}
