using System;

namespace HarmonyLib
{
	/// <summary>Exception block types</summary>
	// Token: 0x0200007A RID: 122
	public enum ExceptionBlockType
	{
		/// <summary>The beginning of an exception block</summary>
		// Token: 0x04000189 RID: 393
		BeginExceptionBlock,
		/// <summary>The beginning of a catch block</summary>
		// Token: 0x0400018A RID: 394
		BeginCatchBlock,
		/// <summary>The beginning of an except filter block (currently not supported to use in a patch)</summary>
		// Token: 0x0400018B RID: 395
		BeginExceptFilterBlock,
		/// <summary>The beginning of a fault block</summary>
		// Token: 0x0400018C RID: 396
		BeginFaultBlock,
		/// <summary>The beginning of a finally block</summary>
		// Token: 0x0400018D RID: 397
		BeginFinallyBlock,
		/// <summary>The end of an exception block</summary>
		// Token: 0x0400018E RID: 398
		EndExceptionBlock
	}
}
