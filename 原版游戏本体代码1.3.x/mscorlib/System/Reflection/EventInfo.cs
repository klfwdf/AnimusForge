using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Permissions;

namespace System.Reflection
{
	// Token: 0x020005E2 RID: 1506
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_EventInfo))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[Serializable]
	public abstract class EventInfo : MemberInfo, _EventInfo
	{
		// Token: 0x060045B6 RID: 17846 RVA: 0x001012C5 File Offset: 0x000FF4C5
		[__DynamicallyInvokable]
		public static bool operator ==(EventInfo left, EventInfo right)
		{
			return left == right || (left != null && right != null && !(left is RuntimeEventInfo) && !(right is RuntimeEventInfo) && left.Equals(right));
		}

		// Token: 0x060045B7 RID: 17847 RVA: 0x001012EC File Offset: 0x000FF4EC
		[__DynamicallyInvokable]
		public static bool operator !=(EventInfo left, EventInfo right)
		{
			return !(left == right);
		}

		// Token: 0x060045B8 RID: 17848 RVA: 0x001012F8 File Offset: 0x000FF4F8
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x060045B9 RID: 17849 RVA: 0x00101301 File Offset: 0x000FF501
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x17000A5E RID: 2654
		// (get) Token: 0x060045BA RID: 17850 RVA: 0x00101309 File Offset: 0x000FF509
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Event;
			}
		}

		// Token: 0x060045BB RID: 17851 RVA: 0x0010130C File Offset: 0x000FF50C
		public virtual MethodInfo[] GetOtherMethods(bool nonPublic)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060045BC RID: 17852
		[__DynamicallyInvokable]
		public abstract MethodInfo GetAddMethod(bool nonPublic);

		// Token: 0x060045BD RID: 17853
		[__DynamicallyInvokable]
		public abstract MethodInfo GetRemoveMethod(bool nonPublic);

		// Token: 0x060045BE RID: 17854
		[__DynamicallyInvokable]
		public abstract MethodInfo GetRaiseMethod(bool nonPublic);

		// Token: 0x17000A5F RID: 2655
		// (get) Token: 0x060045BF RID: 17855
		[__DynamicallyInvokable]
		public abstract EventAttributes Attributes
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000A60 RID: 2656
		// (get) Token: 0x060045C0 RID: 17856 RVA: 0x00101313 File Offset: 0x000FF513
		[__DynamicallyInvokable]
		public virtual MethodInfo AddMethod
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetAddMethod(true);
			}
		}

		// Token: 0x17000A61 RID: 2657
		// (get) Token: 0x060045C1 RID: 17857 RVA: 0x0010131C File Offset: 0x000FF51C
		[__DynamicallyInvokable]
		public virtual MethodInfo RemoveMethod
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetRemoveMethod(true);
			}
		}

		// Token: 0x17000A62 RID: 2658
		// (get) Token: 0x060045C2 RID: 17858 RVA: 0x00101325 File Offset: 0x000FF525
		[__DynamicallyInvokable]
		public virtual MethodInfo RaiseMethod
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetRaiseMethod(true);
			}
		}

		// Token: 0x060045C3 RID: 17859 RVA: 0x0010132E File Offset: 0x000FF52E
		public MethodInfo[] GetOtherMethods()
		{
			return this.GetOtherMethods(false);
		}

		// Token: 0x060045C4 RID: 17860 RVA: 0x00101337 File Offset: 0x000FF537
		[__DynamicallyInvokable]
		public MethodInfo GetAddMethod()
		{
			return this.GetAddMethod(false);
		}

		// Token: 0x060045C5 RID: 17861 RVA: 0x00101340 File Offset: 0x000FF540
		[__DynamicallyInvokable]
		public MethodInfo GetRemoveMethod()
		{
			return this.GetRemoveMethod(false);
		}

		// Token: 0x060045C6 RID: 17862 RVA: 0x00101349 File Offset: 0x000FF549
		[__DynamicallyInvokable]
		public MethodInfo GetRaiseMethod()
		{
			return this.GetRaiseMethod(false);
		}

		// Token: 0x060045C7 RID: 17863 RVA: 0x00101354 File Offset: 0x000FF554
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public virtual void AddEventHandler(object target, Delegate handler)
		{
			MethodInfo addMethod = this.GetAddMethod();
			if (addMethod == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoPublicAddMethod"));
			}
			if (addMethod.ReturnType == typeof(EventRegistrationToken))
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotSupportedOnWinRTEvent"));
			}
			addMethod.Invoke(target, new object[] { handler });
		}

		// Token: 0x060045C8 RID: 17864 RVA: 0x001013BC File Offset: 0x000FF5BC
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public virtual void RemoveEventHandler(object target, Delegate handler)
		{
			MethodInfo removeMethod = this.GetRemoveMethod();
			if (removeMethod == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoPublicRemoveMethod"));
			}
			ParameterInfo[] parametersNoCopy = removeMethod.GetParametersNoCopy();
			if (parametersNoCopy[0].ParameterType == typeof(EventRegistrationToken))
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotSupportedOnWinRTEvent"));
			}
			removeMethod.Invoke(target, new object[] { handler });
		}

		// Token: 0x17000A63 RID: 2659
		// (get) Token: 0x060045C9 RID: 17865 RVA: 0x0010142C File Offset: 0x000FF62C
		[__DynamicallyInvokable]
		public virtual Type EventHandlerType
		{
			[__DynamicallyInvokable]
			get
			{
				MethodInfo addMethod = this.GetAddMethod(true);
				ParameterInfo[] parametersNoCopy = addMethod.GetParametersNoCopy();
				Type typeFromHandle = typeof(Delegate);
				for (int i = 0; i < parametersNoCopy.Length; i++)
				{
					Type parameterType = parametersNoCopy[i].ParameterType;
					if (parameterType.IsSubclassOf(typeFromHandle))
					{
						return parameterType;
					}
				}
				return null;
			}
		}

		// Token: 0x17000A64 RID: 2660
		// (get) Token: 0x060045CA RID: 17866 RVA: 0x00101479 File Offset: 0x000FF679
		[__DynamicallyInvokable]
		public bool IsSpecialName
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & EventAttributes.SpecialName) > EventAttributes.None;
			}
		}

		// Token: 0x17000A65 RID: 2661
		// (get) Token: 0x060045CB RID: 17867 RVA: 0x0010148C File Offset: 0x000FF68C
		[__DynamicallyInvokable]
		public virtual bool IsMulticast
		{
			[__DynamicallyInvokable]
			get
			{
				Type eventHandlerType = this.EventHandlerType;
				Type typeFromHandle = typeof(MulticastDelegate);
				return typeFromHandle.IsAssignableFrom(eventHandlerType);
			}
		}

		// Token: 0x060045CC RID: 17868 RVA: 0x001014B2 File Offset: 0x000FF6B2
		Type _EventInfo.GetType()
		{
			return base.GetType();
		}

		// Token: 0x060045CD RID: 17869 RVA: 0x001014BA File Offset: 0x000FF6BA
		void _EventInfo.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060045CE RID: 17870 RVA: 0x001014C1 File Offset: 0x000FF6C1
		void _EventInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060045CF RID: 17871 RVA: 0x001014C8 File Offset: 0x000FF6C8
		void _EventInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060045D0 RID: 17872 RVA: 0x001014CF File Offset: 0x000FF6CF
		void _EventInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}
	}
}
