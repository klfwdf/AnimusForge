using System;

namespace HarmonyLib
{
	/// <summary>Annotation to define a category for use with PatchCategory</summary>
	// Token: 0x02000064 RID: 100
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
	public class HarmonyPatchCategory : HarmonyAttribute
	{
		/// <summary>Annotation specifying the category</summary>
		/// <param name="category">Name of patch category</param>
		// Token: 0x060001D7 RID: 471 RVA: 0x0000D2E5 File Offset: 0x0000B4E5
		public HarmonyPatchCategory(string category)
		{
			this.info.category = category;
		}
	}
}
