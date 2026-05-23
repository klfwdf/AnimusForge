using System;

namespace HarmonyLib
{
	/// <summary>Specifies the type of patch</summary>
	// Token: 0x02000060 RID: 96
	public enum HarmonyPatchType
	{
		/// <summary>Any patch</summary>
		// Token: 0x0400016C RID: 364
		All,
		/// <summary>A prefix patch</summary>
		// Token: 0x0400016D RID: 365
		Prefix,
		/// <summary>A postfix patch</summary>
		// Token: 0x0400016E RID: 366
		Postfix,
		/// <summary>A transpiler</summary>
		// Token: 0x0400016F RID: 367
		Transpiler,
		/// <summary>A finalizer</summary>
		// Token: 0x04000170 RID: 368
		Finalizer,
		/// <summary>A reverse patch</summary>
		// Token: 0x04000171 RID: 369
		ReversePatch,
		/// <summary>An inner prefix patch</summary>
		// Token: 0x04000172 RID: 370
		InnerPrefix,
		/// <summary>An inner postfix patch</summary>
		// Token: 0x04000173 RID: 371
		InnerPostfix
	}
}
