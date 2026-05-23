using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Reflection
{
	// Token: 0x020005E5 RID: 1509
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_FieldInfo))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[Serializable]
	public abstract class FieldInfo : MemberInfo, _FieldInfo
	{
		// Token: 0x060045E8 RID: 17896 RVA: 0x00101818 File Offset: 0x000FFA18
		[__DynamicallyInvokable]
		public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle)
		{
			if (handle.IsNullHandle())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));
			}
			FieldInfo fieldInfo = RuntimeType.GetFieldInfo(handle.GetRuntimeFieldInfo());
			Type declaringType = fieldInfo.DeclaringType;
			if (declaringType != null && declaringType.IsGenericType)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_FieldDeclaringTypeGeneric"), fieldInfo.Name, declaringType.GetGenericTypeDefinition()));
			}
			return fieldInfo;
		}

		// Token: 0x060045E9 RID: 17897 RVA: 0x0010188A File Offset: 0x000FFA8A
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle, RuntimeTypeHandle declaringType)
		{
			if (handle.IsNullHandle())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));
			}
			return RuntimeType.GetFieldInfo(declaringType.GetRuntimeType(), handle.GetRuntimeFieldInfo());
		}

		// Token: 0x060045EB RID: 17899 RVA: 0x001018C0 File Offset: 0x000FFAC0
		[__DynamicallyInvokable]
		public static bool operator ==(FieldInfo left, FieldInfo right)
		{
			return left == right || (left != null && right != null && !(left is RuntimeFieldInfo) && !(right is RuntimeFieldInfo) && left.Equals(right));
		}

		// Token: 0x060045EC RID: 17900 RVA: 0x001018E7 File Offset: 0x000FFAE7
		[__DynamicallyInvokable]
		public static bool operator !=(FieldInfo left, FieldInfo right)
		{
			return !(left == right);
		}

		// Token: 0x060045ED RID: 17901 RVA: 0x001018F3 File Offset: 0x000FFAF3
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x060045EE RID: 17902 RVA: 0x001018FC File Offset: 0x000FFAFC
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x17000A6F RID: 2671
		// (get) Token: 0x060045EF RID: 17903 RVA: 0x00101904 File Offset: 0x000FFB04
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Field;
			}
		}

		// Token: 0x060045F0 RID: 17904 RVA: 0x00101907 File Offset: 0x000FFB07
		public virtual Type[] GetRequiredCustomModifiers()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060045F1 RID: 17905 RVA: 0x0010190E File Offset: 0x000FFB0E
		public virtual Type[] GetOptionalCustomModifiers()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060045F2 RID: 17906 RVA: 0x00101915 File Offset: 0x000FFB15
		[CLSCompliant(false)]
		public virtual void SetValueDirect(TypedReference obj, object value)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
		}

		// Token: 0x060045F3 RID: 17907 RVA: 0x00101926 File Offset: 0x000FFB26
		[CLSCompliant(false)]
		public virtual object GetValueDirect(TypedReference obj)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
		}

		// Token: 0x17000A70 RID: 2672
		// (get) Token: 0x060045F4 RID: 17908
		[__DynamicallyInvokable]
		public abstract RuntimeFieldHandle FieldHandle
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000A71 RID: 2673
		// (get) Token: 0x060045F5 RID: 17909
		[__DynamicallyInvokable]
		public abstract Type FieldType
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x060045F6 RID: 17910
		[__DynamicallyInvokable]
		public abstract object GetValue(object obj);

		// Token: 0x060045F7 RID: 17911 RVA: 0x00101937 File Offset: 0x000FFB37
		public virtual object GetRawConstantValue()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
		}

		// Token: 0x060045F8 RID: 17912
		public abstract void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture);

		// Token: 0x17000A72 RID: 2674
		// (get) Token: 0x060045F9 RID: 17913
		[__DynamicallyInvokable]
		public abstract FieldAttributes Attributes
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x060045FA RID: 17914 RVA: 0x00101948 File Offset: 0x000FFB48
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public void SetValue(object obj, object value)
		{
			this.SetValue(obj, value, BindingFlags.Default, Type.DefaultBinder, null);
		}

		// Token: 0x17000A73 RID: 2675
		// (get) Token: 0x060045FB RID: 17915 RVA: 0x00101959 File Offset: 0x000FFB59
		[__DynamicallyInvokable]
		public bool IsPublic
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;
			}
		}

		// Token: 0x17000A74 RID: 2676
		// (get) Token: 0x060045FC RID: 17916 RVA: 0x00101966 File Offset: 0x000FFB66
		[__DynamicallyInvokable]
		public bool IsPrivate
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;
			}
		}

		// Token: 0x17000A75 RID: 2677
		// (get) Token: 0x060045FD RID: 17917 RVA: 0x00101973 File Offset: 0x000FFB73
		[__DynamicallyInvokable]
		public bool IsFamily
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;
			}
		}

		// Token: 0x17000A76 RID: 2678
		// (get) Token: 0x060045FE RID: 17918 RVA: 0x00101980 File Offset: 0x000FFB80
		[__DynamicallyInvokable]
		public bool IsAssembly
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;
			}
		}

		// Token: 0x17000A77 RID: 2679
		// (get) Token: 0x060045FF RID: 17919 RVA: 0x0010198D File Offset: 0x000FFB8D
		[__DynamicallyInvokable]
		public bool IsFamilyAndAssembly
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem;
			}
		}

		// Token: 0x17000A78 RID: 2680
		// (get) Token: 0x06004600 RID: 17920 RVA: 0x0010199A File Offset: 0x000FFB9A
		[__DynamicallyInvokable]
		public bool IsFamilyOrAssembly
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem;
			}
		}

		// Token: 0x17000A79 RID: 2681
		// (get) Token: 0x06004601 RID: 17921 RVA: 0x001019A7 File Offset: 0x000FFBA7
		[__DynamicallyInvokable]
		public bool IsStatic
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.Static) > FieldAttributes.PrivateScope;
			}
		}

		// Token: 0x17000A7A RID: 2682
		// (get) Token: 0x06004602 RID: 17922 RVA: 0x001019B5 File Offset: 0x000FFBB5
		[__DynamicallyInvokable]
		public bool IsInitOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.InitOnly) > FieldAttributes.PrivateScope;
			}
		}

		// Token: 0x17000A7B RID: 2683
		// (get) Token: 0x06004603 RID: 17923 RVA: 0x001019C3 File Offset: 0x000FFBC3
		[__DynamicallyInvokable]
		public bool IsLiteral
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.Literal) > FieldAttributes.PrivateScope;
			}
		}

		// Token: 0x17000A7C RID: 2684
		// (get) Token: 0x06004604 RID: 17924 RVA: 0x001019D1 File Offset: 0x000FFBD1
		public bool IsNotSerialized
		{
			get
			{
				return (this.Attributes & FieldAttributes.NotSerialized) > FieldAttributes.PrivateScope;
			}
		}

		// Token: 0x17000A7D RID: 2685
		// (get) Token: 0x06004605 RID: 17925 RVA: 0x001019E2 File Offset: 0x000FFBE2
		[__DynamicallyInvokable]
		public bool IsSpecialName
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & FieldAttributes.SpecialName) > FieldAttributes.PrivateScope;
			}
		}

		// Token: 0x17000A7E RID: 2686
		// (get) Token: 0x06004606 RID: 17926 RVA: 0x001019F3 File Offset: 0x000FFBF3
		public bool IsPinvokeImpl
		{
			get
			{
				return (this.Attributes & FieldAttributes.PinvokeImpl) > FieldAttributes.PrivateScope;
			}
		}

		// Token: 0x17000A7F RID: 2687
		// (get) Token: 0x06004607 RID: 17927 RVA: 0x00101A04 File Offset: 0x000FFC04
		public virtual bool IsSecurityCritical
		{
			get
			{
				return this.FieldHandle.IsSecurityCritical();
			}
		}

		// Token: 0x17000A80 RID: 2688
		// (get) Token: 0x06004608 RID: 17928 RVA: 0x00101A20 File Offset: 0x000FFC20
		public virtual bool IsSecuritySafeCritical
		{
			get
			{
				return this.FieldHandle.IsSecuritySafeCritical();
			}
		}

		// Token: 0x17000A81 RID: 2689
		// (get) Token: 0x06004609 RID: 17929 RVA: 0x00101A3C File Offset: 0x000FFC3C
		public virtual bool IsSecurityTransparent
		{
			get
			{
				return this.FieldHandle.IsSecurityTransparent();
			}
		}

		// Token: 0x0600460A RID: 17930 RVA: 0x00101A57 File Offset: 0x000FFC57
		Type _FieldInfo.GetType()
		{
			return base.GetType();
		}

		// Token: 0x0600460B RID: 17931 RVA: 0x00101A5F File Offset: 0x000FFC5F
		void _FieldInfo.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600460C RID: 17932 RVA: 0x00101A66 File Offset: 0x000FFC66
		void _FieldInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600460D RID: 17933 RVA: 0x00101A6D File Offset: 0x000FFC6D
		void _FieldInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600460E RID: 17934 RVA: 0x00101A74 File Offset: 0x000FFC74
		void _FieldInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}
	}
}
