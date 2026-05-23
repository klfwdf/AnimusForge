using System;

namespace HarmonyLib
{
	/// <summary>Specifies the type of reverse patch</summary>
	// Token: 0x02000061 RID: 97
	public enum HarmonyReversePatchType
	{
		/// <summary>Use the unmodified original method (directly from IL)</summary>
		// Token: 0x04000175 RID: 373
		Original,
		/// <summary>Use the original as it is right now including previous patches but excluding future ones</summary>
		// Token: 0x04000176 RID: 374
		Snapshot
	}
}
