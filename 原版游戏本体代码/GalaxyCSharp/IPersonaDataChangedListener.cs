using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Galaxy.Api
{
	// Token: 0x02000133 RID: 307
	public abstract class IPersonaDataChangedListener : GalaxyTypeAwareListenerPersonaDataChanged
	{
		// Token: 0x06000B88 RID: 2952 RVA: 0x00013674 File Offset: 0x00011874
		internal IPersonaDataChangedListener(IntPtr cPtr, bool cMemoryOwn)
			: base(GalaxyInstancePINVOKE.IPersonaDataChangedListener_SWIGUpcast(cPtr), cMemoryOwn)
		{
			IPersonaDataChangedListener.listeners.Add(cPtr, this);
			this.swigCPtr = new HandleRef(this, cPtr);
		}

		// Token: 0x06000B89 RID: 2953 RVA: 0x0001369C File Offset: 0x0001189C
		public IPersonaDataChangedListener()
			: this(GalaxyInstancePINVOKE.new_IPersonaDataChangedListener(), true)
		{
			if (GalaxyInstancePINVOKE.SWIGPendingException.Pending)
			{
				throw GalaxyInstancePINVOKE.SWIGPendingException.Retrieve();
			}
			this.SwigDirectorConnect();
		}

		// Token: 0x06000B8A RID: 2954 RVA: 0x000136C0 File Offset: 0x000118C0
		internal static HandleRef getCPtr(IPersonaDataChangedListener obj)
		{
			return (obj != null) ? obj.swigCPtr : new HandleRef(null, IntPtr.Zero);
		}

		// Token: 0x06000B8B RID: 2955 RVA: 0x000136E0 File Offset: 0x000118E0
		~IPersonaDataChangedListener()
		{
			this.Dispose();
		}

		// Token: 0x06000B8C RID: 2956 RVA: 0x00013710 File Offset: 0x00011910
		public override void Dispose()
		{
			lock (this)
			{
				if (this.swigCPtr.Handle != IntPtr.Zero)
				{
					if (this.swigCMemOwn)
					{
						this.swigCMemOwn = false;
						GalaxyInstancePINVOKE.delete_IPersonaDataChangedListener(this.swigCPtr);
					}
					IntPtr handle = this.swigCPtr.Handle;
					if (IPersonaDataChangedListener.listeners.ContainsKey(handle))
					{
						IPersonaDataChangedListener.listeners.Remove(handle);
					}
					this.swigCPtr = new HandleRef(null, IntPtr.Zero);
				}
				GC.SuppressFinalize(this);
				base.Dispose();
			}
		}

		// Token: 0x06000B8D RID: 2957
		public abstract void OnPersonaDataChanged(GalaxyID userID, uint personaStateChange);

		// Token: 0x06000B8E RID: 2958 RVA: 0x000137C0 File Offset: 0x000119C0
		private void SwigDirectorConnect()
		{
			if (this.SwigDerivedClassHasMethod("OnPersonaDataChanged", IPersonaDataChangedListener.swigMethodTypes0))
			{
				this.swigDelegate0 = new IPersonaDataChangedListener.SwigDelegateIPersonaDataChangedListener_0(IPersonaDataChangedListener.SwigDirectorOnPersonaDataChanged);
			}
			GalaxyInstancePINVOKE.IPersonaDataChangedListener_director_connect(this.swigCPtr, this.swigDelegate0);
		}

		// Token: 0x06000B8F RID: 2959 RVA: 0x000137FC File Offset: 0x000119FC
		private bool SwigDerivedClassHasMethod(string methodName, Type[] methodTypes)
		{
			MethodInfo method = base.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, methodTypes, null);
			return method.DeclaringType.IsSubclassOf(typeof(IPersonaDataChangedListener));
		}

		// Token: 0x06000B90 RID: 2960 RVA: 0x00013832 File Offset: 0x00011A32
		[MonoPInvokeCallback(typeof(IPersonaDataChangedListener.SwigDelegateIPersonaDataChangedListener_0))]
		private static void SwigDirectorOnPersonaDataChanged(IntPtr cPtr, IntPtr userID, uint personaStateChange)
		{
			if (IPersonaDataChangedListener.listeners.ContainsKey(cPtr))
			{
				IPersonaDataChangedListener.listeners[cPtr].OnPersonaDataChanged(new GalaxyID(new GalaxyID(userID, false).ToUint64()), personaStateChange);
			}
		}

		// Token: 0x04000216 RID: 534
		private static Dictionary<IntPtr, IPersonaDataChangedListener> listeners = new Dictionary<IntPtr, IPersonaDataChangedListener>();

		// Token: 0x04000217 RID: 535
		private HandleRef swigCPtr;

		// Token: 0x04000218 RID: 536
		private IPersonaDataChangedListener.SwigDelegateIPersonaDataChangedListener_0 swigDelegate0;

		// Token: 0x04000219 RID: 537
		private static Type[] swigMethodTypes0 = new Type[]
		{
			typeof(GalaxyID),
			typeof(uint)
		};

		// Token: 0x02000134 RID: 308
		// (Invoke) Token: 0x06000B93 RID: 2963
		public delegate void SwigDelegateIPersonaDataChangedListener_0(IntPtr cPtr, IntPtr userID, uint personaStateChange);

		// Token: 0x02000135 RID: 309
		public enum PersonaStateChange
		{
			// Token: 0x0400021B RID: 539
			PERSONA_CHANGE_NONE,
			// Token: 0x0400021C RID: 540
			PERSONA_CHANGE_NAME,
			// Token: 0x0400021D RID: 541
			PERSONA_CHANGE_AVATAR,
			// Token: 0x0400021E RID: 542
			PERSONA_CHANGE_AVATAR_DOWNLOADED_IMAGE_SMALL = 4,
			// Token: 0x0400021F RID: 543
			PERSONA_CHANGE_AVATAR_DOWNLOADED_IMAGE_MEDIUM = 8,
			// Token: 0x04000220 RID: 544
			PERSONA_CHANGE_AVATAR_DOWNLOADED_IMAGE_LARGE = 16,
			// Token: 0x04000221 RID: 545
			PERSONA_CHANGE_AVATAR_DOWNLOADED_IMAGE_ANY = 28
		}
	}
}
