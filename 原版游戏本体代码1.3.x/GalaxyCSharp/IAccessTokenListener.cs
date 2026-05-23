using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x020000A1 RID: 161
	public abstract class IAccessTokenListener : GalaxyTypeAwareListenerAccessToken
	{
		// Token: 0x060007BD RID: 1981 RVA: 0x0000783C File Offset: 0x00005A3C
		internal IAccessTokenListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IAccessTokenListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IAccessTokenListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x060007BE RID: 1982 RVA: 0x00007864 File Offset: 0x00005A64
		public IAccessTokenListener()
			: this(GalaxyInstancePINVOKE.new_IAccessTokenListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x060007BF RID: 1983 RVA: 0x00007888 File Offset: 0x00005A88
		internal static HandleRef getCPtr(IAccessTokenListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x060007C0 RID: 1984 RVA: 0x000078A8 File Offset: 0x00005AA8
		~IAccessTokenListener()
		{
			this.Dispose();
		}

		// Token: 0x060007C1 RID: 1985 RVA: 0x000078D8 File Offset: 0x00005AD8
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IAccessTokenListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IAccessTokenListener.listeners.ContainsKey(handle))
					{
						IAccessTokenListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x060007C2 RID: 1986
		public abstract void OnAccessTokenChanged();

		// Token: 0x060007C3 RID: 1987 RVA: 0x00007988 File Offset: 0x00005B88
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnAccessTokenChanged", IAccessTokenListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IAccessTokenListener.SwigDelegateIAccessTokenListener_0(IAccessTokenListener.SwigDirectorOnAccessTokenChanged);
			}
			GalaxyInstancePINVOKE.IAccessTokenListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x060007C4 RID: 1988 RVA: 0x000079C4 File Offset: 0x00005BC4
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IAccessTokenListener));
		}

		// Token: 0x060007C5 RID: 1989 RVA: 0x000079FA File Offset: 0x00005BFA
		[MonoPInvokeCallback(typeof(IAccessTokenListener.SwigDelegateIAccessTokenListener_0))]
		private static void SwigDirectorOnAccessTokenChanged(IntPtr cPtr)
		{
			if (IAccessTokenListener.listeners.ContainsKey(cPtr))
			{
				IAccessTokenListener.listeners[cPtr].OnAccessTokenChanged();
			}
		}

		// Token: 0x040000C3 RID: 195
		private static Dictionary<IntPtr, IAccessTokenListener> listeners = new Dictionary<IntPtr, IAccessTokenListener>();

		// Token: 0x040000C4 RID: 196
		private HandleRef swigCPtr;

		// Token: 0x040000C5 RID: 197
		private IAccessTokenListener.SwigDelegateIAccessTokenListener_0 swigDelegate0;

		// Token: 0x040000C6 RID: 198
		private static Type[] swigMethodTypes0 = new Type[0];

		// Token: 0x020000A2 RID: 162
		// (Invoke) Token: 0x060007C8 RID: 1992
		public delegate void SwigDelegateIAccessTokenListener_0(IntPtr cPtr);
	}
}
