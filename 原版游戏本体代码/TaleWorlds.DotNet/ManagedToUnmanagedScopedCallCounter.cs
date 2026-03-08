using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000027 RID: 39
	public class ManagedToUnmanagedScopedCallCounter : IDisposable
	{
		// Token: 0x060000FB RID: 251 RVA: 0x00004E64 File Offset: 0x00003064
		public ManagedToUnmanagedScopedCallCounter()
		{
			if (!ManagedToUnmanagedScopedCallCounter._table.IsValueCreated)
			{
				ManagedToUnmanagedScopedCallCounter._table.Value = new Dictionary<int, List<StackTrace>>();
			}
			ThreadLocal<int> depth = ManagedToUnmanagedScopedCallCounter._depth;
			int value = depth.Value;
			depth.Value = value + 1;
			if (ManagedToUnmanagedScopedCallCounter._depth.Value < ManagedToUnmanagedScopedCallCounter._depthThreshold)
			{
				return;
			}
			this._st = new StackTrace(true);
			List<StackTrace> list;
			if (ManagedToUnmanagedScopedCallCounter._table.Value.TryGetValue(ManagedToUnmanagedScopedCallCounter._depth.Value, out list))
			{
				list.Add(this._st);
				return;
			}
			ManagedToUnmanagedScopedCallCounter._table.Value.Add(ManagedToUnmanagedScopedCallCounter._depth.Value, new List<StackTrace> { this._st });
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00004F18 File Offset: 0x00003118
		public void Dispose()
		{
			ThreadLocal<int> depth = ManagedToUnmanagedScopedCallCounter._depth;
			int value = depth.Value;
			depth.Value = value - 1;
		}

		// Token: 0x04000066 RID: 102
		private static ThreadLocal<Dictionary<int, List<StackTrace>>> _table = new ThreadLocal<Dictionary<int, List<StackTrace>>>();

		// Token: 0x04000067 RID: 103
		private static ThreadLocal<int> _depth = new ThreadLocal<int>();

		// Token: 0x04000068 RID: 104
		private static int _depthThreshold = 4;

		// Token: 0x04000069 RID: 105
		private StackTrace _st;
	}
}
