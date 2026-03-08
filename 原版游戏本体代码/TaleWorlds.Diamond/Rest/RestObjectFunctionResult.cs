using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest
{
	// Token: 0x02000037 RID: 55
	[DataContract]
	[Serializable]
	public class RestObjectFunctionResult : RestFunctionResult
	{
		// Token: 0x0600012C RID: 300 RVA: 0x00003FA0 File Offset: 0x000021A0
		public override FunctionResult GetFunctionResult()
		{
			return this._functionResult;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00003FA8 File Offset: 0x000021A8
		public RestObjectFunctionResult()
		{
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00003FB0 File Offset: 0x000021B0
		public RestObjectFunctionResult(FunctionResult functionResult)
		{
			this._functionResult = functionResult;
		}

		// Token: 0x0400005B RID: 91
		[DataMember]
		private FunctionResult _functionResult;
	}
}
