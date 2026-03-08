using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x02000010 RID: 16
	[NullableContext(1)]
	[Nullable(0)]
	public sealed class Gen2GcCallback : CriticalFinalizerObject
	{
		// Token: 0x06000014 RID: 20 RVA: 0x0000216F File Offset: 0x0000036F
		private Gen2GcCallback(Func<bool> callback)
		{
			this._callback0 = callback;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x0000217E File Offset: 0x0000037E
		private Gen2GcCallback(Func<object, bool> callback, object targetObj)
		{
			this._callback1 = callback;
			this._weakTargetObj = GCHandle.Alloc(targetObj, GCHandleType.Weak);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x0000219A File Offset: 0x0000039A
		public static void Register(Func<bool> callback)
		{
			new Gen2GcCallback(callback);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000021A3 File Offset: 0x000003A3
		public static void Register(Func<object, bool> callback, object targetObj)
		{
			new Gen2GcCallback(callback, targetObj);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000021B0 File Offset: 0x000003B0
		protected override void Finalize()
		{
			try
			{
				if (this._weakTargetObj.IsAllocated)
				{
					object target = this._weakTargetObj.Target;
					if (target == null)
					{
						this._weakTargetObj.Free();
						return;
					}
					try
					{
						if (!this._callback1(target))
						{
							this._weakTargetObj.Free();
							return;
						}
						goto IL_5F;
					}
					catch
					{
						goto IL_5F;
					}
				}
				try
				{
					if (!this._callback0())
					{
						return;
					}
				}
				catch
				{
				}
				IL_5F:
				GC.ReRegisterForFinalize(this);
			}
			finally
			{
				base.Finalize();
			}
		}

		// Token: 0x04000014 RID: 20
		[Nullable(2)]
		private readonly Func<bool> _callback0;

		// Token: 0x04000015 RID: 21
		[Nullable(new byte[] { 2, 1 })]
		private readonly Func<object, bool> _callback1;

		// Token: 0x04000016 RID: 22
		private GCHandle _weakTargetObj;
	}
}
