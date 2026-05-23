using System;

namespace TaleWorlds.Library
{
	// Token: 0x02000061 RID: 97
	[Serializable]
	public struct ManagedArray
	{
		// Token: 0x060002BF RID: 703 RVA: 0x000086C0 File Offset: 0x000068C0
		public ManagedArray(IntPtr array, int length)
		{
			this.Array = array;
			this.Length = length;
		}

		// Token: 0x0400011F RID: 287
		internal IntPtr Array;

		// Token: 0x04000120 RID: 288
		internal int Length;
	}
}
