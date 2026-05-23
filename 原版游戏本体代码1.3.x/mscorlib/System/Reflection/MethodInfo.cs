using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Reflection
{
	// Token: 0x02000608 RID: 1544
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_MethodInfo))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[Serializable]
	public abstract class MethodInfo : MethodBase, _MethodInfo
	{
		// Token: 0x06004733 RID: 18227 RVA: 0x00103C88 File Offset: 0x00101E88
		[__DynamicallyInvokable]
		public static bool operator ==(MethodInfo left, MethodInfo right)
		{
			return left == right || (left != null && right != null && !(left is RuntimeMethodInfo) && !(right is RuntimeMethodInfo) && left.Equals(right));
		}

		// Token: 0x06004734 RID: 18228 RVA: 0x00103CAF File Offset: 0x00101EAF
		[__DynamicallyInvokable]
		public static bool operator !=(MethodInfo left, MethodInfo right)
		{
			return !(left == right);
		}

		// Token: 0x06004735 RID: 18229 RVA: 0x00103CBB File Offset: 0x00101EBB
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x06004736 RID: 18230 RVA: 0x00103CC4 File Offset: 0x00101EC4
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x17000AE0 RID: 2784
		// (get) Token: 0x06004737 RID: 18231 RVA: 0x00103CCC File Offset: 0x00101ECC
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Method;
			}
		}

		// Token: 0x17000AE1 RID: 2785
		// (get) Token: 0x06004738 RID: 18232 RVA: 0x00103CCF File Offset: 0x00101ECF
		[__DynamicallyInvokable]
		public virtual Type ReturnType
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000AE2 RID: 2786
		// (get) Token: 0x06004739 RID: 18233 RVA: 0x00103CD6 File Offset: 0x00101ED6
		[__DynamicallyInvokable]
		public virtual ParameterInfo ReturnParameter
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000AE3 RID: 2787
		// (get) Token: 0x0600473A RID: 18234
		public abstract ICustomAttributeProvider ReturnTypeCustomAttributes { get; }

		// Token: 0x0600473B RID: 18235
		[__DynamicallyInvokable]
		public abstract MethodInfo GetBaseDefinition();

		// Token: 0x0600473C RID: 18236 RVA: 0x00103CDD File Offset: 0x00101EDD
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public override Type[] GetGenericArguments()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x0600473D RID: 18237 RVA: 0x00103CEE File Offset: 0x00101EEE
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public virtual MethodInfo GetGenericMethodDefinition()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x0600473E RID: 18238 RVA: 0x00103CFF File Offset: 0x00101EFF
		[__DynamicallyInvokable]
		public virtual MethodInfo MakeGenericMethod(params Type[] typeArguments)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x0600473F RID: 18239 RVA: 0x00103D10 File Offset: 0x00101F10
		[__DynamicallyInvokable]
		public virtual Delegate CreateDelegate(Type delegateType)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x06004740 RID: 18240 RVA: 0x00103D21 File Offset: 0x00101F21
		[__DynamicallyInvokable]
		public virtual Delegate CreateDelegate(Type delegateType, object target)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x06004741 RID: 18241 RVA: 0x00103D32 File Offset: 0x00101F32
		Type _MethodInfo.GetType()
		{
			return base.GetType();
		}

		// Token: 0x06004742 RID: 18242 RVA: 0x00103D3A File Offset: 0x00101F3A
		void _MethodInfo.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004743 RID: 18243 RVA: 0x00103D41 File Offset: 0x00101F41
		void _MethodInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004744 RID: 18244 RVA: 0x00103D48 File Offset: 0x00101F48
		void _MethodInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004745 RID: 18245 RVA: 0x00103D4F File Offset: 0x00101F4F
		void _MethodInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}
	}
}
