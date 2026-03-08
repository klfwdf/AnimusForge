using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x02000036 RID: 54
	[DataContract]
	[Serializable]
	public abstract class RestFunctionResult : RestData
	{
		// Token: 0x0600012A RID: 298
		public abstract FunctionResult GetFunctionResult();
	}
}
