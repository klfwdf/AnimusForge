using System;
using System.Runtime.InteropServices;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C0 RID: 1984
	[ComVisible(true)]
	public class TypeEntry
	{
		// Token: 0x060055EE RID: 21998 RVA: 0x00131060 File Offset: 0x0012F260
		protected TypeEntry()
		{
		}

		// Token: 0x17000E29 RID: 3625
		// (get) Token: 0x060055EF RID: 21999 RVA: 0x00131068 File Offset: 0x0012F268
		// (set) Token: 0x060055F0 RID: 22000 RVA: 0x00131070 File Offset: 0x0012F270
		public string TypeName
		{
			get
			{
				return this._typeName;
			}
			set
			{
				this._typeName = value;
			}
		}

		// Token: 0x17000E2A RID: 3626
		// (get) Token: 0x060055F1 RID: 22001 RVA: 0x00131079 File Offset: 0x0012F279
		// (set) Token: 0x060055F2 RID: 22002 RVA: 0x00131081 File Offset: 0x0012F281
		public string AssemblyName
		{
			get
			{
				return this._assemblyName;
			}
			set
			{
				this._assemblyName = value;
			}
		}

		// Token: 0x060055F3 RID: 22003 RVA: 0x0013108A File Offset: 0x0012F28A
		internal void CacheRemoteAppEntry(RemoteAppEntry entry)
		{
			this._cachedRemoteAppEntry = entry;
		}

		// Token: 0x060055F4 RID: 22004 RVA: 0x00131093 File Offset: 0x0012F293
		internal RemoteAppEntry GetRemoteAppEntry()
		{
			return this._cachedRemoteAppEntry;
		}

		// Token: 0x0400277E RID: 10110
		private string _typeName;

		// Token: 0x0400277F RID: 10111
		private string _assemblyName;

		// Token: 0x04002780 RID: 10112
		private RemoteAppEntry _cachedRemoteAppEntry;
	}
}
