using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime
{
	// Token: 0x02000037 RID: 55
	[NullableContext(2)]
	[Nullable(0)]
	public struct DependentHandle : IDisposable
	{
		// Token: 0x0600021D RID: 541 RVA: 0x0000B7D0 File Offset: 0x000099D0
		public DependentHandle(object target, object dependent)
		{
			GCHandle targetHandle = GCHandle.Alloc(target, GCHandleType.WeakTrackResurrection);
			this.dependentHandle = DependentHandle.AllocDepHolder(targetHandle, dependent);
			GC.KeepAlive(target);
			this.allocated = true;
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0000B801 File Offset: 0x00009A01
		private static GCHandle AllocDepHolder(GCHandle targetHandle, object dependent)
		{
			return GCHandle.Alloc((dependent != null) ? new DependentHandle.DependentHolder(targetHandle, dependent) : null, GCHandleType.WeakTrackResurrection);
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600021F RID: 543 RVA: 0x0000B816 File Offset: 0x00009A16
		public bool IsAllocated
		{
			get
			{
				return this.allocated;
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000220 RID: 544 RVA: 0x0000B820 File Offset: 0x00009A20
		// (set) Token: 0x06000221 RID: 545 RVA: 0x0000B838 File Offset: 0x00009A38
		public object Target
		{
			get
			{
				if (!this.allocated)
				{
					throw new InvalidOperationException();
				}
				return this.UnsafeGetTarget();
			}
			set
			{
				if (!this.allocated || value != null)
				{
					throw new InvalidOperationException();
				}
				this.UnsafeSetTargetToNull();
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000222 RID: 546 RVA: 0x0000B853 File Offset: 0x00009A53
		// (set) Token: 0x06000223 RID: 547 RVA: 0x0000B876 File Offset: 0x00009A76
		public object Dependent
		{
			get
			{
				if (!this.allocated)
				{
					throw new InvalidOperationException();
				}
				DependentHandle.DependentHolder dependentHolder = this.UnsafeGetHolder();
				if (dependentHolder == null)
				{
					return null;
				}
				return dependentHolder.Dependent;
			}
			set
			{
				if (!this.allocated)
				{
					throw new InvalidOperationException();
				}
				this.UnsafeSetDependent(value);
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000224 RID: 548 RVA: 0x0000B88F File Offset: 0x00009A8F
		[TupleElementNames(new string[] { "Target", "Dependent" })]
		[Nullable(new byte[] { 0, 2, 2 })]
		public ValueTuple<object, object> TargetAndDependent
		{
			[return: TupleElementNames(new string[] { "Target", "Dependent" })]
			[return: Nullable(new byte[] { 0, 2, 2 })]
			get
			{
				if (!this.allocated)
				{
					throw new InvalidOperationException();
				}
				return new ValueTuple<object, object>(this.UnsafeGetTarget(), this.Dependent);
			}
		}

		// Token: 0x06000225 RID: 549 RVA: 0x0000B8B2 File Offset: 0x00009AB2
		private DependentHandle.DependentHolder UnsafeGetHolder()
		{
			return Unsafe.As<DependentHandle.DependentHolder>(this.dependentHandle.Target);
		}

		// Token: 0x06000226 RID: 550 RVA: 0x0000B8C4 File Offset: 0x00009AC4
		internal object UnsafeGetTarget()
		{
			DependentHandle.DependentHolder dependentHolder = this.UnsafeGetHolder();
			if (dependentHolder == null)
			{
				return null;
			}
			return dependentHolder.TargetHandle.Target;
		}

		// Token: 0x06000227 RID: 551 RVA: 0x0000B8DC File Offset: 0x00009ADC
		internal object UnsafeGetTargetAndDependent(out object dependent)
		{
			dependent = null;
			DependentHandle.DependentHolder dependentHolder = this.UnsafeGetHolder();
			if (dependentHolder == null)
			{
				return null;
			}
			object target = dependentHolder.TargetHandle.Target;
			if (target == null)
			{
				return null;
			}
			dependent = dependentHolder.Dependent;
			return target;
		}

		// Token: 0x06000228 RID: 552 RVA: 0x0000B912 File Offset: 0x00009B12
		internal void UnsafeSetTargetToNull()
		{
			this.Free();
		}

		// Token: 0x06000229 RID: 553 RVA: 0x0000B91C File Offset: 0x00009B1C
		internal void UnsafeSetDependent(object value)
		{
			DependentHandle.DependentHolder dependentHolder = this.UnsafeGetHolder();
			if (dependentHolder == null)
			{
				return;
			}
			if (!dependentHolder.TargetHandle.IsAllocated)
			{
				this.Free();
				return;
			}
			dependentHolder.Dependent = value;
		}

		// Token: 0x0600022A RID: 554 RVA: 0x0000B94F File Offset: 0x00009B4F
		private void FreeDependentHandle()
		{
			if (this.allocated)
			{
				DependentHandle.DependentHolder dependentHolder = this.UnsafeGetHolder();
				if (dependentHolder != null)
				{
					dependentHolder.TargetHandle.Free();
				}
				this.dependentHandle.Free();
			}
			this.allocated = false;
		}

		// Token: 0x0600022B RID: 555 RVA: 0x0000B985 File Offset: 0x00009B85
		private void Free()
		{
			this.FreeDependentHandle();
		}

		// Token: 0x0600022C RID: 556 RVA: 0x0000B98D File Offset: 0x00009B8D
		public void Dispose()
		{
			this.Free();
			this.allocated = false;
		}

		// Token: 0x04000076 RID: 118
		private GCHandle dependentHandle;

		// Token: 0x04000077 RID: 119
		private volatile bool allocated;

		// Token: 0x0200006B RID: 107
		[Nullable(0)]
		private sealed class DependentHolder : CriticalFinalizerObject
		{
			// Token: 0x17000047 RID: 71
			// (get) Token: 0x060002E4 RID: 740 RVA: 0x0000D2FC File Offset: 0x0000B4FC
			// (set) Token: 0x060002E5 RID: 741 RVA: 0x0000D31C File Offset: 0x0000B51C
			public object Dependent
			{
				get
				{
					return GCHandle.FromIntPtr(this.dependent).Target;
				}
				set
				{
					IntPtr value2 = GCHandle.ToIntPtr(GCHandle.Alloc(value, GCHandleType.Normal));
					IntPtr intPtr;
					do
					{
						intPtr = this.dependent;
					}
					while (Interlocked.CompareExchange(ref this.dependent, value2, intPtr) == intPtr);
					GCHandle.FromIntPtr(intPtr).Free();
				}
			}

			// Token: 0x060002E6 RID: 742 RVA: 0x0000D360 File Offset: 0x0000B560
			[NullableContext(1)]
			public DependentHolder(GCHandle targetHandle, object dependent)
			{
				this.TargetHandle = targetHandle;
				this.dependent = GCHandle.ToIntPtr(GCHandle.Alloc(dependent, GCHandleType.Normal));
			}

			// Token: 0x060002E7 RID: 743 RVA: 0x0000D384 File Offset: 0x0000B584
			~DependentHolder()
			{
				if (!AppDomain.CurrentDomain.IsFinalizingForUnload() && !Environment.HasShutdownStarted && this.TargetHandle.IsAllocated && this.TargetHandle.Target != null)
				{
					GC.ReRegisterForFinalize(this);
				}
				else
				{
					GCHandle.FromIntPtr(this.dependent).Free();
				}
			}

			// Token: 0x040000CC RID: 204
			public GCHandle TargetHandle;

			// Token: 0x040000CD RID: 205
			private IntPtr dependent;
		}
	}
}
