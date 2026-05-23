using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200050E RID: 1294
	internal sealed class Gen2GcCallback : CriticalFinalizerObject
	{
		// Token: 0x06003CD6 RID: 15574 RVA: 0x000E5BFC File Offset: 0x000E3DFC
		[SecuritySafeCritical]
		public Gen2GcCallback()
		{
		}

		// Token: 0x06003CD7 RID: 15575 RVA: 0x000E5C04 File Offset: 0x000E3E04
		public static void Register(Func<object, bool> callback, object targetObj)
		{
			Gen2GcCallback gen2GcCallback = new Gen2GcCallback();
			gen2GcCallback.Setup(callback, targetObj);
		}

		// Token: 0x06003CD8 RID: 15576 RVA: 0x000E5C1F File Offset: 0x000E3E1F
		[SecuritySafeCritical]
		private void Setup(Func<object, bool> callback, object targetObj)
		{
			this.m_callback = callback;
			this.m_weakTargetObj = GCHandle.Alloc(targetObj, GCHandleType.Weak);
		}

		// Token: 0x06003CD9 RID: 15577 RVA: 0x000E5C38 File Offset: 0x000E3E38
		[SecuritySafeCritical]
		protected override void Finalize()
		{
			try
			{
				if (this.m_weakTargetObj.IsAllocated)
				{
					object target = this.m_weakTargetObj.Target;
					if (target == null)
					{
						this.m_weakTargetObj.Free();
					}
					else
					{
						try
						{
							if (!this.m_callback(target))
							{
								return;
							}
						}
						catch
						{
						}
						if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
						{
							GC.ReRegisterForFinalize(this);
						}
					}
				}
			}
			finally
			{
				base.Finalize();
			}
		}

		// Token: 0x040019D8 RID: 6616
		private Func<object, bool> m_callback;

		// Token: 0x040019D9 RID: 6617
		private GCHandle m_weakTargetObj;
	}
}
