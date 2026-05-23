using System;

namespace HarmonyLib
{
	/// <summary>Specifies the type of argument</summary>
	// Token: 0x0200005F RID: 95
	public enum ArgumentType
	{
		/// <summary>This is a normal argument</summary>
		// Token: 0x04000167 RID: 359
		Normal,
		/// <summary>This is a reference argument (ref)</summary>
		// Token: 0x04000168 RID: 360
		Ref,
		/// <summary>This is an out argument (out)</summary>
		// Token: 0x04000169 RID: 361
		Out,
		/// <summary>This is a pointer argument (&amp;)</summary>
		// Token: 0x0400016A RID: 362
		Pointer
	}
}
