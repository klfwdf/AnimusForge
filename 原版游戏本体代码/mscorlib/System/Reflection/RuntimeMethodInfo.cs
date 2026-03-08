using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Metadata;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace System.Reflection
{
	// Token: 0x02000609 RID: 1545
	[Serializable]
	internal sealed class RuntimeMethodInfo : MethodInfo, ISerializable, IRuntimeMethodInfo
	{
		// Token: 0x06004746 RID: 18246 RVA: 0x00103D58 File Offset: 0x00101F58
		private bool IsNonW8PFrameworkAPI()
		{
			if (this.m_declaringType.IsArray && base.IsPublic && !base.IsStatic)
			{
				return false;
			}
			RuntimeAssembly runtimeAssembly = this.GetRuntimeAssembly();
			if (runtimeAssembly.IsFrameworkAssembly())
			{
				int invocableAttributeCtorToken = runtimeAssembly.InvocableAttributeCtorToken;
				if (System.Reflection.MetadataToken.IsNullToken(invocableAttributeCtorToken) || !CustomAttribute.IsAttributeDefined(this.GetRuntimeModule(), this.MetadataToken, invocableAttributeCtorToken))
				{
					return true;
				}
			}
			if (this.GetRuntimeType().IsNonW8PFrameworkAPI())
			{
				return true;
			}
			if (this.IsGenericMethod && !this.IsGenericMethodDefinition)
			{
				foreach (Type type in this.GetGenericArguments())
				{
					if (((RuntimeType)type).IsNonW8PFrameworkAPI())
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x17000AE4 RID: 2788
		// (get) Token: 0x06004747 RID: 18247 RVA: 0x00103E02 File Offset: 0x00102002
		internal override bool IsDynamicallyInvokable
		{
			get
			{
				return !AppDomain.ProfileAPICheck || !this.IsNonW8PFrameworkAPI();
			}
		}

		// Token: 0x17000AE5 RID: 2789
		// (get) Token: 0x06004748 RID: 18248 RVA: 0x00103E18 File Offset: 0x00102018
		internal INVOCATION_FLAGS InvocationFlags
		{
			[SecuritySafeCritical]
			get
			{
				if ((this.m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
				{
					Type declaringType = this.DeclaringType;
					INVOCATION_FLAGS invocation_FLAGS;
					if (this.ContainsGenericParameters || this.ReturnType.IsByRef || (declaringType != null && declaringType.ContainsGenericParameters) || (this.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs || (this.Attributes & MethodAttributes.RequireSecObject) == MethodAttributes.RequireSecObject)
					{
						invocation_FLAGS = INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
					}
					else
					{
						invocation_FLAGS = RuntimeMethodHandle.GetSecurityFlags(this);
						if ((invocation_FLAGS & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) == INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
						{
							if ((this.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public || (declaringType != null && declaringType.NeedsReflectionSecurityCheck))
							{
								invocation_FLAGS |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
							}
							else if (this.IsGenericMethod)
							{
								Type[] genericArguments = this.GetGenericArguments();
								for (int i = 0; i < genericArguments.Length; i++)
								{
									if (genericArguments[i].NeedsReflectionSecurityCheck)
									{
										invocation_FLAGS |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
										break;
									}
								}
							}
						}
					}
					if (AppDomain.ProfileAPICheck && this.IsNonW8PFrameworkAPI())
					{
						invocation_FLAGS |= INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API;
					}
					this.m_invocationFlags = invocation_FLAGS | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
				}
				return this.m_invocationFlags;
			}
		}

		// Token: 0x06004749 RID: 18249 RVA: 0x00103F01 File Offset: 0x00102101
		[SecurityCritical]
		internal RuntimeMethodInfo(RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeType.RuntimeTypeCache reflectedTypeCache, MethodAttributes methodAttributes, BindingFlags bindingFlags, object keepalive)
		{
			this.m_bindingFlags = bindingFlags;
			this.m_declaringType = declaringType;
			this.m_keepalive = keepalive;
			this.m_handle = handle.Value;
			this.m_reflectedTypeCache = reflectedTypeCache;
			this.m_methodAttributes = methodAttributes;
		}

		// Token: 0x17000AE6 RID: 2790
		// (get) Token: 0x0600474A RID: 18250 RVA: 0x00103F3C File Offset: 0x0010213C
		internal RemotingMethodCachedData RemotingCache
		{
			get
			{
				RemotingMethodCachedData remotingMethodCachedData = this.m_cachedData;
				if (remotingMethodCachedData == null)
				{
					remotingMethodCachedData = new RemotingMethodCachedData(this);
					RemotingMethodCachedData remotingMethodCachedData2 = Interlocked.CompareExchange<RemotingMethodCachedData>(ref this.m_cachedData, remotingMethodCachedData, null);
					if (remotingMethodCachedData2 != null)
					{
						remotingMethodCachedData = remotingMethodCachedData2;
					}
				}
				return remotingMethodCachedData;
			}
		}

		// Token: 0x17000AE7 RID: 2791
		// (get) Token: 0x0600474B RID: 18251 RVA: 0x00103F6E File Offset: 0x0010216E
		RuntimeMethodHandleInternal IRuntimeMethodInfo.Value
		{
			[SecuritySafeCritical]
			get
			{
				return new RuntimeMethodHandleInternal(this.m_handle);
			}
		}

		// Token: 0x17000AE8 RID: 2792
		// (get) Token: 0x0600474C RID: 18252 RVA: 0x00103F7B File Offset: 0x0010217B
		private RuntimeType ReflectedTypeInternal
		{
			get
			{
				return this.m_reflectedTypeCache.GetRuntimeType();
			}
		}

		// Token: 0x0600474D RID: 18253 RVA: 0x00103F88 File Offset: 0x00102188
		[SecurityCritical]
		private ParameterInfo[] FetchNonReturnParameters()
		{
			if (this.m_parameters == null)
			{
				this.m_parameters = RuntimeParameterInfo.GetParameters(this, this, this.Signature);
			}
			return this.m_parameters;
		}

		// Token: 0x0600474E RID: 18254 RVA: 0x00103FAB File Offset: 0x001021AB
		[SecurityCritical]
		private ParameterInfo FetchReturnParameter()
		{
			if (this.m_returnParameter == null)
			{
				this.m_returnParameter = RuntimeParameterInfo.GetReturnParameter(this, this, this.Signature);
			}
			return this.m_returnParameter;
		}

		// Token: 0x0600474F RID: 18255 RVA: 0x00103FD0 File Offset: 0x001021D0
		internal override string FormatNameAndSig(bool serialization)
		{
			StringBuilder stringBuilder = new StringBuilder(this.Name);
			TypeNameFormatFlags format = (serialization ? TypeNameFormatFlags.FormatSerialization : TypeNameFormatFlags.FormatBasic);
			if (this.IsGenericMethod)
			{
				stringBuilder.Append(RuntimeMethodHandle.ConstructInstantiation(this, format));
			}
			stringBuilder.Append("(");
			stringBuilder.Append(MethodBase.ConstructParameters(this.GetParameterTypes(), this.CallingConvention, serialization));
			stringBuilder.Append(")");
			return stringBuilder.ToString();
		}

		// Token: 0x06004750 RID: 18256 RVA: 0x00104044 File Offset: 0x00102244
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal override bool CacheEquals(object o)
		{
			RuntimeMethodInfo runtimeMethodInfo = o as RuntimeMethodInfo;
			return runtimeMethodInfo != null && runtimeMethodInfo.m_handle == this.m_handle;
		}

		// Token: 0x17000AE9 RID: 2793
		// (get) Token: 0x06004751 RID: 18257 RVA: 0x0010406E File Offset: 0x0010226E
		internal Signature Signature
		{
			get
			{
				if (this.m_signature == null)
				{
					this.m_signature = new Signature(this, this.m_declaringType);
				}
				return this.m_signature;
			}
		}

		// Token: 0x17000AEA RID: 2794
		// (get) Token: 0x06004752 RID: 18258 RVA: 0x00104090 File Offset: 0x00102290
		internal BindingFlags BindingFlags
		{
			get
			{
				return this.m_bindingFlags;
			}
		}

		// Token: 0x06004753 RID: 18259 RVA: 0x00104098 File Offset: 0x00102298
		internal RuntimeMethodHandle GetMethodHandle()
		{
			return new RuntimeMethodHandle(this);
		}

		// Token: 0x06004754 RID: 18260 RVA: 0x001040A0 File Offset: 0x001022A0
		[SecuritySafeCritical]
		internal RuntimeMethodInfo GetParentDefinition()
		{
			if (!base.IsVirtual || this.m_declaringType.IsInterface)
			{
				return null;
			}
			RuntimeType runtimeType = (RuntimeType)this.m_declaringType.BaseType;
			if (runtimeType == null)
			{
				return null;
			}
			int slot = RuntimeMethodHandle.GetSlot(this);
			if (RuntimeTypeHandle.GetNumVirtuals(runtimeType) <= slot)
			{
				return null;
			}
			return (RuntimeMethodInfo)RuntimeType.GetMethodBase(runtimeType, RuntimeTypeHandle.GetMethodAt(runtimeType, slot));
		}

		// Token: 0x06004755 RID: 18261 RVA: 0x00104104 File Offset: 0x00102304
		internal RuntimeType GetDeclaringTypeInternal()
		{
			return this.m_declaringType;
		}

		// Token: 0x06004756 RID: 18262 RVA: 0x0010410C File Offset: 0x0010230C
		public override string ToString()
		{
			if (this.m_toString == null)
			{
				this.m_toString = this.ReturnType.FormatTypeName() + " " + base.FormatNameAndSig();
			}
			return this.m_toString;
		}

		// Token: 0x06004757 RID: 18263 RVA: 0x0010413D File Offset: 0x0010233D
		public override int GetHashCode()
		{
			if (this.IsGenericMethod)
			{
				return ValueType.GetHashCodeOfPtr(this.m_handle);
			}
			return base.GetHashCode();
		}

		// Token: 0x06004758 RID: 18264 RVA: 0x0010415C File Offset: 0x0010235C
		[SecuritySafeCritical]
		public override bool Equals(object obj)
		{
			if (!this.IsGenericMethod)
			{
				return obj == this;
			}
			RuntimeMethodInfo runtimeMethodInfo = obj as RuntimeMethodInfo;
			if (runtimeMethodInfo == null || !runtimeMethodInfo.IsGenericMethod)
			{
				return false;
			}
			IRuntimeMethodInfo runtimeMethodInfo2 = RuntimeMethodHandle.StripMethodInstantiation(this);
			IRuntimeMethodInfo runtimeMethodInfo3 = RuntimeMethodHandle.StripMethodInstantiation(runtimeMethodInfo);
			if (runtimeMethodInfo2.Value.Value != runtimeMethodInfo3.Value.Value)
			{
				return false;
			}
			Type[] genericArguments = this.GetGenericArguments();
			Type[] genericArguments2 = runtimeMethodInfo.GetGenericArguments();
			if (genericArguments.Length != genericArguments2.Length)
			{
				return false;
			}
			for (int i = 0; i < genericArguments.Length; i++)
			{
				if (genericArguments[i] != genericArguments2[i])
				{
					return false;
				}
			}
			return !(this.DeclaringType != runtimeMethodInfo.DeclaringType) && !(this.ReflectedType != runtimeMethodInfo.ReflectedType);
		}

		// Token: 0x06004759 RID: 18265 RVA: 0x0010422E File Offset: 0x0010242E
		[SecuritySafeCritical]
		public override object[] GetCustomAttributes(bool inherit)
		{
			return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType, inherit);
		}

		// Token: 0x0600475A RID: 18266 RVA: 0x00104248 File Offset: 0x00102448
		[SecuritySafeCritical]
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
			}
			return CustomAttribute.GetCustomAttributes(this, runtimeType, inherit);
		}

		// Token: 0x0600475B RID: 18267 RVA: 0x0010429C File Offset: 0x0010249C
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
			}
			return CustomAttribute.IsDefined(this, runtimeType, inherit);
		}

		// Token: 0x0600475C RID: 18268 RVA: 0x001042EF File Offset: 0x001024EF
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return CustomAttributeData.GetCustomAttributesInternal(this);
		}

		// Token: 0x17000AEB RID: 2795
		// (get) Token: 0x0600475D RID: 18269 RVA: 0x001042F7 File Offset: 0x001024F7
		public override string Name
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_name == null)
				{
					this.m_name = RuntimeMethodHandle.GetName(this);
				}
				return this.m_name;
			}
		}

		// Token: 0x17000AEC RID: 2796
		// (get) Token: 0x0600475E RID: 18270 RVA: 0x00104313 File Offset: 0x00102513
		public override Type DeclaringType
		{
			get
			{
				if (this.m_reflectedTypeCache.IsGlobal)
				{
					return null;
				}
				return this.m_declaringType;
			}
		}

		// Token: 0x17000AED RID: 2797
		// (get) Token: 0x0600475F RID: 18271 RVA: 0x0010432A File Offset: 0x0010252A
		public override Type ReflectedType
		{
			get
			{
				if (this.m_reflectedTypeCache.IsGlobal)
				{
					return null;
				}
				return this.m_reflectedTypeCache.GetRuntimeType();
			}
		}

		// Token: 0x17000AEE RID: 2798
		// (get) Token: 0x06004760 RID: 18272 RVA: 0x00104346 File Offset: 0x00102546
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.Method;
			}
		}

		// Token: 0x17000AEF RID: 2799
		// (get) Token: 0x06004761 RID: 18273 RVA: 0x00104349 File Offset: 0x00102549
		public override int MetadataToken
		{
			[SecuritySafeCritical]
			get
			{
				return RuntimeMethodHandle.GetMethodDef(this);
			}
		}

		// Token: 0x17000AF0 RID: 2800
		// (get) Token: 0x06004762 RID: 18274 RVA: 0x00104351 File Offset: 0x00102551
		public override Module Module
		{
			get
			{
				return this.GetRuntimeModule();
			}
		}

		// Token: 0x06004763 RID: 18275 RVA: 0x00104359 File Offset: 0x00102559
		internal RuntimeType GetRuntimeType()
		{
			return this.m_declaringType;
		}

		// Token: 0x06004764 RID: 18276 RVA: 0x00104361 File Offset: 0x00102561
		internal RuntimeModule GetRuntimeModule()
		{
			return this.m_declaringType.GetRuntimeModule();
		}

		// Token: 0x06004765 RID: 18277 RVA: 0x0010436E File Offset: 0x0010256E
		internal RuntimeAssembly GetRuntimeAssembly()
		{
			return this.GetRuntimeModule().GetRuntimeAssembly();
		}

		// Token: 0x17000AF1 RID: 2801
		// (get) Token: 0x06004766 RID: 18278 RVA: 0x0010437B File Offset: 0x0010257B
		public override bool IsSecurityCritical
		{
			get
			{
				return RuntimeMethodHandle.IsSecurityCritical(this);
			}
		}

		// Token: 0x17000AF2 RID: 2802
		// (get) Token: 0x06004767 RID: 18279 RVA: 0x00104383 File Offset: 0x00102583
		public override bool IsSecuritySafeCritical
		{
			get
			{
				return RuntimeMethodHandle.IsSecuritySafeCritical(this);
			}
		}

		// Token: 0x17000AF3 RID: 2803
		// (get) Token: 0x06004768 RID: 18280 RVA: 0x0010438B File Offset: 0x0010258B
		public override bool IsSecurityTransparent
		{
			get
			{
				return RuntimeMethodHandle.IsSecurityTransparent(this);
			}
		}

		// Token: 0x06004769 RID: 18281 RVA: 0x00104393 File Offset: 0x00102593
		[SecuritySafeCritical]
		internal override ParameterInfo[] GetParametersNoCopy()
		{
			this.FetchNonReturnParameters();
			return this.m_parameters;
		}

		// Token: 0x0600476A RID: 18282 RVA: 0x001043A4 File Offset: 0x001025A4
		[SecuritySafeCritical]
		public override ParameterInfo[] GetParameters()
		{
			this.FetchNonReturnParameters();
			if (this.m_parameters.Length == 0)
			{
				return this.m_parameters;
			}
			ParameterInfo[] array = new ParameterInfo[this.m_parameters.Length];
			Array.Copy(this.m_parameters, array, this.m_parameters.Length);
			return array;
		}

		// Token: 0x0600476B RID: 18283 RVA: 0x001043EB File Offset: 0x001025EB
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return RuntimeMethodHandle.GetImplAttributes(this);
		}

		// Token: 0x17000AF4 RID: 2804
		// (get) Token: 0x0600476C RID: 18284 RVA: 0x001043F3 File Offset: 0x001025F3
		internal bool IsOverloaded
		{
			get
			{
				return this.m_reflectedTypeCache.GetMethodList(RuntimeType.MemberListType.CaseSensitive, this.Name).Length > 1;
			}
		}

		// Token: 0x17000AF5 RID: 2805
		// (get) Token: 0x0600476D RID: 18285 RVA: 0x0010440C File Offset: 0x0010260C
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				Type declaringType = this.DeclaringType;
				if ((declaringType == null && this.Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
				}
				return new RuntimeMethodHandle(this);
			}
		}

		// Token: 0x17000AF6 RID: 2806
		// (get) Token: 0x0600476E RID: 18286 RVA: 0x00104459 File Offset: 0x00102659
		public override MethodAttributes Attributes
		{
			get
			{
				return this.m_methodAttributes;
			}
		}

		// Token: 0x17000AF7 RID: 2807
		// (get) Token: 0x0600476F RID: 18287 RVA: 0x00104461 File Offset: 0x00102661
		public override CallingConventions CallingConvention
		{
			get
			{
				return this.Signature.CallingConvention;
			}
		}

		// Token: 0x06004770 RID: 18288 RVA: 0x00104470 File Offset: 0x00102670
		[SecuritySafeCritical]
		[ReflectionPermission(SecurityAction.Demand, Flags = ReflectionPermissionFlag.MemberAccess)]
		public override MethodBody GetMethodBody()
		{
			MethodBody methodBody = RuntimeMethodHandle.GetMethodBody(this, this.ReflectedTypeInternal);
			if (methodBody != null)
			{
				methodBody.m_methodBase = this;
			}
			return methodBody;
		}

		// Token: 0x06004771 RID: 18289 RVA: 0x00104495 File Offset: 0x00102695
		private void CheckConsistency(object target)
		{
			if ((this.m_methodAttributes & MethodAttributes.Static) == MethodAttributes.Static || this.m_declaringType.IsInstanceOfType(target))
			{
				return;
			}
			if (target == null)
			{
				throw new TargetException(Environment.GetResourceString("RFLCT.Targ_StatMethReqTarg"));
			}
			throw new TargetException(Environment.GetResourceString("RFLCT.Targ_ITargMismatch"));
		}

		// Token: 0x06004772 RID: 18290 RVA: 0x001044D8 File Offset: 0x001026D8
		[SecuritySafeCritical]
		private void ThrowNoInvokeException()
		{
			Type declaringType = this.DeclaringType;
			if ((declaringType == null && this.Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyInvoke"));
			}
			if ((this.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
			{
				throw new NotSupportedException();
			}
			if ((this.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
			{
				throw new NotSupportedException();
			}
			if (this.DeclaringType.ContainsGenericParameters || this.ContainsGenericParameters)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Arg_UnboundGenParam"));
			}
			if (base.IsAbstract)
			{
				throw new MemberAccessException();
			}
			if (this.ReturnType.IsByRef)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_ByRefReturn"));
			}
			throw new TargetException();
		}

		// Token: 0x06004773 RID: 18291 RVA: 0x0010459C File Offset: 0x0010279C
		[SecuritySafeCritical]
		[DebuggerStepThrough]
		[DebuggerHidden]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			object[] arguments = this.InvokeArgumentsCheck(obj, invokeAttr, binder, parameters, culture);
			INVOCATION_FLAGS invocationFlags = this.InvocationFlags;
			if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
			{
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
				RuntimeAssembly executingAssembly = RuntimeAssembly.GetExecutingAssembly(ref stackCrawlMark);
				if (executingAssembly != null && !executingAssembly.IsSafeForReflection())
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", new object[] { base.FullName }));
				}
			}
			if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY | INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD)) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
			{
				if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
				{
					CodeAccessPermission.Demand(PermissionType.ReflectionMemberAccess);
				}
				if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
				{
					RuntimeMethodHandle.PerformSecurityCheck(obj, this, this.m_declaringType, (uint)this.m_invocationFlags);
				}
			}
			return this.UnsafeInvokeInternal(obj, parameters, arguments);
		}

		// Token: 0x06004774 RID: 18292 RVA: 0x00104634 File Offset: 0x00102834
		[SecurityCritical]
		[DebuggerStepThrough]
		[DebuggerHidden]
		internal object UnsafeInvoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			object[] arguments = this.InvokeArgumentsCheck(obj, invokeAttr, binder, parameters, culture);
			return this.UnsafeInvokeInternal(obj, parameters, arguments);
		}

		// Token: 0x06004775 RID: 18293 RVA: 0x0010465C File Offset: 0x0010285C
		[SecurityCritical]
		[DebuggerStepThrough]
		[DebuggerHidden]
		private object UnsafeInvokeInternal(object obj, object[] parameters, object[] arguments)
		{
			if (arguments == null || arguments.Length == 0)
			{
				return RuntimeMethodHandle.InvokeMethod(obj, null, this.Signature, false);
			}
			object result = RuntimeMethodHandle.InvokeMethod(obj, arguments, this.Signature, false);
			for (int i = 0; i < arguments.Length; i++)
			{
				parameters[i] = arguments[i];
			}
			return result;
		}

		// Token: 0x06004776 RID: 18294 RVA: 0x001046A4 File Offset: 0x001028A4
		[DebuggerStepThrough]
		[DebuggerHidden]
		private object[] InvokeArgumentsCheck(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			Signature signature = this.Signature;
			int num = signature.Arguments.Length;
			int num2 = ((parameters != null) ? parameters.Length : 0);
			INVOCATION_FLAGS invocationFlags = this.InvocationFlags;
			if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS)) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
			{
				this.ThrowNoInvokeException();
			}
			this.CheckConsistency(obj);
			if (num != num2)
			{
				throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
			}
			if (num2 != 0)
			{
				return base.CheckArguments(parameters, binder, invokeAttr, culture, signature);
			}
			return null;
		}

		// Token: 0x17000AF8 RID: 2808
		// (get) Token: 0x06004777 RID: 18295 RVA: 0x00104710 File Offset: 0x00102910
		public override Type ReturnType
		{
			get
			{
				return this.Signature.ReturnType;
			}
		}

		// Token: 0x17000AF9 RID: 2809
		// (get) Token: 0x06004778 RID: 18296 RVA: 0x0010471D File Offset: 0x0010291D
		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get
			{
				return this.ReturnParameter;
			}
		}

		// Token: 0x17000AFA RID: 2810
		// (get) Token: 0x06004779 RID: 18297 RVA: 0x00104725 File Offset: 0x00102925
		public override ParameterInfo ReturnParameter
		{
			[SecuritySafeCritical]
			get
			{
				this.FetchReturnParameter();
				return this.m_returnParameter;
			}
		}

		// Token: 0x0600477A RID: 18298 RVA: 0x00104734 File Offset: 0x00102934
		[SecuritySafeCritical]
		public override MethodInfo GetBaseDefinition()
		{
			if (!base.IsVirtual || base.IsStatic || this.m_declaringType == null || this.m_declaringType.IsInterface)
			{
				return this;
			}
			int slot = RuntimeMethodHandle.GetSlot(this);
			RuntimeType runtimeType = (RuntimeType)this.DeclaringType;
			RuntimeType reflectedType = runtimeType;
			RuntimeMethodHandleInternal methodHandle = default(RuntimeMethodHandleInternal);
			do
			{
				int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(runtimeType);
				if (numVirtuals <= slot)
				{
					break;
				}
				methodHandle = RuntimeTypeHandle.GetMethodAt(runtimeType, slot);
				reflectedType = runtimeType;
				runtimeType = (RuntimeType)runtimeType.BaseType;
			}
			while (runtimeType != null);
			return (MethodInfo)RuntimeType.GetMethodBase(reflectedType, methodHandle);
		}

		// Token: 0x0600477B RID: 18299 RVA: 0x001047C4 File Offset: 0x001029C4
		[SecuritySafeCritical]
		public override Delegate CreateDelegate(Type delegateType)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.CreateDelegateInternal(delegateType, null, (DelegateBindingFlags)132, ref stackCrawlMark);
		}

		// Token: 0x0600477C RID: 18300 RVA: 0x001047E4 File Offset: 0x001029E4
		[SecuritySafeCritical]
		public override Delegate CreateDelegate(Type delegateType, object target)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.CreateDelegateInternal(delegateType, target, DelegateBindingFlags.RelaxedSignature, ref stackCrawlMark);
		}

		// Token: 0x0600477D RID: 18301 RVA: 0x00104804 File Offset: 0x00102A04
		[SecurityCritical]
		private Delegate CreateDelegateInternal(Type delegateType, object firstArgument, DelegateBindingFlags bindingFlags, ref StackCrawlMark stackMark)
		{
			if (delegateType == null)
			{
				throw new ArgumentNullException("delegateType");
			}
			RuntimeType runtimeType = delegateType as RuntimeType;
			if (runtimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "delegateType");
			}
			if (!runtimeType.IsDelegate())
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "delegateType");
			}
			Delegate @delegate = Delegate.CreateDelegateInternal(runtimeType, this, firstArgument, bindingFlags, ref stackMark);
			if (@delegate == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
			}
			return @delegate;
		}

		// Token: 0x0600477E RID: 18302 RVA: 0x00104888 File Offset: 0x00102A88
		[SecuritySafeCritical]
		public override MethodInfo MakeGenericMethod(params Type[] methodInstantiation)
		{
			if (methodInstantiation == null)
			{
				throw new ArgumentNullException("methodInstantiation");
			}
			RuntimeType[] array = new RuntimeType[methodInstantiation.Length];
			if (!this.IsGenericMethodDefinition)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericMethodDefinition", new object[] { this }));
			}
			for (int i = 0; i < methodInstantiation.Length; i++)
			{
				Type type = methodInstantiation[i];
				if (type == null)
				{
					throw new ArgumentNullException();
				}
				RuntimeType runtimeType = type as RuntimeType;
				if (runtimeType == null)
				{
					Type[] array2 = new Type[methodInstantiation.Length];
					for (int j = 0; j < methodInstantiation.Length; j++)
					{
						array2[j] = methodInstantiation[j];
					}
					methodInstantiation = array2;
					return MethodBuilderInstantiation.MakeGenericMethod(this, methodInstantiation);
				}
				array[i] = runtimeType;
			}
			RuntimeType[] genericArgumentsInternal = this.GetGenericArgumentsInternal();
			RuntimeType.SanityCheckGenericArguments(array, genericArgumentsInternal);
			MethodInfo result = null;
			try
			{
				result = RuntimeType.GetMethodBase(this.ReflectedTypeInternal, RuntimeMethodHandle.GetStubIfNeeded(new RuntimeMethodHandleInternal(this.m_handle), this.m_declaringType, array)) as MethodInfo;
			}
			catch (VerificationException e)
			{
				RuntimeType.ValidateGenericArguments(this, array, e);
				throw;
			}
			return result;
		}

		// Token: 0x0600477F RID: 18303 RVA: 0x00104994 File Offset: 0x00102B94
		internal RuntimeType[] GetGenericArgumentsInternal()
		{
			return RuntimeMethodHandle.GetMethodInstantiationInternal(this);
		}

		// Token: 0x06004780 RID: 18304 RVA: 0x0010499C File Offset: 0x00102B9C
		public override Type[] GetGenericArguments()
		{
			Type[] array = RuntimeMethodHandle.GetMethodInstantiationPublic(this);
			if (array == null)
			{
				array = EmptyArray<Type>.Value;
			}
			return array;
		}

		// Token: 0x06004781 RID: 18305 RVA: 0x001049BA File Offset: 0x00102BBA
		public override MethodInfo GetGenericMethodDefinition()
		{
			if (!this.IsGenericMethod)
			{
				throw new InvalidOperationException();
			}
			return RuntimeType.GetMethodBase(this.m_declaringType, RuntimeMethodHandle.StripMethodInstantiation(this)) as MethodInfo;
		}

		// Token: 0x17000AFB RID: 2811
		// (get) Token: 0x06004782 RID: 18306 RVA: 0x001049E0 File Offset: 0x00102BE0
		public override bool IsGenericMethod
		{
			get
			{
				return RuntimeMethodHandle.HasMethodInstantiation(this);
			}
		}

		// Token: 0x17000AFC RID: 2812
		// (get) Token: 0x06004783 RID: 18307 RVA: 0x001049E8 File Offset: 0x00102BE8
		public override bool IsGenericMethodDefinition
		{
			get
			{
				return RuntimeMethodHandle.IsGenericMethodDefinition(this);
			}
		}

		// Token: 0x17000AFD RID: 2813
		// (get) Token: 0x06004784 RID: 18308 RVA: 0x001049F0 File Offset: 0x00102BF0
		public override bool ContainsGenericParameters
		{
			get
			{
				if (this.DeclaringType != null && this.DeclaringType.ContainsGenericParameters)
				{
					return true;
				}
				if (!this.IsGenericMethod)
				{
					return false;
				}
				Type[] genericArguments = this.GetGenericArguments();
				for (int i = 0; i < genericArguments.Length; i++)
				{
					if (genericArguments[i].ContainsGenericParameters)
					{
						return true;
					}
				}
				return false;
			}
		}

		// Token: 0x06004785 RID: 18309 RVA: 0x00104A48 File Offset: 0x00102C48
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			if (this.m_reflectedTypeCache.IsGlobal)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_GlobalMethodSerialization"));
			}
			MemberInfoSerializationHolder.GetSerializationInfo(info, this.Name, this.ReflectedTypeInternal, this.ToString(), this.SerializationToString(), MemberTypes.Method, (this.IsGenericMethod & !this.IsGenericMethodDefinition) ? this.GetGenericArguments() : null);
		}

		// Token: 0x06004786 RID: 18310 RVA: 0x00104ABA File Offset: 0x00102CBA
		internal string SerializationToString()
		{
			return this.ReturnType.FormatTypeName(true) + " " + this.FormatNameAndSig(true);
		}

		// Token: 0x06004787 RID: 18311 RVA: 0x00104ADC File Offset: 0x00102CDC
		internal static MethodBase InternalGetCurrentMethod(ref StackCrawlMark stackMark)
		{
			IRuntimeMethodInfo currentMethod = RuntimeMethodHandle.GetCurrentMethod(ref stackMark);
			if (currentMethod == null)
			{
				return null;
			}
			return RuntimeType.GetMethodBase(currentMethod);
		}

		// Token: 0x04001DA1 RID: 7585
		private IntPtr m_handle;

		// Token: 0x04001DA2 RID: 7586
		private RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

		// Token: 0x04001DA3 RID: 7587
		private string m_name;

		// Token: 0x04001DA4 RID: 7588
		private string m_toString;

		// Token: 0x04001DA5 RID: 7589
		private ParameterInfo[] m_parameters;

		// Token: 0x04001DA6 RID: 7590
		private ParameterInfo m_returnParameter;

		// Token: 0x04001DA7 RID: 7591
		private BindingFlags m_bindingFlags;

		// Token: 0x04001DA8 RID: 7592
		private MethodAttributes m_methodAttributes;

		// Token: 0x04001DA9 RID: 7593
		private Signature m_signature;

		// Token: 0x04001DAA RID: 7594
		private RuntimeType m_declaringType;

		// Token: 0x04001DAB RID: 7595
		private object m_keepalive;

		// Token: 0x04001DAC RID: 7596
		private INVOCATION_FLAGS m_invocationFlags;

		// Token: 0x04001DAD RID: 7597
		private RemotingMethodCachedData m_cachedData;
	}
}
