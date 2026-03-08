using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;

namespace System
{
	// Token: 0x0200015E RID: 350
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class WeakReference<T> : ISerializable where T : class
	{
		// Token: 0x060015BF RID: 5567 RVA: 0x000402DB File Offset: 0x0003E4DB
		[__DynamicallyInvokable]
		public WeakReference(T target)
			: this(target, false)
		{
		}

		// Token: 0x060015C0 RID: 5568 RVA: 0x000402E5 File Offset: 0x0003E4E5
		[__DynamicallyInvokable]
		public WeakReference(T target, bool trackResurrection)
		{
			this.Create(target, trackResurrection);
		}

		// Token: 0x060015C1 RID: 5569 RVA: 0x000402F8 File Offset: 0x0003E4F8
		internal WeakReference(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			T target = (T)((object)info.GetValue("TrackedObject", typeof(T)));
			bool boolean = info.GetBoolean("TrackResurrection");
			this.Create(target, boolean);
		}

		// Token: 0x060015C2 RID: 5570 RVA: 0x00040348 File Offset: 0x0003E548
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetTarget(out T target)
		{
			T target2 = this.Target;
			target = target2;
			return target2 != null;
		}

		// Token: 0x060015C3 RID: 5571 RVA: 0x0004036C File Offset: 0x0003E56C
		[__DynamicallyInvokable]
		public void SetTarget(T target)
		{
			this.Target = target;
		}

		// Token: 0x1700026D RID: 621
		// (get) Token: 0x060015C4 RID: 5572
		// (set) Token: 0x060015C5 RID: 5573
		private extern T Target
		{
			[SecuritySafeCritical]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[SecuritySafeCritical]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		// Token: 0x060015C6 RID: 5574
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		protected override extern void Finalize();

		// Token: 0x060015C7 RID: 5575 RVA: 0x00040378 File Offset: 0x0003E578
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.AddValue("TrackedObject", this.Target, typeof(T));
			info.AddValue("TrackResurrection", this.IsTrackResurrection());
		}

		// Token: 0x060015C8 RID: 5576
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void Create(T target, bool trackResurrection);

		// Token: 0x060015C9 RID: 5577
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool IsTrackResurrection();

		// Token: 0x04000749 RID: 1865
		internal IntPtr m_handle;
	}
}
