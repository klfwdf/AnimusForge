using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000A3 RID: 163
	public abstract class IAchievementChangeListener : GalaxyTypeAwareListenerAchievementChange
	{
		// Token: 0x060007CB RID: 1995 RVA: 0x0000BDA8 File Offset: 0x00009FA8
		internal IAchievementChangeListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IAchievementChangeListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IAchievementChangeListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007CC RID: 1996 RVA: 0x0000BDD0 File Offset: 0x00009FD0
		public IAchievementChangeListener()
			: this(GalaxyInstancePINVOKE.new_IAchievementChangeListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060007CD RID: 1997 RVA: 0x0000BDF4 File Offset: 0x00009FF4
		internal static HandleRef getCPtr(IAchievementChangeListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007CE RID: 1998 RVA: 0x0000BE14 File Offset: 0x0000A014
		~IAchievementChangeListener()
		{
			this.Dispose();
		}

		// Token: 0x060007CF RID: 1999 RVA: 0x0000BE44 File Offset: 0x0000A044
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IAchievementChangeListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IAchievementChangeListener.listeners.ContainsKey(handle))
					{
						IAchievementChangeListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060007D0 RID: 2000
		public abstract void OnAchievementUnlocked(string name);

		// Token: 0x060007D1 RID: 2001 RVA: 0x0000BEF4 File Offset: 0x0000A0F4
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnAchievementUnlocked", IAchievementChangeListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IAchievementChangeListener.SwigDelegateIAchievementChangeListener_0(IAchievementChangeListener.SwigDirectorOnAchievementUnlocked);
			}
			GalaxyInstancePINVOKE.IAchievementChangeListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x060007D2 RID: 2002 RVA: 0x0000BF30 File Offset: 0x0000A130
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IAchievementChangeListener));
		}

		// Token: 0x060007D3 RID: 2003 RVA: 0x0000BF66 File Offset: 0x0000A166
		[MonoPInvokeCallback(typeof(IAchievementChangeListener.SwigDelegateIAchievementChangeListener_0))]
		private static void SwigDirectorOnAchievementUnlocked(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name)
		{
			if (IAchievementChangeListener.listeners.ContainsKey(cPtr))
			{
				IAchievementChangeListener.listeners[cPtr].OnAchievementUnlocked(name);
			}
		}

		// Token: 0x040000C7 RID: 199
		private static Dictionary<IntPtr, IAchievementChangeListener> listeners = new Dictionary<IntPtr, IAchievementChangeListener>();

		// Token: 0x040000C8 RID: 200
		private HandleRef swigCPtr;

		// Token: 0x040000C9 RID: 201
		private IAchievementChangeListener.SwigDelegateIAchievementChangeListener_0 swigDelegate0;

		// Token: 0x040000CA RID: 202
		private static Type[] swigMethodTypes0 = new Type[] { typeof(string) };

		// Token: 0x020000A4 RID: 164
		// (Invoke) Token: 0x060007D6 RID: 2006
		public delegate void SwigDelegateIAchievementChangeListener_0(IntPtr cPtr, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = Galaxy.Api.GalaxyInstancePINVOKE/UTF8Marshaler)] string name);
	}
}
