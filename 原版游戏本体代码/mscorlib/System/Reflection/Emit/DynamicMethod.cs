using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Reflection.Emit
{
	// Token: 0x02000638 RID: 1592
	[ComVisible(true)]
	public sealed class DynamicMethod : MethodInfo
	{
		// Token: 0x06004A32 RID: 18994 RVA: 0x0010C7A5 File Offset: 0x0010A9A5
		private DynamicMethod()
		{
		}

		// Token: 0x06004A33 RID: 18995 RVA: 0x0010C7B0 File Offset: 0x0010A9B0
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public DynamicMethod(string name, Type returnType, Type[] parameterTypes)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.Init(name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, false, true, ref stackCrawlMark);
		}

		// Token: 0x06004A34 RID: 18996 RVA: 0x0010C7D8 File Offset: 0x0010A9D8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public DynamicMethod(string name, Type returnType, Type[] parameterTypes, bool restrictedSkipVisibility)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.Init(name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, restrictedSkipVisibility, true, ref stackCrawlMark);
		}

		// Token: 0x06004A35 RID: 18997 RVA: 0x0010C800 File Offset: 0x0010AA00
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public DynamicMethod(string name, Type returnType, Type[] parameterTypes, Module m)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PerformSecurityCheck(m, ref stackCrawlMark, false);
			this.Init(name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, m, false, false, ref stackCrawlMark);
		}

		// Token: 0x06004A36 RID: 18998 RVA: 0x0010C834 File Offset: 0x0010AA34
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public DynamicMethod(string name, Type returnType, Type[] parameterTypes, Module m, bool skipVisibility)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PerformSecurityCheck(m, ref stackCrawlMark, skipVisibility);
			this.Init(name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, m, skipVisibility, false, ref stackCrawlMark);
		}

		// Token: 0x06004A37 RID: 18999 RVA: 0x0010C86C File Offset: 0x0010AA6C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Module m, bool skipVisibility)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PerformSecurityCheck(m, ref stackCrawlMark, skipVisibility);
			this.Init(name, attributes, callingConvention, returnType, parameterTypes, null, m, skipVisibility, false, ref stackCrawlMark);
		}

		// Token: 0x06004A38 RID: 19000 RVA: 0x0010C8A4 File Offset: 0x0010AAA4
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public DynamicMethod(string name, Type returnType, Type[] parameterTypes, Type owner)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PerformSecurityCheck(owner, ref stackCrawlMark, false);
			this.Init(name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, null, false, false, ref stackCrawlMark);
		}

		// Token: 0x06004A39 RID: 19001 RVA: 0x0010C8D8 File Offset: 0x0010AAD8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public DynamicMethod(string name, Type returnType, Type[] parameterTypes, Type owner, bool skipVisibility)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PerformSecurityCheck(owner, ref stackCrawlMark, skipVisibility);
			this.Init(name, MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, null, skipVisibility, false, ref stackCrawlMark);
		}

		// Token: 0x06004A3A RID: 19002 RVA: 0x0010C910 File Offset: 0x0010AB10
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type owner, bool skipVisibility)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			this.PerformSecurityCheck(owner, ref stackCrawlMark, skipVisibility);
			this.Init(name, attributes, callingConvention, returnType, parameterTypes, owner, null, skipVisibility, false, ref stackCrawlMark);
		}

		// Token: 0x06004A3B RID: 19003 RVA: 0x0010C948 File Offset: 0x0010AB48
		private static void CheckConsistency(MethodAttributes attributes, CallingConventions callingConvention)
		{
			if ((attributes & ~MethodAttributes.MemberAccessMask) != MethodAttributes.Static)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
			}
			if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
			}
			if (callingConvention != CallingConventions.Standard && callingConvention != CallingConventions.VarArgs)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
			}
			if (callingConvention == CallingConventions.VarArgs)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
			}
		}

		// Token: 0x06004A3C RID: 19004 RVA: 0x0010C9B0 File Offset: 0x0010ABB0
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static RuntimeModule GetDynamicMethodsModule()
		{
			if (DynamicMethod.s_anonymouslyHostedDynamicMethodsModule != null)
			{
				return DynamicMethod.s_anonymouslyHostedDynamicMethodsModule;
			}
			object obj = DynamicMethod.s_anonymouslyHostedDynamicMethodsModuleLock;
			lock (obj)
			{
				if (DynamicMethod.s_anonymouslyHostedDynamicMethodsModule != null)
				{
					return DynamicMethod.s_anonymouslyHostedDynamicMethodsModule;
				}
				ConstructorInfo constructor = typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes);
				CustomAttributeBuilder item = new CustomAttributeBuilder(constructor, EmptyArray<object>.Value);
				List<CustomAttributeBuilder> list = new List<CustomAttributeBuilder>();
				list.Add(item);
				ConstructorInfo constructor2 = typeof(SecurityRulesAttribute).GetConstructor(new Type[] { typeof(SecurityRuleSet) });
				CustomAttributeBuilder item2 = new CustomAttributeBuilder(constructor2, new object[] { SecurityRuleSet.Level1 });
				list.Add(item2);
				AssemblyName name = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMe;
				AssemblyBuilder assemblyBuilder = AssemblyBuilder.InternalDefineDynamicAssembly(name, AssemblyBuilderAccess.Run, null, null, null, null, null, ref stackCrawlMark, list, SecurityContextSource.CurrentAssembly);
				AppDomain.PublishAnonymouslyHostedDynamicMethodsAssembly(assemblyBuilder.GetNativeHandle());
				DynamicMethod.s_anonymouslyHostedDynamicMethodsModule = (InternalModuleBuilder)assemblyBuilder.ManifestModule;
			}
			return DynamicMethod.s_anonymouslyHostedDynamicMethodsModule;
		}

		// Token: 0x06004A3D RID: 19005 RVA: 0x0010CAE0 File Offset: 0x0010ACE0
		[SecurityCritical]
		private void Init(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] signature, Type owner, Module m, bool skipVisibility, bool transparentMethod, ref StackCrawlMark stackMark)
		{
			DynamicMethod.CheckConsistency(attributes, callingConvention);
			if (signature != null)
			{
				this.m_parameterTypes = new RuntimeType[signature.Length];
				for (int i = 0; i < signature.Length; i++)
				{
					if (signature[i] == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature"));
					}
					this.m_parameterTypes[i] = signature[i].UnderlyingSystemType as RuntimeType;
					if (this.m_parameterTypes[i] == null || this.m_parameterTypes[i] == null || this.m_parameterTypes[i] == (RuntimeType)typeof(void))
					{
						throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature"));
					}
				}
			}
			else
			{
				this.m_parameterTypes = new RuntimeType[0];
			}
			this.m_returnType = ((returnType == null) ? ((RuntimeType)typeof(void)) : (returnType.UnderlyingSystemType as RuntimeType));
			if (this.m_returnType == null || this.m_returnType == null || this.m_returnType.IsByRef)
			{
				throw new NotSupportedException(Environment.GetResourceString("Arg_InvalidTypeInRetType"));
			}
			if (transparentMethod)
			{
				this.m_module = DynamicMethod.GetDynamicMethodsModule();
				if (skipVisibility)
				{
					this.m_restrictedSkipVisibility = true;
				}
				this.m_creationContext = CompressedStack.Capture();
			}
			else
			{
				if (m != null)
				{
					this.m_module = m.ModuleHandle.GetRuntimeModule();
				}
				else
				{
					RuntimeType runtimeType = null;
					if (owner != null)
					{
						runtimeType = owner.UnderlyingSystemType as RuntimeType;
					}
					if (runtimeType != null)
					{
						if (runtimeType.HasElementType || runtimeType.ContainsGenericParameters || runtimeType.IsGenericParameter || runtimeType.IsInterface)
						{
							throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeForDynamicMethod"));
						}
						this.m_typeOwner = runtimeType;
						this.m_module = runtimeType.GetRuntimeModule();
					}
				}
				this.m_skipVisibility = skipVisibility;
			}
			this.m_ilGenerator = null;
			this.m_fInitLocals = true;
			this.m_methodHandle = null;
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (AppDomain.ProfileAPICheck)
			{
				if (this.m_creatorAssembly == null)
				{
					this.m_creatorAssembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
				}
				if (this.m_creatorAssembly != null && !this.m_creatorAssembly.IsFrameworkAssembly())
				{
					this.m_profileAPICheck = true;
				}
			}
			this.m_dynMethod = new DynamicMethod.RTDynamicMethod(this, name, attributes, callingConvention);
		}

		// Token: 0x06004A3E RID: 19006 RVA: 0x0010CD34 File Offset: 0x0010AF34
		[SecurityCritical]
		private void PerformSecurityCheck(Module m, ref StackCrawlMark stackMark, bool skipVisibility)
		{
			if (m == null)
			{
				throw new ArgumentNullException("m");
			}
			ModuleBuilder moduleBuilder = m as ModuleBuilder;
			RuntimeModule left;
			if (moduleBuilder != null)
			{
				left = moduleBuilder.InternalModule;
			}
			else
			{
				left = m as RuntimeModule;
			}
			if (left == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeModule"), "m");
			}
			if (left == DynamicMethod.s_anonymouslyHostedDynamicMethodsModule)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"), "m");
			}
			if (skipVisibility)
			{
				new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
			}
			RuntimeType callerType = RuntimeMethodHandle.GetCallerType(ref stackMark);
			this.m_creatorAssembly = callerType.GetRuntimeAssembly();
			if (m.Assembly != this.m_creatorAssembly)
			{
				CodeAccessSecurityEngine.ReflectionTargetDemandHelper(PermissionType.SecurityControlEvidence, m.Assembly.PermissionSet);
			}
		}

		// Token: 0x06004A3F RID: 19007 RVA: 0x0010CE00 File Offset: 0x0010B000
		[SecurityCritical]
		private void PerformSecurityCheck(Type owner, ref StackCrawlMark stackMark, bool skipVisibility)
		{
			if (owner == null)
			{
				throw new ArgumentNullException("owner");
			}
			RuntimeType runtimeType = owner as RuntimeType;
			if (runtimeType == null)
			{
				runtimeType = owner.UnderlyingSystemType as RuntimeType;
			}
			if (runtimeType == null)
			{
				throw new ArgumentNullException("owner", Environment.GetResourceString("Argument_MustBeRuntimeType"));
			}
			RuntimeType callerType = RuntimeMethodHandle.GetCallerType(ref stackMark);
			if (skipVisibility)
			{
				new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
			}
			else if (callerType != runtimeType)
			{
				new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
			}
			this.m_creatorAssembly = callerType.GetRuntimeAssembly();
			if (runtimeType.Assembly != this.m_creatorAssembly)
			{
				CodeAccessSecurityEngine.ReflectionTargetDemandHelper(PermissionType.SecurityControlEvidence, owner.Assembly.PermissionSet);
			}
		}

		// Token: 0x06004A40 RID: 19008 RVA: 0x0010CEB8 File Offset: 0x0010B0B8
		[SecuritySafeCritical]
		[ComVisible(true)]
		public sealed override Delegate CreateDelegate(Type delegateType)
		{
			if (this.m_restrictedSkipVisibility)
			{
				this.GetMethodDescriptor();
				RuntimeHelpers._CompileMethod(this.m_methodHandle);
			}
			MulticastDelegate multicastDelegate = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, null, this.GetMethodDescriptor());
			multicastDelegate.StoreDynamicMethod(this.GetMethodInfo());
			return multicastDelegate;
		}

		// Token: 0x06004A41 RID: 19009 RVA: 0x0010CF00 File Offset: 0x0010B100
		[SecuritySafeCritical]
		[ComVisible(true)]
		public sealed override Delegate CreateDelegate(Type delegateType, object target)
		{
			if (this.m_restrictedSkipVisibility)
			{
				this.GetMethodDescriptor();
				RuntimeHelpers._CompileMethod(this.m_methodHandle);
			}
			MulticastDelegate multicastDelegate = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, this.GetMethodDescriptor());
			multicastDelegate.StoreDynamicMethod(this.GetMethodInfo());
			return multicastDelegate;
		}

		// Token: 0x17000B91 RID: 2961
		// (get) Token: 0x06004A42 RID: 19010 RVA: 0x0010CF47 File Offset: 0x0010B147
		// (set) Token: 0x06004A43 RID: 19011 RVA: 0x0010CF4F File Offset: 0x0010B14F
		internal bool ProfileAPICheck
		{
			get
			{
				return this.m_profileAPICheck;
			}
			[FriendAccessAllowed]
			set
			{
				this.m_profileAPICheck = value;
			}
		}

		// Token: 0x06004A44 RID: 19012 RVA: 0x0010CF58 File Offset: 0x0010B158
		[SecurityCritical]
		internal RuntimeMethodHandle GetMethodDescriptor()
		{
			if (this.m_methodHandle == null)
			{
				lock (this)
				{
					if (this.m_methodHandle == null)
					{
						if (this.m_DynamicILInfo != null)
						{
							this.m_DynamicILInfo.GetCallableMethod(this.m_module, this);
						}
						else
						{
							if (this.m_ilGenerator == null || this.m_ilGenerator.ILOffset == 0)
							{
								throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadEmptyMethodBody", new object[] { this.Name }));
							}
							this.m_ilGenerator.GetCallableMethod(this.m_module, this);
						}
					}
				}
			}
			return new RuntimeMethodHandle(this.m_methodHandle);
		}

		// Token: 0x06004A45 RID: 19013 RVA: 0x0010D010 File Offset: 0x0010B210
		public override string ToString()
		{
			return this.m_dynMethod.ToString();
		}

		// Token: 0x17000B92 RID: 2962
		// (get) Token: 0x06004A46 RID: 19014 RVA: 0x0010D01D File Offset: 0x0010B21D
		public override string Name
		{
			get
			{
				return this.m_dynMethod.Name;
			}
		}

		// Token: 0x17000B93 RID: 2963
		// (get) Token: 0x06004A47 RID: 19015 RVA: 0x0010D02A File Offset: 0x0010B22A
		public override Type DeclaringType
		{
			get
			{
				return this.m_dynMethod.DeclaringType;
			}
		}

		// Token: 0x17000B94 RID: 2964
		// (get) Token: 0x06004A48 RID: 19016 RVA: 0x0010D037 File Offset: 0x0010B237
		public override Type ReflectedType
		{
			get
			{
				return this.m_dynMethod.ReflectedType;
			}
		}

		// Token: 0x17000B95 RID: 2965
		// (get) Token: 0x06004A49 RID: 19017 RVA: 0x0010D044 File Offset: 0x0010B244
		public override Module Module
		{
			get
			{
				return this.m_dynMethod.Module;
			}
		}

		// Token: 0x17000B96 RID: 2966
		// (get) Token: 0x06004A4A RID: 19018 RVA: 0x0010D051 File Offset: 0x0010B251
		public override RuntimeMethodHandle MethodHandle
		{
			get
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
			}
		}

		// Token: 0x17000B97 RID: 2967
		// (get) Token: 0x06004A4B RID: 19019 RVA: 0x0010D062 File Offset: 0x0010B262
		public override MethodAttributes Attributes
		{
			get
			{
				return this.m_dynMethod.Attributes;
			}
		}

		// Token: 0x17000B98 RID: 2968
		// (get) Token: 0x06004A4C RID: 19020 RVA: 0x0010D06F File Offset: 0x0010B26F
		public override CallingConventions CallingConvention
		{
			get
			{
				return this.m_dynMethod.CallingConvention;
			}
		}

		// Token: 0x06004A4D RID: 19021 RVA: 0x0010D07C File Offset: 0x0010B27C
		public override MethodInfo GetBaseDefinition()
		{
			return this;
		}

		// Token: 0x06004A4E RID: 19022 RVA: 0x0010D07F File Offset: 0x0010B27F
		public override ParameterInfo[] GetParameters()
		{
			return this.m_dynMethod.GetParameters();
		}

		// Token: 0x06004A4F RID: 19023 RVA: 0x0010D08C File Offset: 0x0010B28C
		public override MethodImplAttributes GetMethodImplementationFlags()
		{
			return this.m_dynMethod.GetMethodImplementationFlags();
		}

		// Token: 0x17000B99 RID: 2969
		// (get) Token: 0x06004A50 RID: 19024 RVA: 0x0010D09C File Offset: 0x0010B29C
		public override bool IsSecurityCritical
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_methodHandle != null)
				{
					return RuntimeMethodHandle.IsSecurityCritical(this.m_methodHandle);
				}
				if (this.m_typeOwner != null)
				{
					RuntimeAssembly runtimeAssembly = this.m_typeOwner.Assembly as RuntimeAssembly;
					return runtimeAssembly.IsAllSecurityCritical();
				}
				RuntimeAssembly runtimeAssembly2 = this.m_module.Assembly as RuntimeAssembly;
				return runtimeAssembly2.IsAllSecurityCritical();
			}
		}

		// Token: 0x17000B9A RID: 2970
		// (get) Token: 0x06004A51 RID: 19025 RVA: 0x0010D0FC File Offset: 0x0010B2FC
		public override bool IsSecuritySafeCritical
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_methodHandle != null)
				{
					return RuntimeMethodHandle.IsSecuritySafeCritical(this.m_methodHandle);
				}
				if (this.m_typeOwner != null)
				{
					RuntimeAssembly runtimeAssembly = this.m_typeOwner.Assembly as RuntimeAssembly;
					return runtimeAssembly.IsAllPublicAreaSecuritySafeCritical();
				}
				RuntimeAssembly runtimeAssembly2 = this.m_module.Assembly as RuntimeAssembly;
				return runtimeAssembly2.IsAllSecuritySafeCritical();
			}
		}

		// Token: 0x17000B9B RID: 2971
		// (get) Token: 0x06004A52 RID: 19026 RVA: 0x0010D15C File Offset: 0x0010B35C
		public override bool IsSecurityTransparent
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_methodHandle != null)
				{
					return RuntimeMethodHandle.IsSecurityTransparent(this.m_methodHandle);
				}
				if (this.m_typeOwner != null)
				{
					RuntimeAssembly runtimeAssembly = this.m_typeOwner.Assembly as RuntimeAssembly;
					return !runtimeAssembly.IsAllSecurityCritical();
				}
				RuntimeAssembly runtimeAssembly2 = this.m_module.Assembly as RuntimeAssembly;
				return !runtimeAssembly2.IsAllSecurityCritical();
			}
		}

		// Token: 0x06004A53 RID: 19027 RVA: 0x0010D1C0 File Offset: 0x0010B3C0
		[SecuritySafeCritical]
		public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
		{
			if ((this.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_CallToVarArg"));
			}
			RuntimeMethodHandle methodDescriptor = this.GetMethodDescriptor();
			Signature signature = new Signature(this.m_methodHandle, this.m_parameterTypes, this.m_returnType, this.CallingConvention);
			int num = signature.Arguments.Length;
			int num2 = ((parameters != null) ? parameters.Length : 0);
			if (num != num2)
			{
				throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
			}
			object result;
			if (num2 > 0)
			{
				object[] array = base.CheckArguments(parameters, binder, invokeAttr, culture, signature);
				result = RuntimeMethodHandle.InvokeMethod(null, array, signature, false);
				for (int i = 0; i < array.Length; i++)
				{
					parameters[i] = array[i];
				}
			}
			else
			{
				result = RuntimeMethodHandle.InvokeMethod(null, null, signature, false);
			}
			GC.KeepAlive(this);
			return result;
		}

		// Token: 0x06004A54 RID: 19028 RVA: 0x0010D28A File Offset: 0x0010B48A
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this.m_dynMethod.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06004A55 RID: 19029 RVA: 0x0010D299 File Offset: 0x0010B499
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.m_dynMethod.GetCustomAttributes(inherit);
		}

		// Token: 0x06004A56 RID: 19030 RVA: 0x0010D2A7 File Offset: 0x0010B4A7
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this.m_dynMethod.IsDefined(attributeType, inherit);
		}

		// Token: 0x17000B9C RID: 2972
		// (get) Token: 0x06004A57 RID: 19031 RVA: 0x0010D2B6 File Offset: 0x0010B4B6
		public override Type ReturnType
		{
			get
			{
				return this.m_dynMethod.ReturnType;
			}
		}

		// Token: 0x17000B9D RID: 2973
		// (get) Token: 0x06004A58 RID: 19032 RVA: 0x0010D2C3 File Offset: 0x0010B4C3
		public override ParameterInfo ReturnParameter
		{
			get
			{
				return this.m_dynMethod.ReturnParameter;
			}
		}

		// Token: 0x17000B9E RID: 2974
		// (get) Token: 0x06004A59 RID: 19033 RVA: 0x0010D2D0 File Offset: 0x0010B4D0
		public override ICustomAttributeProvider ReturnTypeCustomAttributes
		{
			get
			{
				return this.m_dynMethod.ReturnTypeCustomAttributes;
			}
		}

		// Token: 0x06004A5A RID: 19034 RVA: 0x0010D2E0 File Offset: 0x0010B4E0
		public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName)
		{
			if (position < 0 || position > this.m_parameterTypes.Length)
			{
				throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_ParamSequence"));
			}
			position--;
			if (position >= 0)
			{
				ParameterInfo[] array = this.m_dynMethod.LoadParameters();
				array[position].SetName(parameterName);
				array[position].SetAttributes(attributes);
			}
			return null;
		}

		// Token: 0x06004A5B RID: 19035 RVA: 0x0010D334 File Offset: 0x0010B534
		[SecuritySafeCritical]
		public DynamicILInfo GetDynamicILInfo()
		{
			new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
			if (this.m_DynamicILInfo != null)
			{
				return this.m_DynamicILInfo;
			}
			return this.GetDynamicILInfo(new DynamicScope());
		}

		// Token: 0x06004A5C RID: 19036 RVA: 0x0010D35C File Offset: 0x0010B55C
		[SecurityCritical]
		internal DynamicILInfo GetDynamicILInfo(DynamicScope scope)
		{
			if (this.m_DynamicILInfo == null)
			{
				Module scope2 = null;
				CallingConventions callingConvention = this.CallingConvention;
				Type returnType = this.ReturnType;
				Type[] requiredReturnTypeCustomModifiers = null;
				Type[] optionalReturnTypeCustomModifiers = null;
				Type[] parameterTypes = this.m_parameterTypes;
				byte[] signature = SignatureHelper.GetMethodSigHelper(scope2, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, null, null).GetSignature(true);
				this.m_DynamicILInfo = new DynamicILInfo(scope, this, signature);
			}
			return this.m_DynamicILInfo;
		}

		// Token: 0x06004A5D RID: 19037 RVA: 0x0010D3AA File Offset: 0x0010B5AA
		public ILGenerator GetILGenerator()
		{
			return this.GetILGenerator(64);
		}

		// Token: 0x06004A5E RID: 19038 RVA: 0x0010D3B4 File Offset: 0x0010B5B4
		[SecuritySafeCritical]
		public ILGenerator GetILGenerator(int streamSize)
		{
			if (this.m_ilGenerator == null)
			{
				Module scope = null;
				CallingConventions callingConvention = this.CallingConvention;
				Type returnType = this.ReturnType;
				Type[] requiredReturnTypeCustomModifiers = null;
				Type[] optionalReturnTypeCustomModifiers = null;
				Type[] parameterTypes = this.m_parameterTypes;
				byte[] signature = SignatureHelper.GetMethodSigHelper(scope, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, null, null).GetSignature(true);
				this.m_ilGenerator = new DynamicILGenerator(this, signature, streamSize);
			}
			return this.m_ilGenerator;
		}

		// Token: 0x17000B9F RID: 2975
		// (get) Token: 0x06004A5F RID: 19039 RVA: 0x0010D402 File Offset: 0x0010B602
		// (set) Token: 0x06004A60 RID: 19040 RVA: 0x0010D40A File Offset: 0x0010B60A
		public bool InitLocals
		{
			get
			{
				return this.m_fInitLocals;
			}
			set
			{
				this.m_fInitLocals = value;
			}
		}

		// Token: 0x06004A61 RID: 19041 RVA: 0x0010D413 File Offset: 0x0010B613
		internal MethodInfo GetMethodInfo()
		{
			return this.m_dynMethod;
		}

		// Token: 0x04001E9E RID: 7838
		private RuntimeType[] m_parameterTypes;

		// Token: 0x04001E9F RID: 7839
		internal IRuntimeMethodInfo m_methodHandle;

		// Token: 0x04001EA0 RID: 7840
		private RuntimeType m_returnType;

		// Token: 0x04001EA1 RID: 7841
		private DynamicILGenerator m_ilGenerator;

		// Token: 0x04001EA2 RID: 7842
		private DynamicILInfo m_DynamicILInfo;

		// Token: 0x04001EA3 RID: 7843
		private bool m_fInitLocals;

		// Token: 0x04001EA4 RID: 7844
		private RuntimeModule m_module;

		// Token: 0x04001EA5 RID: 7845
		internal bool m_skipVisibility;

		// Token: 0x04001EA6 RID: 7846
		internal RuntimeType m_typeOwner;

		// Token: 0x04001EA7 RID: 7847
		private DynamicMethod.RTDynamicMethod m_dynMethod;

		// Token: 0x04001EA8 RID: 7848
		internal DynamicResolver m_resolver;

		// Token: 0x04001EA9 RID: 7849
		private bool m_profileAPICheck;

		// Token: 0x04001EAA RID: 7850
		private RuntimeAssembly m_creatorAssembly;

		// Token: 0x04001EAB RID: 7851
		internal bool m_restrictedSkipVisibility;

		// Token: 0x04001EAC RID: 7852
		internal CompressedStack m_creationContext;

		// Token: 0x04001EAD RID: 7853
		private static volatile InternalModuleBuilder s_anonymouslyHostedDynamicMethodsModule;

		// Token: 0x04001EAE RID: 7854
		private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();

		// Token: 0x02000C41 RID: 3137
		internal class RTDynamicMethod : MethodInfo
		{
			// Token: 0x06007050 RID: 28752 RVA: 0x00183154 File Offset: 0x00181354
			private RTDynamicMethod()
			{
			}

			// Token: 0x06007051 RID: 28753 RVA: 0x0018315C File Offset: 0x0018135C
			internal RTDynamicMethod(DynamicMethod owner, string name, MethodAttributes attributes, CallingConventions callingConvention)
			{
				this.m_owner = owner;
				this.m_name = name;
				this.m_attributes = attributes;
				this.m_callingConvention = callingConvention;
			}

			// Token: 0x06007052 RID: 28754 RVA: 0x00183181 File Offset: 0x00181381
			public override string ToString()
			{
				return this.ReturnType.FormatTypeName() + " " + base.FormatNameAndSig();
			}

			// Token: 0x17001342 RID: 4930
			// (get) Token: 0x06007053 RID: 28755 RVA: 0x0018319E File Offset: 0x0018139E
			public override string Name
			{
				get
				{
					return this.m_name;
				}
			}

			// Token: 0x17001343 RID: 4931
			// (get) Token: 0x06007054 RID: 28756 RVA: 0x001831A6 File Offset: 0x001813A6
			public override Type DeclaringType
			{
				get
				{
					return null;
				}
			}

			// Token: 0x17001344 RID: 4932
			// (get) Token: 0x06007055 RID: 28757 RVA: 0x001831A9 File Offset: 0x001813A9
			public override Type ReflectedType
			{
				get
				{
					return null;
				}
			}

			// Token: 0x17001345 RID: 4933
			// (get) Token: 0x06007056 RID: 28758 RVA: 0x001831AC File Offset: 0x001813AC
			public override Module Module
			{
				get
				{
					return this.m_owner.m_module;
				}
			}

			// Token: 0x17001346 RID: 4934
			// (get) Token: 0x06007057 RID: 28759 RVA: 0x001831B9 File Offset: 0x001813B9
			public override RuntimeMethodHandle MethodHandle
			{
				get
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
				}
			}

			// Token: 0x17001347 RID: 4935
			// (get) Token: 0x06007058 RID: 28760 RVA: 0x001831CA File Offset: 0x001813CA
			public override MethodAttributes Attributes
			{
				get
				{
					return this.m_attributes;
				}
			}

			// Token: 0x17001348 RID: 4936
			// (get) Token: 0x06007059 RID: 28761 RVA: 0x001831D2 File Offset: 0x001813D2
			public override CallingConventions CallingConvention
			{
				get
				{
					return this.m_callingConvention;
				}
			}

			// Token: 0x0600705A RID: 28762 RVA: 0x001831DA File Offset: 0x001813DA
			public override MethodInfo GetBaseDefinition()
			{
				return this;
			}

			// Token: 0x0600705B RID: 28763 RVA: 0x001831E0 File Offset: 0x001813E0
			public override ParameterInfo[] GetParameters()
			{
				ParameterInfo[] array = this.LoadParameters();
				ParameterInfo[] array2 = new ParameterInfo[array.Length];
				Array.Copy(array, array2, array.Length);
				return array2;
			}

			// Token: 0x0600705C RID: 28764 RVA: 0x00183208 File Offset: 0x00181408
			public override MethodImplAttributes GetMethodImplementationFlags()
			{
				return MethodImplAttributes.NoInlining;
			}

			// Token: 0x0600705D RID: 28765 RVA: 0x0018320B File Offset: 0x0018140B
			public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "this");
			}

			// Token: 0x0600705E RID: 28766 RVA: 0x00183224 File Offset: 0x00181424
			public override object[] GetCustomAttributes(Type attributeType, bool inherit)
			{
				if (attributeType == null)
				{
					throw new ArgumentNullException("attributeType");
				}
				if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
				{
					return new object[]
					{
						new MethodImplAttribute(this.GetMethodImplementationFlags())
					};
				}
				return EmptyArray<object>.Value;
			}

			// Token: 0x0600705F RID: 28767 RVA: 0x00183271 File Offset: 0x00181471
			public override object[] GetCustomAttributes(bool inherit)
			{
				return new object[]
				{
					new MethodImplAttribute(this.GetMethodImplementationFlags())
				};
			}

			// Token: 0x06007060 RID: 28768 RVA: 0x00183287 File Offset: 0x00181487
			public override bool IsDefined(Type attributeType, bool inherit)
			{
				if (attributeType == null)
				{
					throw new ArgumentNullException("attributeType");
				}
				return attributeType.IsAssignableFrom(typeof(MethodImplAttribute));
			}

			// Token: 0x17001349 RID: 4937
			// (get) Token: 0x06007061 RID: 28769 RVA: 0x001832B2 File Offset: 0x001814B2
			public override bool IsSecurityCritical
			{
				get
				{
					return this.m_owner.IsSecurityCritical;
				}
			}

			// Token: 0x1700134A RID: 4938
			// (get) Token: 0x06007062 RID: 28770 RVA: 0x001832BF File Offset: 0x001814BF
			public override bool IsSecuritySafeCritical
			{
				get
				{
					return this.m_owner.IsSecuritySafeCritical;
				}
			}

			// Token: 0x1700134B RID: 4939
			// (get) Token: 0x06007063 RID: 28771 RVA: 0x001832CC File Offset: 0x001814CC
			public override bool IsSecurityTransparent
			{
				get
				{
					return this.m_owner.IsSecurityTransparent;
				}
			}

			// Token: 0x1700134C RID: 4940
			// (get) Token: 0x06007064 RID: 28772 RVA: 0x001832D9 File Offset: 0x001814D9
			public override Type ReturnType
			{
				get
				{
					return this.m_owner.m_returnType;
				}
			}

			// Token: 0x1700134D RID: 4941
			// (get) Token: 0x06007065 RID: 28773 RVA: 0x001832E6 File Offset: 0x001814E6
			public override ParameterInfo ReturnParameter
			{
				get
				{
					return null;
				}
			}

			// Token: 0x1700134E RID: 4942
			// (get) Token: 0x06007066 RID: 28774 RVA: 0x001832E9 File Offset: 0x001814E9
			public override ICustomAttributeProvider ReturnTypeCustomAttributes
			{
				get
				{
					return this.GetEmptyCAHolder();
				}
			}

			// Token: 0x06007067 RID: 28775 RVA: 0x001832F4 File Offset: 0x001814F4
			internal ParameterInfo[] LoadParameters()
			{
				if (this.m_parameters == null)
				{
					Type[] parameterTypes = this.m_owner.m_parameterTypes;
					Type[] array = parameterTypes;
					ParameterInfo[] array2 = new ParameterInfo[array.Length];
					for (int i = 0; i < array.Length; i++)
					{
						array2[i] = new RuntimeParameterInfo(this, null, array[i], i);
					}
					if (this.m_parameters == null)
					{
						this.m_parameters = array2;
					}
				}
				return this.m_parameters;
			}

			// Token: 0x06007068 RID: 28776 RVA: 0x00183351 File Offset: 0x00181551
			private ICustomAttributeProvider GetEmptyCAHolder()
			{
				return new DynamicMethod.RTDynamicMethod.EmptyCAHolder();
			}

			// Token: 0x04003755 RID: 14165
			internal DynamicMethod m_owner;

			// Token: 0x04003756 RID: 14166
			private ParameterInfo[] m_parameters;

			// Token: 0x04003757 RID: 14167
			private string m_name;

			// Token: 0x04003758 RID: 14168
			private MethodAttributes m_attributes;

			// Token: 0x04003759 RID: 14169
			private CallingConventions m_callingConvention;

			// Token: 0x02000D10 RID: 3344
			private class EmptyCAHolder : ICustomAttributeProvider
			{
				// Token: 0x06007228 RID: 29224 RVA: 0x001897E3 File Offset: 0x001879E3
				internal EmptyCAHolder()
				{
				}

				// Token: 0x06007229 RID: 29225 RVA: 0x001897EB File Offset: 0x001879EB
				object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit)
				{
					return EmptyArray<object>.Value;
				}

				// Token: 0x0600722A RID: 29226 RVA: 0x001897F2 File Offset: 0x001879F2
				object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit)
				{
					return EmptyArray<object>.Value;
				}

				// Token: 0x0600722B RID: 29227 RVA: 0x001897F9 File Offset: 0x001879F9
				bool ICustomAttributeProvider.IsDefined(Type attributeType, bool inherit)
				{
					return false;
				}
			}
		}
	}
}
