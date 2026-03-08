using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native
{
	// Token: 0x02000013 RID: 19
	internal class AutoPinner : IDisposable
	{
		// Token: 0x060000F8 RID: 248 RVA: 0x00005533 File Offset: 0x00003733
		public AutoPinner(object obj)
		{
			if (obj != null)
			{
				this._pinnedObject = GCHandle.Alloc(obj, GCHandleType.Pinned);
			}
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000554B File Offset: 0x0000374B
		public static implicit operator IntPtr(AutoPinner autoPinner)
		{
			if (autoPinner._pinnedObject.IsAllocated)
			{
				return autoPinner._pinnedObject.AddrOfPinnedObject();
			}
			return IntPtr.Zero;
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000556B File Offset: 0x0000376B
		public void Dispose()
		{
			if (this._pinnedObject.IsAllocated)
			{
				this._pinnedObject.Free();
			}
		}

		// Token: 0x0400005C RID: 92
		private GCHandle _pinnedObject;
	}
}
