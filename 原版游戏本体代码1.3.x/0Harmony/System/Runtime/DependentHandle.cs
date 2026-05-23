using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime
{
	// Token: 0x020004A5 RID: 1189
	[NullableContext(2)]
	[Nullable(0)]
	internal struct DependentHandle : IDisposable
	{
		// Token: 0x06001A8C RID: 6796 RVA: 0x00056CCC File Offset: 0x00054ECC
		public DependentHandle(object target, object dependent)
		{
			GCHandle targetHandle = GCHandle.Alloc(target, GCHandleType.WeakTrackResurrection);
			this.dependentHandle = DependentHandle.AllocDepHolder(targetHandle, dependent);
			GC.KeepAlive(target);
			this.allocated = true;
		}

		// Token: 0x06001A8D RID: 6797 RVA: 0x00056CFD File Offset: 0x00054EFD
		private static GCHandle AllocDepHolder(GCHandle targetHandle, object dependent)
		{
			return GCHandle.Alloc((dependent != null) ? new DependentHandle.DependentHolder(targetHandle, dependent) : null, GCHandleType.WeakTrackResurrection);
		}

		// Token: 0x170005D6 RID: 1494
		// (get) Token: 0x06001A8E RID: 6798 RVA: 0x00056D12 File Offset: 0x00054F12
		public bool IsAllocated
		{
			get
			{
				return this.allocated;
			}
		}

		// Token: 0x170005D7 RID: 1495
		// (get) Token: 0x06001A8F RID: 6799 RVA: 0x00056D1C File Offset: 0x00054F1C
		// (set) Token: 0x06001A90 RID: 6800 RVA: 0x00056D34 File Offset: 0x00054F34
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

		// Token: 0x170005D8 RID: 1496
		// (get) Token: 0x06001A91 RID: 6801 RVA: 0x00056D4F File Offset: 0x00054F4F
		// (set) Token: 0x06001A92 RID: 6802 RVA: 0x00056D72 File Offset: 0x00054F72
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

		// Token: 0x170005D9 RID: 1497
		// (get) Token: 0x06001A93 RID: 6803 RVA: 0x00056D8B File Offset: 0x00054F8B
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

		// Token: 0x06001A94 RID: 6804 RVA: 0x00056DAE File Offset: 0x00054FAE
		private DependentHandle.DependentHolder UnsafeGetHolder()
		{
			return Unsafe.As<DependentHandle.DependentHolder>(this.dependentHandle.Target);
		}

		// Token: 0x06001A95 RID: 6805 RVA: 0x00056DC0 File Offset: 0x00054FC0
		internal object UnsafeGetTarget()
		{
			DependentHandle.DependentHolder dependentHolder = this.UnsafeGetHolder();
			if (dependentHolder == null)
			{
				return null;
			}
			return dependentHolder.TargetHandle.Target;
		}

		// Token: 0x06001A96 RID: 6806 RVA: 0x00056DD8 File Offset: 0x00054FD8
		internal object UnsafeGetTargetAndDependent(out object dependent)
		{
			dependent = null;
			DependentHandle.DependentHolder holder = this.UnsafeGetHolder();
			if (holder == null)
			{
				return null;
			}
			object target = holder.TargetHandle.Target;
			if (target == null)
			{
				return null;
			}
			dependent = holder.Dependent;
			return target;
		}

		// Token: 0x06001A97 RID: 6807 RVA: 0x00056E0E File Offset: 0x0005500E
		internal void UnsafeSetTargetToNull()
		{
			this.Free();
		}

		// Token: 0x06001A98 RID: 6808 RVA: 0x00056E18 File Offset: 0x00055018
		internal void UnsafeSetDependent(object value)
		{
			DependentHandle.DependentHolder holder = this.UnsafeGetHolder();
			if (holder == null)
			{
				return;
			}
			if (!holder.TargetHandle.IsAllocated)
			{
				this.Free();
				return;
			}
			holder.Dependent = value;
		}

		// Token: 0x06001A99 RID: 6809 RVA: 0x00056E4B File Offset: 0x0005504B
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

		// Token: 0x06001A9A RID: 6810 RVA: 0x00056E81 File Offset: 0x00055081
		private void Free()
		{
			this.FreeDependentHandle();
		}

		// Token: 0x06001A9B RID: 6811 RVA: 0x00056E89 File Offset: 0x00055089
		public void Dispose()
		{
			this.Free();
			this.allocated = false;
		}

		// Token: 0x04001111 RID: 4369
		private GCHandle dependentHandle;

		// Token: 0x04001112 RID: 4370
		private volatile bool allocated;

		// Token: 0x020004A6 RID: 1190
		[Nullable(0)]
		private sealed class DependentHolder : CriticalFinalizerObject
		{
			// Token: 0x170005DA RID: 1498
			// (get) Token: 0x06001A9C RID: 6812 RVA: 0x00056E9C File Offset: 0x0005509C
			// (set) Token: 0x06001A9D RID: 6813 RVA: 0x00056EBC File Offset: 0x000550BC
			public object Dependent
			{
				get
				{
					return GCHandle.FromIntPtr(this.dependent).Target;
				}
				set
				{
					IntPtr newHandle = GCHandle.ToIntPtr(GCHandle.Alloc(value, GCHandleType.Normal));
					IntPtr oldHandle;
					do
					{
						oldHandle = this.dependent;
					}
					while (Interlocked.CompareExchange(ref this.dependent, newHandle, oldHandle) == oldHandle);
					GCHandle.FromIntPtr(oldHandle).Free();
				}
			}

			// Token: 0x06001A9E RID: 6814 RVA: 0x00056F00 File Offset: 0x00055100
			[NullableContext(1)]
			public DependentHolder(GCHandle targetHandle, object dependent)
			{
				this.TargetHandle = targetHandle;
				this.dependent = GCHandle.ToIntPtr(GCHandle.Alloc(dependent, GCHandleType.Normal));
			}

			// Token: 0x06001A9F RID: 6815 RVA: 0x00056F24 File Offset: 0x00055124
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

			// Token: 0x04001113 RID: 4371
			public GCHandle TargetHandle;

			// Token: 0x04001114 RID: 4372
			private IntPtr dependent;
		}
	}
}
