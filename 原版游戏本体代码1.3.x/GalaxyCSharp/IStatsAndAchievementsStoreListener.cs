using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000150 RID: 336
	public abstract class IStatsAndAchievementsStoreListener : GalaxyTypeAwareListenerStatsAndAchievementsStore
	{
		// Token: 0x06000C57 RID: 3159 RVA: 0x00014EDC File Offset: 0x000130DC
		internal IStatsAndAchievementsStoreListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IStatsAndAchievementsStoreListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IStatsAndAchievementsStoreListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000C58 RID: 3160 RVA: 0x00014F04 File Offset: 0x00013104
		public IStatsAndAchievementsStoreListener()
			: this(GalaxyInstancePINVOKE.new_IStatsAndAchievementsStoreListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000C59 RID: 3161 RVA: 0x00014F28 File Offset: 0x00013128
		internal static HandleRef getCPtr(IStatsAndAchievementsStoreListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000C5A RID: 3162 RVA: 0x00014F48 File Offset: 0x00013148
		~IStatsAndAchievementsStoreListener()
		{
			this.Dispose();
		}

		// Token: 0x06000C5B RID: 3163 RVA: 0x00014F78 File Offset: 0x00013178
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IStatsAndAchievementsStoreListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IStatsAndAchievementsStoreListener.listeners.ContainsKey(handle))
					{
						IStatsAndAchievementsStoreListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000C5C RID: 3164
		public abstract void OnUserStatsAndAchievementsStoreSuccess();

		// Token: 0x06000C5D RID: 3165
		public abstract void OnUserStatsAndAchievementsStoreFailure(IStatsAndAchievementsStoreListener.FailureReason failureReason);

		// Token: 0x06000C5E RID: 3166 RVA: 0x00015028 File Offset: 0x00013228
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnUserStatsAndAchievementsStoreSuccess", IStatsAndAchievementsStoreListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IStatsAndAchievementsStoreListener.SwigDelegateIStatsAndAchievementsStoreListener_0(IStatsAndAchievementsStoreListener.SwigDirectorOnUserStatsAndAchievementsStoreSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnUserStatsAndAchievementsStoreFailure", IStatsAndAchievementsStoreListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IStatsAndAchievementsStoreListener.SwigDelegateIStatsAndAchievementsStoreListener_1(IStatsAndAchievementsStoreListener.SwigDirectorOnUserStatsAndAchievementsStoreFailure);
			}
			GalaxyInstancePINVOKE.IStatsAndAchievementsStoreListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000C5F RID: 3167 RVA: 0x0001509C File Offset: 0x0001329C
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IStatsAndAchievementsStoreListener));
		}

		// Token: 0x06000C60 RID: 3168 RVA: 0x000150D2 File Offset: 0x000132D2
		[MonoPInvokeCallback(typeof(IStatsAndAchievementsStoreListener.SwigDelegateIStatsAndAchievementsStoreListener_0))]
		private static void SwigDirectorOnUserStatsAndAchievementsStoreSuccess(IntPtr cPtr)
		{
			if (IStatsAndAchievementsStoreListener.listeners.ContainsKey(cPtr))
			{
				IStatsAndAchievementsStoreListener.listeners[cPtr].OnUserStatsAndAchievementsStoreSuccess();
			}
		}

		// Token: 0x06000C61 RID: 3169 RVA: 0x000150F4 File Offset: 0x000132F4
		[MonoPInvokeCallback(typeof(IStatsAndAchievementsStoreListener.SwigDelegateIStatsAndAchievementsStoreListener_1))]
		private static void SwigDirectorOnUserStatsAndAchievementsStoreFailure(IntPtr cPtr, int failureReason)
		{
			if (IStatsAndAchievementsStoreListener.listeners.ContainsKey(cPtr))
			{
				IStatsAndAchievementsStoreListener.listeners[cPtr].OnUserStatsAndAchievementsStoreFailure((IStatsAndAchievementsStoreListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000261 RID: 609
		private static Dictionary<IntPtr, IStatsAndAchievementsStoreListener> listeners = new Dictionary<IntPtr, IStatsAndAchievementsStoreListener>();

		// Token: 0x04000262 RID: 610
		private HandleRef swigCPtr;

		// Token: 0x04000263 RID: 611
		private IStatsAndAchievementsStoreListener.SwigDelegateIStatsAndAchievementsStoreListener_0 swigDelegate0;

		// Token: 0x04000264 RID: 612
		private IStatsAndAchievementsStoreListener.SwigDelegateIStatsAndAchievementsStoreListener_1 swigDelegate1;

		// Token: 0x04000265 RID: 613
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x04000266 RID: 614
		private static Type[] swigMethodTypes1 = new Type[] { typeof(IStatsAndAchievementsStoreListener.FailureReason) };

		// Token: 0x02000151 RID: 337
		// (Invoke) Token: 0x06000C64 RID: 3172
		public delegate void SwigDelegateIStatsAndAchievementsStoreListener_0(IntPtr cPtr);

		// Token: 0x02000152 RID: 338
		// (Invoke) Token: 0x06000C68 RID: 3176
		public delegate void SwigDelegateIStatsAndAchievementsStoreListener_1(IntPtr cPtr, int failureReason);

		// Token: 0x02000153 RID: 339
		public enum FailureReason
		{
			// Token: 0x04000268 RID: 616
			FAILURE_REASON_UNDEFINED,
			// Token: 0x04000269 RID: 617
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
