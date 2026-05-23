using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000166 RID: 358
	public abstract class IUserStatsAndAchievementsRetrieveListener : GalaxyTypeAwareListenerUserStatsAndAchievementsRetrieve
	{
		// Token: 0x06000D21 RID: 3361 RVA: 0x00015C10 File Offset: 0x00013E10
		internal IUserStatsAndAchievementsRetrieveListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IUserStatsAndAchievementsRetrieveListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IUserStatsAndAchievementsRetrieveListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000D22 RID: 3362 RVA: 0x00015C38 File Offset: 0x00013E38
		public IUserStatsAndAchievementsRetrieveListener()
			: this(GalaxyInstancePINVOKE.new_IUserStatsAndAchievementsRetrieveListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000D23 RID: 3363 RVA: 0x00015C5C File Offset: 0x00013E5C
		internal static HandleRef getCPtr(IUserStatsAndAchievementsRetrieveListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000D24 RID: 3364 RVA: 0x00015C7C File Offset: 0x00013E7C
		~IUserStatsAndAchievementsRetrieveListener()
		{
			this.Dispose();
		}

		// Token: 0x06000D25 RID: 3365 RVA: 0x00015CAC File Offset: 0x00013EAC
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IUserStatsAndAchievementsRetrieveListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IUserStatsAndAchievementsRetrieveListener.listeners.ContainsKey(handle))
					{
						IUserStatsAndAchievementsRetrieveListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000D26 RID: 3366
		public abstract void OnUserStatsAndAchievementsRetrieveSuccess(GalaxyID userID);

		// Token: 0x06000D27 RID: 3367
		public abstract void OnUserStatsAndAchievementsRetrieveFailure(GalaxyID userID, IUserStatsAndAchievementsRetrieveListener.FailureReason failureReason);

		// Token: 0x06000D28 RID: 3368 RVA: 0x00015D5C File Offset: 0x00013F5C
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnUserStatsAndAchievementsRetrieveSuccess", IUserStatsAndAchievementsRetrieveListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IUserStatsAndAchievementsRetrieveListener.SwigDelegateIUserStatsAndAchievementsRetrieveListener_0(IUserStatsAndAchievementsRetrieveListener.SwigDirectorOnUserStatsAndAchievementsRetrieveSuccess);
			}
			if (this.SwigDerivedClassHasMethod("OnUserStatsAndAchievementsRetrieveFailure", IUserStatsAndAchievementsRetrieveListener.swigMethodTypes1))
			{
				this.swigDelegate1 = new IUserStatsAndAchievementsRetrieveListener.SwigDelegateIUserStatsAndAchievementsRetrieveListener_1(IUserStatsAndAchievementsRetrieveListener.SwigDirectorOnUserStatsAndAchievementsRetrieveFailure);
			}
			GalaxyInstancePINVOKE.IUserStatsAndAchievementsRetrieveListener_director_connect(this.swigCPtr, this.swigDelegate0, this.swigDelegate1);
		}

		// Token: 0x06000D29 RID: 3369 RVA: 0x00015DD0 File Offset: 0x00013FD0
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IUserStatsAndAchievementsRetrieveListener));
		}

		// Token: 0x06000D2A RID: 3370 RVA: 0x00015E06 File Offset: 0x00014006
		[MonoPInvokeCallback(typeof(IUserStatsAndAchievementsRetrieveListener.SwigDelegateIUserStatsAndAchievementsRetrieveListener_0))]
		private static void SwigDirectorOnUserStatsAndAchievementsRetrieveSuccess(IntPtr cPtr, IntPtr userID)
		{
			if (IUserStatsAndAchievementsRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IUserStatsAndAchievementsRetrieveListener.listeners[cPtr].OnUserStatsAndAchievementsRetrieveSuccess(new GalaxyID(new GalaxyID(userID, false).ToUint64()));
			}
		}

		// Token: 0x06000D2B RID: 3371 RVA: 0x00015E39 File Offset: 0x00014039
		[MonoPInvokeCallback(typeof(IUserStatsAndAchievementsRetrieveListener.SwigDelegateIUserStatsAndAchievementsRetrieveListener_1))]
		private static void SwigDirectorOnUserStatsAndAchievementsRetrieveFailure(IntPtr cPtr, IntPtr userID, int failureReason)
		{
			if (IUserStatsAndAchievementsRetrieveListener.listeners.ContainsKey(cPtr))
			{
				IUserStatsAndAchievementsRetrieveListener.listeners[cPtr].OnUserStatsAndAchievementsRetrieveFailure(new GalaxyID(new GalaxyID(userID, false).ToUint64()), (IUserStatsAndAchievementsRetrieveListener.FailureReason)failureReason);
			}
		}

		// Token: 0x04000296 RID: 662
		private static Dictionary<IntPtr, IUserStatsAndAchievementsRetrieveListener> listeners = new Dictionary<IntPtr, IUserStatsAndAchievementsRetrieveListener>();

		// Token: 0x04000297 RID: 663
		private HandleRef swigCPtr;

		// Token: 0x04000298 RID: 664
		private IUserStatsAndAchievementsRetrieveListener.SwigDelegateIUserStatsAndAchievementsRetrieveListener_0 swigDelegate0;

		// Token: 0x04000299 RID: 665
		private IUserStatsAndAchievementsRetrieveListener.SwigDelegateIUserStatsAndAchievementsRetrieveListener_1 swigDelegate1;

		// Token: 0x0400029A RID: 666
		private static Type[] swigMethodTypes0 = new Type[] { typeof(GalaxyID) };

		// Token: 0x0400029B RID: 667
		private static Type[] swigMethodTypes1 = new Type[]
		{
			typeof(GalaxyID),
			typeof(IUserStatsAndAchievementsRetrieveListener.FailureReason)
		};

		// Token: 0x02000167 RID: 359
		// (Invoke) Token: 0x06000D2E RID: 3374
		public delegate void SwigDelegateIUserStatsAndAchievementsRetrieveListener_0(IntPtr cPtr, IntPtr userID);

		// Token: 0x02000168 RID: 360
		// (Invoke) Token: 0x06000D32 RID: 3378
		public delegate void SwigDelegateIUserStatsAndAchievementsRetrieveListener_1(IntPtr cPtr, IntPtr userID, int failureReason);

		// Token: 0x02000169 RID: 361
		public enum FailureReason
		{
			// Token: 0x0400029D RID: 669
			FAILURE_REASON_UNDEFINED,
			// Token: 0x0400029E RID: 670
			FAILURE_REASON_CONNECTION_FAILURE
		}
	}
}
