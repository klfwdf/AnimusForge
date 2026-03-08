using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Reflection
{
	// Token: 0x0200061B RID: 1563
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_PropertyInfo))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[Serializable]
	public abstract class PropertyInfo : MemberInfo, _PropertyInfo
	{
		// Token: 0x06004855 RID: 18517 RVA: 0x00106E4A File Offset: 0x0010504A
		[__DynamicallyInvokable]
		public static bool operator ==(PropertyInfo left, PropertyInfo right)
		{
			return left == right || (left != null && right != null && !(left is RuntimePropertyInfo) && !(right is RuntimePropertyInfo) && left.Equals(right));
		}

		// Token: 0x06004856 RID: 18518 RVA: 0x00106E71 File Offset: 0x00105071
		[__DynamicallyInvokable]
		public static bool operator !=(PropertyInfo left, PropertyInfo right)
		{
			return !(left == right);
		}

		// Token: 0x06004857 RID: 18519 RVA: 0x00106E7D File Offset: 0x0010507D
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x06004858 RID: 18520 RVA: 0x00106E86 File Offset: 0x00105086
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x17000B3E RID: 2878
		// (get) Token: 0x06004859 RID: 18521 RVA: 0x00106E8E File Offset: 0x0010508E
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Property;
			}
		}

		// Token: 0x0600485A RID: 18522 RVA: 0x00106E92 File Offset: 0x00105092
		[__DynamicallyInvokable]
		public virtual object GetConstantValue()
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600485B RID: 18523 RVA: 0x00106E99 File Offset: 0x00105099
		public virtual object GetRawConstantValue()
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000B3F RID: 2879
		// (get) Token: 0x0600485C RID: 18524
		[__DynamicallyInvokable]
		public abstract Type PropertyType
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x0600485D RID: 18525
		public abstract void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture);

		// Token: 0x0600485E RID: 18526
		[__DynamicallyInvokable]
		public abstract MethodInfo[] GetAccessors(bool nonPublic);

		// Token: 0x0600485F RID: 18527
		[__DynamicallyInvokable]
		public abstract MethodInfo GetGetMethod(bool nonPublic);

		// Token: 0x06004860 RID: 18528
		[__DynamicallyInvokable]
		public abstract MethodInfo GetSetMethod(bool nonPublic);

		// Token: 0x06004861 RID: 18529
		[__DynamicallyInvokable]
		public abstract ParameterInfo[] GetIndexParameters();

		// Token: 0x17000B40 RID: 2880
		// (get) Token: 0x06004862 RID: 18530
		[__DynamicallyInvokable]
		public abstract PropertyAttributes Attributes
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000B41 RID: 2881
		// (get) Token: 0x06004863 RID: 18531
		[__DynamicallyInvokable]
		public abstract bool CanRead
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000B42 RID: 2882
		// (get) Token: 0x06004864 RID: 18532
		[__DynamicallyInvokable]
		public abstract bool CanWrite
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06004865 RID: 18533 RVA: 0x00106EA0 File Offset: 0x001050A0
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public object GetValue(object obj)
		{
			return this.GetValue(obj, null);
		}

		// Token: 0x06004866 RID: 18534 RVA: 0x00106EAA File Offset: 0x001050AA
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public virtual object GetValue(object obj, object[] index)
		{
			return this.GetValue(obj, BindingFlags.Default, null, index, null);
		}

		// Token: 0x06004867 RID: 18535
		public abstract object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture);

		// Token: 0x06004868 RID: 18536 RVA: 0x00106EB7 File Offset: 0x001050B7
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public void SetValue(object obj, object value)
		{
			this.SetValue(obj, value, null);
		}

		// Token: 0x06004869 RID: 18537 RVA: 0x00106EC2 File Offset: 0x001050C2
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public virtual void SetValue(object obj, object value, object[] index)
		{
			this.SetValue(obj, value, BindingFlags.Default, null, index, null);
		}

		// Token: 0x0600486A RID: 18538 RVA: 0x00106ED0 File Offset: 0x001050D0
		public virtual Type[] GetRequiredCustomModifiers()
		{
			return EmptyArray<Type>.Value;
		}

		// Token: 0x0600486B RID: 18539 RVA: 0x00106ED7 File Offset: 0x001050D7
		public virtual Type[] GetOptionalCustomModifiers()
		{
			return EmptyArray<Type>.Value;
		}

		// Token: 0x0600486C RID: 18540 RVA: 0x00106EDE File Offset: 0x001050DE
		[__DynamicallyInvokable]
		public MethodInfo[] GetAccessors()
		{
			return this.GetAccessors(false);
		}

		// Token: 0x17000B43 RID: 2883
		// (get) Token: 0x0600486D RID: 18541 RVA: 0x00106EE7 File Offset: 0x001050E7
		[__DynamicallyInvokable]
		public virtual MethodInfo GetMethod
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetGetMethod(true);
			}
		}

		// Token: 0x17000B44 RID: 2884
		// (get) Token: 0x0600486E RID: 18542 RVA: 0x00106EF0 File Offset: 0x001050F0
		[__DynamicallyInvokable]
		public virtual MethodInfo SetMethod
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetSetMethod(true);
			}
		}

		// Token: 0x0600486F RID: 18543 RVA: 0x00106EF9 File Offset: 0x001050F9
		[__DynamicallyInvokable]
		public MethodInfo GetGetMethod()
		{
			return this.GetGetMethod(false);
		}

		// Token: 0x06004870 RID: 18544 RVA: 0x00106F02 File Offset: 0x00105102
		[__DynamicallyInvokable]
		public MethodInfo GetSetMethod()
		{
			return this.GetSetMethod(false);
		}

		// Token: 0x17000B45 RID: 2885
		// (get) Token: 0x06004871 RID: 18545 RVA: 0x00106F0B File Offset: 0x0010510B
		[__DynamicallyInvokable]
		public bool IsSpecialName
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & PropertyAttributes.SpecialName) > PropertyAttributes.None;
			}
		}

		// Token: 0x06004872 RID: 18546 RVA: 0x00106F1C File Offset: 0x0010511C
		Type _PropertyInfo.GetType()
		{
			return base.GetType();
		}

		// Token: 0x06004873 RID: 18547 RVA: 0x00106F24 File Offset: 0x00105124
		void _PropertyInfo.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004874 RID: 18548 RVA: 0x00106F2B File Offset: 0x0010512B
		void _PropertyInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004875 RID: 18549 RVA: 0x00106F32 File Offset: 0x00105132
		void _PropertyInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004876 RID: 18550 RVA: 0x00106F39 File Offset: 0x00105139
		void _PropertyInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}
	}
}
