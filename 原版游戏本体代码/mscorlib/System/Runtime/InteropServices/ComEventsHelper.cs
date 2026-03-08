using System;
using System.Runtime.Remoting;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009AC RID: 2476
	[__DynamicallyInvokable]
	public static class ComEventsHelper
	{
		// Token: 0x06006310 RID: 25360 RVA: 0x00151B74 File Offset: 0x0014FD74
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static void Combine(object rcw, Guid iid, int dispid, Delegate d)
		{
			rcw = ComEventsHelper.UnwrapIfTransparentProxy(rcw);
			object obj = rcw;
			lock (obj)
			{
				ComEventsInfo comEventsInfo = ComEventsInfo.FromObject(rcw);
				ComEventsSink comEventsSink = comEventsInfo.FindSink(ref iid);
				if (comEventsSink == null)
				{
					comEventsSink = comEventsInfo.AddSink(ref iid);
				}
				ComEventsMethod comEventsMethod = comEventsSink.FindMethod(dispid);
				if (comEventsMethod == null)
				{
					comEventsMethod = comEventsSink.AddMethod(dispid);
				}
				comEventsMethod.AddDelegate(d);
			}
		}

		// Token: 0x06006311 RID: 25361 RVA: 0x00151BEC File Offset: 0x0014FDEC
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static Delegate Remove(object rcw, Guid iid, int dispid, Delegate d)
		{
			rcw = ComEventsHelper.UnwrapIfTransparentProxy(rcw);
			object obj = rcw;
			Delegate result;
			lock (obj)
			{
				ComEventsInfo comEventsInfo = ComEventsInfo.Find(rcw);
				if (comEventsInfo == null)
				{
					result = null;
				}
				else
				{
					ComEventsSink comEventsSink = comEventsInfo.FindSink(ref iid);
					if (comEventsSink == null)
					{
						result = null;
					}
					else
					{
						ComEventsMethod comEventsMethod = comEventsSink.FindMethod(dispid);
						if (comEventsMethod == null)
						{
							result = null;
						}
						else
						{
							comEventsMethod.RemoveDelegate(d);
							if (comEventsMethod.Empty)
							{
								comEventsMethod = comEventsSink.RemoveMethod(comEventsMethod);
							}
							if (comEventsMethod == null)
							{
								comEventsSink = comEventsInfo.RemoveSink(comEventsSink);
							}
							if (comEventsSink == null)
							{
								Marshal.SetComObjectData(rcw, typeof(ComEventsInfo), null);
								GC.SuppressFinalize(comEventsInfo);
							}
							result = d;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06006312 RID: 25362 RVA: 0x00151CA4 File Offset: 0x0014FEA4
		[SecurityCritical]
		internal static object UnwrapIfTransparentProxy(object rcw)
		{
			if (RemotingServices.IsTransparentProxy(rcw))
			{
				IntPtr iunknownForObject = Marshal.GetIUnknownForObject(rcw);
				try
				{
					rcw = Marshal.GetObjectForIUnknown(iunknownForObject);
				}
				finally
				{
					Marshal.Release(iunknownForObject);
				}
			}
			return rcw;
		}
	}
}
