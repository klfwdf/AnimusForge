using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x0200046B RID: 1131
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class Gen2GcCallback : CriticalFinalizerObject
	{
		// Token: 0x0600185B RID: 6235 RVA: 0x0004D103 File Offset: 0x0004B303
		private Gen2GcCallback(Func<bool> callback)
		{
			this._callback0 = callback;
		}

		// Token: 0x0600185C RID: 6236 RVA: 0x0004D112 File Offset: 0x0004B312
		private Gen2GcCallback(Func<object, bool> callback, object targetObj)
		{
			this._callback1 = callback;
			this._weakTargetObj = GCHandle.Alloc(targetObj, GCHandleType.Weak);
		}

		// Token: 0x0600185D RID: 6237 RVA: 0x0004D12E File Offset: 0x0004B32E
		public static void Register(Func<bool> callback)
		{
			new Gen2GcCallback(callback);
		}

		// Token: 0x0600185E RID: 6238 RVA: 0x0004D137 File Offset: 0x0004B337
		public static void Register(Func<object, bool> callback, object targetObj)
		{
			new Gen2GcCallback(callback, targetObj);
		}

		// Token: 0x0600185F RID: 6239 RVA: 0x0004D144 File Offset: 0x0004B344
		protected override void Finalize()
		{
			try
			{
				if (this._weakTargetObj.IsAllocated)
				{
					object targetObj = this._weakTargetObj.Target;
					if (targetObj == null)
					{
						this._weakTargetObj.Free();
						return;
					}
					try
					{
						if (!this._callback1(targetObj))
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

		// Token: 0x04001084 RID: 4228
		[Nullable(2)]
		private readonly Func<bool> _callback0;

		// Token: 0x04001085 RID: 4229
		[Nullable(new byte[] { 2, 1 })]
		private readonly Func<object, bool> _callback1;

		// Token: 0x04001086 RID: 4230
		private GCHandle _weakTargetObj;
	}
}
