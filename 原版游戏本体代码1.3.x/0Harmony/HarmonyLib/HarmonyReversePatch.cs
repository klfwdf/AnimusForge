using System;

namespace HarmonyLib
{
	/// <summary>Annotation to define your standin methods for reverse patching</summary>
	// Token: 0x02000067 RID: 103
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
	public class HarmonyReversePatch : HarmonyAttribute
	{
		/// <summary>An annotation that specifies the type of reverse patching</summary>
		/// <param name="type">The <see cref="T:HarmonyLib.HarmonyReversePatchType" /> of the reverse patch</param>
		// Token: 0x060001FF RID: 511 RVA: 0x0000D757 File Offset: 0x0000B957
		public HarmonyReversePatch(HarmonyReversePatchType type = HarmonyReversePatchType.Original)
		{
			this.info.reversePatchType = new HarmonyReversePatchType?(type);
		}
	}
}
