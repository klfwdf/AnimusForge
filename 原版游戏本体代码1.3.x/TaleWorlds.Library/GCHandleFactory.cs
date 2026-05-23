using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaleWorlds.Library
{
	// Token: 0x02000034 RID: 52
	internal static class GCHandleFactory
	{
		// Token: 0x060001B7 RID: 439 RVA: 0x00006F58 File Offset: 0x00005158
		static GCHandleFactory()
		{
			for (int i = 0; i < 512; i++)
			{
				GCHandleFactory._handles.Add(GCHandle.Alloc(null, GCHandleType.Pinned));
			}
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x00006F9C File Offset: 0x0000519C
		public static GCHandle GetHandle()
		{
			object locker = GCHandleFactory._locker;
			lock (locker)
			{
				if (GCHandleFactory._handles.Count > 0)
				{
					GCHandle result = GCHandleFactory._handles[GCHandleFactory._handles.Count - 1];
					GCHandleFactory._handles.RemoveAt(GCHandleFactory._handles.Count - 1);
					return result;
				}
			}
			return GCHandle.Alloc(null, GCHandleType.Pinned);
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x0000701C File Offset: 0x0000521C
		public static void ReturnHandle(GCHandle handle)
		{
			object locker = GCHandleFactory._locker;
			lock (locker)
			{
				GCHandleFactory._handles.Add(handle);
			}
		}

		// Token: 0x040000A9 RID: 169
		private static List<GCHandle> _handles = new List<GCHandle>();

		// Token: 0x040000AA RID: 170
		private static object _locker = new object();
	}
}
