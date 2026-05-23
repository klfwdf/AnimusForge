using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Bannerlord.Harmony
{
	// Token: 0x02000007 RID: 7
	internal class HarmonyRef : Harmony
	{
		// Token: 0x06000012 RID: 18 RVA: 0x00002C17 File Offset: 0x00000E17
		[NullableContext(1)]
		public HarmonyRef(string id)
			: base(id)
		{
		}
	}
}
