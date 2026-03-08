using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace System.Reflection
{
	// Token: 0x02000606 RID: 1542
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_MethodBase))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[Serializable]
	public abstract class MethodBase : MemberInfo, _MethodBase
	{
		// Token: 0x060046F2 RID: 18162 RVA: 0x00103794 File Offset: 0x00101994
		[__DynamicallyInvokable]
		public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle)
		{
			if (handle.IsNullHandle())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));
			}
			MethodBase methodBase = RuntimeType.GetMethodBase(handle.GetMethodInfo());
			Type declaringType = methodBase.DeclaringType;
			if (declaringType != null && declaringType.IsGenericType)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_MethodDeclaringTypeGeneric"), methodBase, declaringType.GetGenericTypeDefinition()));
			}
			return methodBase;
		}

		// Token: 0x060046F3 RID: 18163 RVA: 0x00103801 File Offset: 0x00101A01
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
		{
			if (handle.IsNullHandle())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));
			}
			return RuntimeType.GetMethodBase(declaringType.GetRuntimeType(), handle.GetMethodInfo());
		}

		// Token: 0x060046F4 RID: 18164 RVA: 0x00103830 File Offset: 0x00101A30
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static MethodBase GetCurrentMethod()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeMethodInfo.InternalGetCurrentMethod(ref stackCrawlMark);
		}

		// Token: 0x060046F6 RID: 18166 RVA: 0x00103850 File Offset: 0x00101A50
		[__DynamicallyInvokable]
		public static bool operator ==(MethodBase left, MethodBase right)
		{
			if (left == right)
			{
				return true;
			}
			if (left == null || right == null)
			{
				return false;
			}
			MethodInfo left2;
			MethodInfo right2;
			if ((left2 = left as MethodInfo) != null && (right2 = right as MethodInfo) != null)
			{
				return left2 == right2;
			}
			ConstructorInfo left3;
			ConstructorInfo right3;
			return (left3 = left as ConstructorInfo) != null && (right3 = right as ConstructorInfo) != null && left3 == right3;
		}

		// Token: 0x060046F7 RID: 18167 RVA: 0x001038BC File Offset: 0x00101ABC
		[__DynamicallyInvokable]
		public static bool operator !=(MethodBase left, MethodBase right)
		{
			return !(left == right);
		}

		// Token: 0x060046F8 RID: 18168 RVA: 0x001038C8 File Offset: 0x00101AC8
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		// Token: 0x060046F9 RID: 18169 RVA: 0x001038D1 File Offset: 0x00101AD1
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x060046FA RID: 18170 RVA: 0x001038DC File Offset: 0x00101ADC
		[SecurityCritical]
		private IntPtr GetMethodDesc()
		{
			return this.MethodHandle.Value;
		}

		// Token: 0x17000ABA RID: 2746
		// (get) Token: 0x060046FB RID: 18171 RVA: 0x001038F7 File Offset: 0x00101AF7
		internal virtual bool IsDynamicallyInvokable
		{
			[__DynamicallyInvokable]
			get
			{
				return true;
			}
		}

		// Token: 0x060046FC RID: 18172 RVA: 0x001038FA File Offset: 0x00101AFA
		internal virtual ParameterInfo[] GetParametersNoCopy()
		{
			return this.GetParameters();
		}

		// Token: 0x060046FD RID: 18173
		[__DynamicallyInvokable]
		public abstract ParameterInfo[] GetParameters();

		// Token: 0x17000ABB RID: 2747
		// (get) Token: 0x060046FE RID: 18174 RVA: 0x00103902 File Offset: 0x00101B02
		[__DynamicallyInvokable]
		public virtual MethodImplAttributes MethodImplementationFlags
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetMethodImplementationFlags();
			}
		}

		// Token: 0x060046FF RID: 18175
		public abstract MethodImplAttributes GetMethodImplementationFlags();

		// Token: 0x17000ABC RID: 2748
		// (get) Token: 0x06004700 RID: 18176
		[__DynamicallyInvokable]
		public abstract RuntimeMethodHandle MethodHandle
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000ABD RID: 2749
		// (get) Token: 0x06004701 RID: 18177
		[__DynamicallyInvokable]
		public abstract MethodAttributes Attributes
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06004702 RID: 18178
		public abstract object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture);

		// Token: 0x17000ABE RID: 2750
		// (get) Token: 0x06004703 RID: 18179 RVA: 0x0010390A File Offset: 0x00101B0A
		[__DynamicallyInvokable]
		public virtual CallingConventions CallingConvention
		{
			[__DynamicallyInvokable]
			get
			{
				return CallingConventions.Standard;
			}
		}

		// Token: 0x06004704 RID: 18180 RVA: 0x0010390D File Offset: 0x00101B0D
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public virtual Type[] GetGenericArguments()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x17000ABF RID: 2751
		// (get) Token: 0x06004705 RID: 18181 RVA: 0x0010391E File Offset: 0x00101B1E
		[__DynamicallyInvokable]
		public virtual bool IsGenericMethodDefinition
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x17000AC0 RID: 2752
		// (get) Token: 0x06004706 RID: 18182 RVA: 0x00103921 File Offset: 0x00101B21
		[__DynamicallyInvokable]
		public virtual bool ContainsGenericParameters
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x17000AC1 RID: 2753
		// (get) Token: 0x06004707 RID: 18183 RVA: 0x00103924 File Offset: 0x00101B24
		[__DynamicallyInvokable]
		public virtual bool IsGenericMethod
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x17000AC2 RID: 2754
		// (get) Token: 0x06004708 RID: 18184 RVA: 0x00103927 File Offset: 0x00101B27
		public virtual bool IsSecurityCritical
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000AC3 RID: 2755
		// (get) Token: 0x06004709 RID: 18185 RVA: 0x0010392E File Offset: 0x00101B2E
		public virtual bool IsSecuritySafeCritical
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000AC4 RID: 2756
		// (get) Token: 0x0600470A RID: 18186 RVA: 0x00103935 File Offset: 0x00101B35
		public virtual bool IsSecurityTransparent
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x0600470B RID: 18187 RVA: 0x0010393C File Offset: 0x00101B3C
		[DebuggerStepThrough]
		[DebuggerHidden]
		[__DynamicallyInvokable]
		public object Invoke(object obj, object[] parameters)
		{
			return this.Invoke(obj, BindingFlags.Default, null, parameters, null);
		}

		// Token: 0x17000AC5 RID: 2757
		// (get) Token: 0x0600470C RID: 18188 RVA: 0x00103949 File Offset: 0x00101B49
		[__DynamicallyInvokable]
		public bool IsPublic
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
			}
		}

		// Token: 0x17000AC6 RID: 2758
		// (get) Token: 0x0600470D RID: 18189 RVA: 0x00103956 File Offset: 0x00101B56
		[__DynamicallyInvokable]
		public bool IsPrivate
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
			}
		}

		// Token: 0x17000AC7 RID: 2759
		// (get) Token: 0x0600470E RID: 18190 RVA: 0x00103963 File Offset: 0x00101B63
		[__DynamicallyInvokable]
		public bool IsFamily
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;
			}
		}

		// Token: 0x17000AC8 RID: 2760
		// (get) Token: 0x0600470F RID: 18191 RVA: 0x00103970 File Offset: 0x00101B70
		[__DynamicallyInvokable]
		public bool IsAssembly
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;
			}
		}

		// Token: 0x17000AC9 RID: 2761
		// (get) Token: 0x06004710 RID: 18192 RVA: 0x0010397D File Offset: 0x00101B7D
		[__DynamicallyInvokable]
		public bool IsFamilyAndAssembly
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;
			}
		}

		// Token: 0x17000ACA RID: 2762
		// (get) Token: 0x06004711 RID: 18193 RVA: 0x0010398A File Offset: 0x00101B8A
		[__DynamicallyInvokable]
		public bool IsFamilyOrAssembly
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;
			}
		}

		// Token: 0x17000ACB RID: 2763
		// (get) Token: 0x06004712 RID: 18194 RVA: 0x00103997 File Offset: 0x00101B97
		[__DynamicallyInvokable]
		public bool IsStatic
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.Static) > MethodAttributes.PrivateScope;
			}
		}

		// Token: 0x17000ACC RID: 2764
		// (get) Token: 0x06004713 RID: 18195 RVA: 0x001039A5 File Offset: 0x00101BA5
		[__DynamicallyInvokable]
		public bool IsFinal
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.Final) > MethodAttributes.PrivateScope;
			}
		}

		// Token: 0x17000ACD RID: 2765
		// (get) Token: 0x06004714 RID: 18196 RVA: 0x001039B3 File Offset: 0x00101BB3
		[__DynamicallyInvokable]
		public bool IsVirtual
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.Virtual) > MethodAttributes.PrivateScope;
			}
		}

		// Token: 0x17000ACE RID: 2766
		// (get) Token: 0x06004715 RID: 18197 RVA: 0x001039C1 File Offset: 0x00101BC1
		[__DynamicallyInvokable]
		public bool IsHideBySig
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.HideBySig) > MethodAttributes.PrivateScope;
			}
		}

		// Token: 0x17000ACF RID: 2767
		// (get) Token: 0x06004716 RID: 18198 RVA: 0x001039D2 File Offset: 0x00101BD2
		[__DynamicallyInvokable]
		public bool IsAbstract
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.Abstract) > MethodAttributes.PrivateScope;
			}
		}

		// Token: 0x17000AD0 RID: 2768
		// (get) Token: 0x06004717 RID: 18199 RVA: 0x001039E3 File Offset: 0x00101BE3
		[__DynamicallyInvokable]
		public bool IsSpecialName
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.Attributes & MethodAttributes.SpecialName) > MethodAttributes.PrivateScope;
			}
		}

		// Token: 0x17000AD1 RID: 2769
		// (get) Token: 0x06004718 RID: 18200 RVA: 0x001039F4 File Offset: 0x00101BF4
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public bool IsConstructor
		{
			[__DynamicallyInvokable]
			get
			{
				return this is ConstructorInfo && !this.IsStatic && (this.Attributes & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName;
			}
		}

		// Token: 0x06004719 RID: 18201 RVA: 0x00103A1B File Offset: 0x00101C1B
		[SecuritySafeCritical]
		[ReflectionPermission(SecurityAction.Demand, Flags = ReflectionPermissionFlag.MemberAccess)]
		public virtual MethodBody GetMethodBody()
		{
			throw new InvalidOperationException();
		}

		// Token: 0x0600471A RID: 18202 RVA: 0x00103A24 File Offset: 0x00101C24
		internal static string ConstructParameters(Type[] parameterTypes, CallingConventions callingConvention, bool serialization)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string value = "";
			foreach (Type type in parameterTypes)
			{
				stringBuilder.Append(value);
				string text = type.FormatTypeName(serialization);
				if (type.IsByRef && !serialization)
				{
					stringBuilder.Append(text.TrimEnd(new char[] { '&' }));
					stringBuilder.Append(" ByRef");
				}
				else
				{
					stringBuilder.Append(text);
				}
				value = ", ";
			}
			if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
			{
				stringBuilder.Append(value);
				stringBuilder.Append("...");
			}
			return stringBuilder.ToString();
		}

		// Token: 0x17000AD2 RID: 2770
		// (get) Token: 0x0600471B RID: 18203 RVA: 0x00103AC1 File Offset: 0x00101CC1
		internal string FullName
		{
			get
			{
				return string.Format("{0}.{1}", this.DeclaringType.FullName, this.FormatNameAndSig());
			}
		}

		// Token: 0x0600471C RID: 18204 RVA: 0x00103ADE File Offset: 0x00101CDE
		internal string FormatNameAndSig()
		{
			return this.FormatNameAndSig(false);
		}

		// Token: 0x0600471D RID: 18205 RVA: 0x00103AE8 File Offset: 0x00101CE8
		internal virtual string FormatNameAndSig(bool serialization)
		{
			StringBuilder stringBuilder = new StringBuilder(this.Name);
			stringBuilder.Append("(");
			stringBuilder.Append(MethodBase.ConstructParameters(this.GetParameterTypes(), this.CallingConvention, serialization));
			stringBuilder.Append(")");
			return stringBuilder.ToString();
		}

		// Token: 0x0600471E RID: 18206 RVA: 0x00103B38 File Offset: 0x00101D38
		internal virtual Type[] GetParameterTypes()
		{
			ParameterInfo[] parametersNoCopy = this.GetParametersNoCopy();
			Type[] array = new Type[parametersNoCopy.Length];
			for (int i = 0; i < parametersNoCopy.Length; i++)
			{
				array[i] = parametersNoCopy[i].ParameterType;
			}
			return array;
		}

		// Token: 0x0600471F RID: 18207 RVA: 0x00103B70 File Offset: 0x00101D70
		[SecuritySafeCritical]
		internal object[] CheckArguments(object[] parameters, Binder binder, BindingFlags invokeAttr, CultureInfo culture, Signature sig)
		{
			object[] array = new object[parameters.Length];
			ParameterInfo[] array2 = null;
			for (int i = 0; i < parameters.Length; i++)
			{
				object obj = parameters[i];
				RuntimeType runtimeType = sig.Arguments[i];
				if (obj == Type.Missing)
				{
					if (array2 == null)
					{
						array2 = this.GetParametersNoCopy();
					}
					if (array2[i].DefaultValue == DBNull.Value)
					{
						throw new ArgumentException(Environment.GetResourceString("Arg_VarMissNull"), "parameters");
					}
					obj = array2[i].DefaultValue;
				}
				array[i] = runtimeType.CheckValue(obj, binder, culture, invokeAttr);
			}
			return array;
		}

		// Token: 0x06004720 RID: 18208 RVA: 0x00103BF4 File Offset: 0x00101DF4
		Type _MethodBase.GetType()
		{
			return base.GetType();
		}

		// Token: 0x17000AD3 RID: 2771
		// (get) Token: 0x06004721 RID: 18209 RVA: 0x00103BFC File Offset: 0x00101DFC
		bool _MethodBase.IsPublic
		{
			get
			{
				return this.IsPublic;
			}
		}

		// Token: 0x17000AD4 RID: 2772
		// (get) Token: 0x06004722 RID: 18210 RVA: 0x00103C04 File Offset: 0x00101E04
		bool _MethodBase.IsPrivate
		{
			get
			{
				return this.IsPrivate;
			}
		}

		// Token: 0x17000AD5 RID: 2773
		// (get) Token: 0x06004723 RID: 18211 RVA: 0x00103C0C File Offset: 0x00101E0C
		bool _MethodBase.IsFamily
		{
			get
			{
				return this.IsFamily;
			}
		}

		// Token: 0x17000AD6 RID: 2774
		// (get) Token: 0x06004724 RID: 18212 RVA: 0x00103C14 File Offset: 0x00101E14
		bool _MethodBase.IsAssembly
		{
			get
			{
				return this.IsAssembly;
			}
		}

		// Token: 0x17000AD7 RID: 2775
		// (get) Token: 0x06004725 RID: 18213 RVA: 0x00103C1C File Offset: 0x00101E1C
		bool _MethodBase.IsFamilyAndAssembly
		{
			get
			{
				return this.IsFamilyAndAssembly;
			}
		}

		// Token: 0x17000AD8 RID: 2776
		// (get) Token: 0x06004726 RID: 18214 RVA: 0x00103C24 File Offset: 0x00101E24
		bool _MethodBase.IsFamilyOrAssembly
		{
			get
			{
				return this.IsFamilyOrAssembly;
			}
		}

		// Token: 0x17000AD9 RID: 2777
		// (get) Token: 0x06004727 RID: 18215 RVA: 0x00103C2C File Offset: 0x00101E2C
		bool _MethodBase.IsStatic
		{
			get
			{
				return this.IsStatic;
			}
		}

		// Token: 0x17000ADA RID: 2778
		// (get) Token: 0x06004728 RID: 18216 RVA: 0x00103C34 File Offset: 0x00101E34
		bool _MethodBase.IsFinal
		{
			get
			{
				return this.IsFinal;
			}
		}

		// Token: 0x17000ADB RID: 2779
		// (get) Token: 0x06004729 RID: 18217 RVA: 0x00103C3C File Offset: 0x00101E3C
		bool _MethodBase.IsVirtual
		{
			get
			{
				return this.IsVirtual;
			}
		}

		// Token: 0x17000ADC RID: 2780
		// (get) Token: 0x0600472A RID: 18218 RVA: 0x00103C44 File Offset: 0x00101E44
		bool _MethodBase.IsHideBySig
		{
			get
			{
				return this.IsHideBySig;
			}
		}

		// Token: 0x17000ADD RID: 2781
		// (get) Token: 0x0600472B RID: 18219 RVA: 0x00103C4C File Offset: 0x00101E4C
		bool _MethodBase.IsAbstract
		{
			get
			{
				return this.IsAbstract;
			}
		}

		// Token: 0x17000ADE RID: 2782
		// (get) Token: 0x0600472C RID: 18220 RVA: 0x00103C54 File Offset: 0x00101E54
		bool _MethodBase.IsSpecialName
		{
			get
			{
				return this.IsSpecialName;
			}
		}

		// Token: 0x17000ADF RID: 2783
		// (get) Token: 0x0600472D RID: 18221 RVA: 0x00103C5C File Offset: 0x00101E5C
		bool _MethodBase.IsConstructor
		{
			get
			{
				return this.IsConstructor;
			}
		}

		// Token: 0x0600472E RID: 18222 RVA: 0x00103C64 File Offset: 0x00101E64
		void _MethodBase.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600472F RID: 18223 RVA: 0x00103C6B File Offset: 0x00101E6B
		void _MethodBase.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004730 RID: 18224 RVA: 0x00103C72 File Offset: 0x00101E72
		void _MethodBase.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004731 RID: 18225 RVA: 0x00103C79 File Offset: 0x00101E79
		void _MethodBase.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}
	}
}
