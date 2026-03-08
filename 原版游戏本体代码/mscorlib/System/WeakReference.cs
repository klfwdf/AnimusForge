using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace System
{
	// Token: 0x0200015D RID: 349
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
	[Serializable]
	public class WeakReference : ISerializable
	{
		// Token: 0x060015B4 RID: 5556 RVA: 0x0004022F File Offset: 0x0003E42F
		[__DynamicallyInvokable]
		public WeakReference(object target)
			: this(target, false)
		{
		}

		// Token: 0x060015B5 RID: 5557 RVA: 0x00040239 File Offset: 0x0003E439
		[__DynamicallyInvokable]
		public WeakReference(object target, bool trackResurrection)
		{
			this.Create(target, trackResurrection);
		}

		// Token: 0x060015B6 RID: 5558 RVA: 0x0004024C File Offset: 0x0003E44C
		protected WeakReference(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			object value = info.GetValue("TrackedObject", typeof(object));
			bool boolean = info.GetBoolean("TrackResurrection");
			this.Create(value, boolean);
		}

		// Token: 0x1700026A RID: 618
		// (get) Token: 0x060015B7 RID: 5559
		[__DynamicallyInvokable]
		public virtual extern bool IsAlive
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
		}

		// Token: 0x1700026B RID: 619
		// (get) Token: 0x060015B8 RID: 5560 RVA: 0x00040297 File Offset: 0x0003E497
		[__DynamicallyInvokable]
		public virtual bool TrackResurrection
		{
			[__DynamicallyInvokable]
			get
			{
				return this.IsTrackResurrection();
			}
		}

		// Token: 0x1700026C RID: 620
		// (get) Token: 0x060015B9 RID: 5561
		// (set) Token: 0x060015BA RID: 5562
		[__DynamicallyInvokable]
		public virtual extern object Target
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			[MethodImpl(MethodImplOptions.InternalCall)]
			get;
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			[MethodImpl(MethodImplOptions.InternalCall)]
			set;
		}

		// Token: 0x060015BB RID: 5563
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		protected override extern void Finalize();

		// Token: 0x060015BC RID: 5564 RVA: 0x0004029F File Offset: 0x0003E49F
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.AddValue("TrackedObject", this.Target, typeof(object));
			info.AddValue("TrackResurrection", this.IsTrackResurrection());
		}

		// Token: 0x060015BD RID: 5565
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern void Create(object target, bool trackResurrection);

		// Token: 0x060015BE RID: 5566
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private extern bool IsTrackResurrection();

		// Token: 0x04000748 RID: 1864
		internal IntPtr m_handle;
	}
}
