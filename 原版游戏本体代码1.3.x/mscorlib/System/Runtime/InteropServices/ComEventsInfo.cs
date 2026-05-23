using System;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009AD RID: 2477
	[SecurityCritical]
	internal class ComEventsInfo
	{
		// Token: 0x06006313 RID: 25363 RVA: 0x00151CE4 File Offset: 0x0014FEE4
		private ComEventsInfo(object rcw)
		{
			this._rcw = rcw;
		}

		// Token: 0x06006314 RID: 25364 RVA: 0x00151CF4 File Offset: 0x0014FEF4
		[SecuritySafeCritical]
		~ComEventsInfo()
		{
			this._sinks = ComEventsSink.RemoveAll(this._sinks);
		}

		// Token: 0x06006315 RID: 25365 RVA: 0x00151D2C File Offset: 0x0014FF2C
		[SecurityCritical]
		internal static ComEventsInfo Find(object rcw)
		{
			return (ComEventsInfo)Marshal.GetComObjectData(rcw, typeof(ComEventsInfo));
		}

		// Token: 0x06006316 RID: 25366 RVA: 0x00151D44 File Offset: 0x0014FF44
		[SecurityCritical]
		internal static ComEventsInfo FromObject(object rcw)
		{
			ComEventsInfo comEventsInfo = ComEventsInfo.Find(rcw);
			if (comEventsInfo == null)
			{
				comEventsInfo = new ComEventsInfo(rcw);
				Marshal.SetComObjectData(rcw, typeof(ComEventsInfo), comEventsInfo);
			}
			return comEventsInfo;
		}

		// Token: 0x06006317 RID: 25367 RVA: 0x00151D75 File Offset: 0x0014FF75
		internal ComEventsSink FindSink(ref Guid iid)
		{
			return ComEventsSink.Find(this._sinks, ref iid);
		}

		// Token: 0x06006318 RID: 25368 RVA: 0x00151D84 File Offset: 0x0014FF84
		internal ComEventsSink AddSink(ref Guid iid)
		{
			ComEventsSink sink = new ComEventsSink(this._rcw, iid);
			this._sinks = ComEventsSink.Add(this._sinks, sink);
			return this._sinks;
		}

		// Token: 0x06006319 RID: 25369 RVA: 0x00151DBB File Offset: 0x0014FFBB
		[SecurityCritical]
		internal ComEventsSink RemoveSink(ComEventsSink sink)
		{
			this._sinks = ComEventsSink.Remove(this._sinks, sink);
			return this._sinks;
		}

		// Token: 0x04002CB6 RID: 11446
		private ComEventsSink _sinks;

		// Token: 0x04002CB7 RID: 11447
		private object _rcw;
	}
}
