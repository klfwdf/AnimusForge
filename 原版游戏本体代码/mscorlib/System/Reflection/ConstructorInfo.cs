using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Reflection
{
	// Token: 0x020005D2 RID: 1490
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_ConstructorInfo))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[Serializable]
	public abstract class ConstructorInfo : MethodBase, _ConstructorInfo
	{
		// Token: 0x060044E8 RID: 17640 RVA: 0x000FD388 File Offset: 0x000FB588
		[__DynamicallyInvokable]
		public static bool operator ==(ConstructorInfo left, ConstructorInfo right)
		{
			return left == right || (left != null && right != null && !(left is RuntimeConstructorInfo) && !(right is RuntimeConstructorInfo) && left.Equals(right));
		}

		// Token: 0x060044E9 RID: 17641 RVA: 0x000FD3AF File Offset: 0x000FB5AF
		[__DynamicallyInvokable]
		public static bool operator !=(ConstructorInfo left, ConstructorInfo right)
		{
			return !(left == right);
		}

		// Token: 0x060044EA RID: 17642 RVA: 0x000FD3BB File Offset: 0x000FB5BB
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x060044EB RID: 17643 RVA: 0x000FD3C4 File Offset: 0x000FB5C4
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x060044EC RID: 17644 RVA: 0x000FD3CC File Offset: 0x000FB5CC
		internal virtual Type GetReturnType()
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000A32 RID: 2610
		// (get) Token: 0x060044ED RID: 17645 RVA: 0x000FD3D3 File Offset: 0x000FB5D3
		[ComVisible(true)]
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Constructor;
			}
		}

		// Token: 0x060044EE RID: 17646
		public abstract object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture);

		// Token: 0x060044EF RID: 17647 RVA: 0x000FD3D6 File Offset: 0x000FB5D6
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public object Invoke(object[] parameters)
		{
			return this.Invoke(BindingFlags.Default, null, parameters, null);
		}

		// Token: 0x060044F0 RID: 17648 RVA: 0x000FD3E2 File Offset: 0x000FB5E2
		Type _ConstructorInfo.GetType()
		{
			return base.GetType();
		}

		// Token: 0x060044F1 RID: 17649 RVA: 0x000FD3EA File Offset: 0x000FB5EA
		object _ConstructorInfo.Invoke_2(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return this.Invoke(obj, invokeAttr, binder, parameters, culture);
		}

		// Token: 0x060044F2 RID: 17650 RVA: 0x000FD3F9 File Offset: 0x000FB5F9
		object _ConstructorInfo.Invoke_3(object obj, object[] parameters)
		{
			return base.Invoke(obj, parameters);
		}

		// Token: 0x060044F3 RID: 17651 RVA: 0x000FD403 File Offset: 0x000FB603
		object _ConstructorInfo.Invoke_4(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			return this.Invoke(invokeAttr, binder, parameters, culture);
		}

		// Token: 0x060044F4 RID: 17652 RVA: 0x000FD410 File Offset: 0x000FB610
		object _ConstructorInfo.Invoke_5(object[] parameters)
		{
			return this.Invoke(parameters);
		}

		// Token: 0x060044F5 RID: 17653 RVA: 0x000FD419 File Offset: 0x000FB619
		void _ConstructorInfo.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060044F6 RID: 17654 RVA: 0x000FD420 File Offset: 0x000FB620
		void _ConstructorInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060044F7 RID: 17655 RVA: 0x000FD427 File Offset: 0x000FB627
		void _ConstructorInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060044F8 RID: 17656 RVA: 0x000FD42E File Offset: 0x000FB62E
		void _ConstructorInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04001C4B RID: 7243
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static readonly string ConstructorName = ".ctor";

		// Token: 0x04001C4C RID: 7244
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public static readonly string TypeConstructorName = ".cctor";
	}
}
