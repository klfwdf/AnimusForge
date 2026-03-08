using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

namespace System.Reflection.Emit
{
	// Token: 0x02000651 RID: 1617
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_ModuleBuilder))]
	[ComVisible(true)]
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public class ModuleBuilder : Module, _ModuleBuilder
	{
		// Token: 0x06004C0C RID: 19468
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern IntPtr nCreateISymWriterForDynamicModule(Module module, string filename);

		// Token: 0x06004C0D RID: 19469 RVA: 0x00113294 File Offset: 0x00111494
		internal static string UnmangleTypeName(string typeName)
		{
			int num = typeName.Length - 1;
			for (;;)
			{
				num = typeName.LastIndexOf('+', num);
				if (num == -1)
				{
					break;
				}
				bool flag = true;
				int num2 = num;
				while (typeName[--num2] == '\\')
				{
					flag = !flag;
				}
				if (flag)
				{
					break;
				}
				num = num2;
			}
			return typeName.Substring(num + 1);
		}

		// Token: 0x17000BEF RID: 3055
		// (get) Token: 0x06004C0E RID: 19470 RVA: 0x001132E2 File Offset: 0x001114E2
		internal AssemblyBuilder ContainingAssemblyBuilder
		{
			get
			{
				return this.m_assemblyBuilder;
			}
		}

		// Token: 0x06004C0F RID: 19471 RVA: 0x001132EA File Offset: 0x001114EA
		internal ModuleBuilder(AssemblyBuilder assemblyBuilder, InternalModuleBuilder internalModuleBuilder)
		{
			this.m_internalModuleBuilder = internalModuleBuilder;
			this.m_assemblyBuilder = assemblyBuilder;
		}

		// Token: 0x06004C10 RID: 19472 RVA: 0x00113300 File Offset: 0x00111500
		internal void AddType(string name, Type type)
		{
			this.m_TypeBuilderDict.Add(name, type);
		}

		// Token: 0x06004C11 RID: 19473 RVA: 0x00113310 File Offset: 0x00111510
		internal void CheckTypeNameConflict(string strTypeName, Type enclosingType)
		{
			Type type = null;
			if (this.m_TypeBuilderDict.TryGetValue(strTypeName, out type) && type.DeclaringType == enclosingType)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateTypeName"));
			}
		}

		// Token: 0x06004C12 RID: 19474 RVA: 0x00113348 File Offset: 0x00111548
		private Type GetType(string strFormat, Type baseType)
		{
			if (strFormat == null || strFormat.Equals(string.Empty))
			{
				return baseType;
			}
			char[] bFormat = strFormat.ToCharArray();
			return SymbolType.FormCompoundType(bFormat, baseType, 0);
		}

		// Token: 0x06004C13 RID: 19475 RVA: 0x00113376 File Offset: 0x00111576
		internal void CheckContext(params Type[][] typess)
		{
			this.ContainingAssemblyBuilder.CheckContext(typess);
		}

		// Token: 0x06004C14 RID: 19476 RVA: 0x00113384 File Offset: 0x00111584
		internal void CheckContext(params Type[] types)
		{
			this.ContainingAssemblyBuilder.CheckContext(types);
		}

		// Token: 0x06004C15 RID: 19477
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetTypeRef(RuntimeModule module, string strFullName, RuntimeModule refedModule, string strRefedModuleFileName, int tkResolution);

		// Token: 0x06004C16 RID: 19478
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetMemberRef(RuntimeModule module, RuntimeModule refedModule, int tr, int defToken);

		// Token: 0x06004C17 RID: 19479 RVA: 0x00113392 File Offset: 0x00111592
		[SecurityCritical]
		private int GetMemberRef(Module refedModule, int tr, int defToken)
		{
			return ModuleBuilder.GetMemberRef(this.GetNativeHandle(), ModuleBuilder.GetRuntimeModuleFromModule(refedModule).GetNativeHandle(), tr, defToken);
		}

		// Token: 0x06004C18 RID: 19480
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetMemberRefFromSignature(RuntimeModule module, int tr, string methodName, byte[] signature, int length);

		// Token: 0x06004C19 RID: 19481 RVA: 0x001133AC File Offset: 0x001115AC
		[SecurityCritical]
		private int GetMemberRefFromSignature(int tr, string methodName, byte[] signature, int length)
		{
			return ModuleBuilder.GetMemberRefFromSignature(this.GetNativeHandle(), tr, methodName, signature, length);
		}

		// Token: 0x06004C1A RID: 19482
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetMemberRefOfMethodInfo(RuntimeModule module, int tr, IRuntimeMethodInfo method);

		// Token: 0x06004C1B RID: 19483 RVA: 0x001133C0 File Offset: 0x001115C0
		[SecurityCritical]
		private int GetMemberRefOfMethodInfo(int tr, RuntimeMethodInfo method)
		{
			if (this.ContainingAssemblyBuilder.ProfileAPICheck && (method.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", new object[] { method.FullName }));
			}
			return ModuleBuilder.GetMemberRefOfMethodInfo(this.GetNativeHandle(), tr, method);
		}

		// Token: 0x06004C1C RID: 19484 RVA: 0x00113414 File Offset: 0x00111614
		[SecurityCritical]
		private int GetMemberRefOfMethodInfo(int tr, RuntimeConstructorInfo method)
		{
			if (this.ContainingAssemblyBuilder.ProfileAPICheck && (method.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", new object[] { method.FullName }));
			}
			return ModuleBuilder.GetMemberRefOfMethodInfo(this.GetNativeHandle(), tr, method);
		}

		// Token: 0x06004C1D RID: 19485
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetMemberRefOfFieldInfo(RuntimeModule module, int tkType, RuntimeTypeHandle declaringType, int tkField);

		// Token: 0x06004C1E RID: 19486 RVA: 0x00113468 File Offset: 0x00111668
		[SecurityCritical]
		private int GetMemberRefOfFieldInfo(int tkType, RuntimeTypeHandle declaringType, RuntimeFieldInfo runtimeField)
		{
			if (this.ContainingAssemblyBuilder.ProfileAPICheck)
			{
				RtFieldInfo rtFieldInfo = runtimeField as RtFieldInfo;
				if (rtFieldInfo != null && (rtFieldInfo.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", new object[] { rtFieldInfo.FullName }));
				}
			}
			return ModuleBuilder.GetMemberRefOfFieldInfo(this.GetNativeHandle(), tkType, declaringType, runtimeField.MetadataToken);
		}

		// Token: 0x06004C1F RID: 19487
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetTokenFromTypeSpec(RuntimeModule pModule, byte[] signature, int length);

		// Token: 0x06004C20 RID: 19488 RVA: 0x001134CF File Offset: 0x001116CF
		[SecurityCritical]
		private int GetTokenFromTypeSpec(byte[] signature, int length)
		{
			return ModuleBuilder.GetTokenFromTypeSpec(this.GetNativeHandle(), signature, length);
		}

		// Token: 0x06004C21 RID: 19489
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetArrayMethodToken(RuntimeModule module, int tkTypeSpec, string methodName, byte[] signature, int sigLength);

		// Token: 0x06004C22 RID: 19490
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern int GetStringConstant(RuntimeModule module, string str, int length);

		// Token: 0x06004C23 RID: 19491
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void PreSavePEFile(RuntimeModule module, int portableExecutableKind, int imageFileMachine);

		// Token: 0x06004C24 RID: 19492
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void SavePEFile(RuntimeModule module, string fileName, int entryPoint, int isExe, bool isManifestFile);

		// Token: 0x06004C25 RID: 19493
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void AddResource(RuntimeModule module, string strName, byte[] resBytes, int resByteCount, int tkFile, int attribute, int portableExecutableKind, int imageFileMachine);

		// Token: 0x06004C26 RID: 19494
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void SetModuleName(RuntimeModule module, string strModuleName);

		// Token: 0x06004C27 RID: 19495
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void SetFieldRVAContent(RuntimeModule module, int fdToken, byte[] data, int length);

		// Token: 0x06004C28 RID: 19496
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void DefineNativeResourceFile(RuntimeModule module, string strFilename, int portableExecutableKind, int ImageFileMachine);

		// Token: 0x06004C29 RID: 19497
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void DefineNativeResourceBytes(RuntimeModule module, byte[] pbResource, int cbResource, int portableExecutableKind, int imageFileMachine);

		// Token: 0x06004C2A RID: 19498 RVA: 0x001134E0 File Offset: 0x001116E0
		[SecurityCritical]
		internal void DefineNativeResource(PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
		{
			string strResourceFileName = this.m_moduleData.m_strResourceFileName;
			byte[] resourceBytes = this.m_moduleData.m_resourceBytes;
			if (strResourceFileName != null)
			{
				ModuleBuilder.DefineNativeResourceFile(this.GetNativeHandle(), strResourceFileName, (int)portableExecutableKind, (int)imageFileMachine);
				return;
			}
			if (resourceBytes != null)
			{
				ModuleBuilder.DefineNativeResourceBytes(this.GetNativeHandle(), resourceBytes, resourceBytes.Length, (int)portableExecutableKind, (int)imageFileMachine);
			}
		}

		// Token: 0x06004C2B RID: 19499 RVA: 0x0011352C File Offset: 0x0011172C
		internal virtual Type FindTypeBuilderWithName(string strTypeName, bool ignoreCase)
		{
			if (ignoreCase)
			{
				using (Dictionary<string, Type>.KeyCollection.Enumerator enumerator = this.m_TypeBuilderDict.Keys.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						string text = enumerator.Current;
						if (string.Compare(text, strTypeName, StringComparison.OrdinalIgnoreCase) == 0)
						{
							return this.m_TypeBuilderDict[text];
						}
					}
					goto IL_62;
				}
			}
			Type result;
			if (this.m_TypeBuilderDict.TryGetValue(strTypeName, out result))
			{
				return result;
			}
			IL_62:
			return null;
		}

		// Token: 0x06004C2C RID: 19500 RVA: 0x001135B0 File Offset: 0x001117B0
		internal void SetEntryPoint(MethodToken entryPoint)
		{
			this.m_EntryPoint = entryPoint;
		}

		// Token: 0x06004C2D RID: 19501 RVA: 0x001135BC File Offset: 0x001117BC
		[SecurityCritical]
		internal void PreSave(string fileName, PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
		{
			if (this.m_moduleData.m_isSaved)
			{
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("InvalidOperation_ModuleHasBeenSaved"), this.m_moduleData.m_strModuleName));
			}
			if (!this.m_moduleData.m_fGlobalBeenCreated && this.m_moduleData.m_fHasGlobal)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_GlobalFunctionNotBaked"));
			}
			foreach (Type type in this.m_TypeBuilderDict.Values)
			{
				TypeBuilder typeBuilder;
				if (type is TypeBuilder)
				{
					typeBuilder = (TypeBuilder)type;
				}
				else
				{
					EnumBuilder enumBuilder = (EnumBuilder)type;
					typeBuilder = enumBuilder.m_typeBuilder;
				}
				if (!typeBuilder.IsCreated())
				{
					throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("NotSupported_NotAllTypesAreBaked"), typeBuilder.FullName));
				}
			}
			ModuleBuilder.PreSavePEFile(this.GetNativeHandle(), (int)portableExecutableKind, (int)imageFileMachine);
		}

		// Token: 0x06004C2E RID: 19502 RVA: 0x001136C0 File Offset: 0x001118C0
		[SecurityCritical]
		internal void Save(string fileName, bool isAssemblyFile, PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
		{
			if (this.m_moduleData.m_embeddedRes != null)
			{
				for (ResWriterData resWriterData = this.m_moduleData.m_embeddedRes; resWriterData != null; resWriterData = resWriterData.m_nextResWriter)
				{
					if (resWriterData.m_resWriter != null)
					{
						resWriterData.m_resWriter.Generate();
					}
					byte[] array = new byte[resWriterData.m_memoryStream.Length];
					resWriterData.m_memoryStream.Flush();
					resWriterData.m_memoryStream.Position = 0L;
					resWriterData.m_memoryStream.Read(array, 0, array.Length);
					ModuleBuilder.AddResource(this.GetNativeHandle(), resWriterData.m_strName, array, array.Length, this.m_moduleData.FileToken, (int)resWriterData.m_attribute, (int)portableExecutableKind, (int)imageFileMachine);
				}
			}
			this.DefineNativeResource(portableExecutableKind, imageFileMachine);
			PEFileKinds isExe = (isAssemblyFile ? this.ContainingAssemblyBuilder.m_assemblyData.m_peFileKind : PEFileKinds.Dll);
			ModuleBuilder.SavePEFile(this.GetNativeHandle(), fileName, this.m_EntryPoint.Token, (int)isExe, isAssemblyFile);
			this.m_moduleData.m_isSaved = true;
		}

		// Token: 0x06004C2F RID: 19503 RVA: 0x001137B4 File Offset: 0x001119B4
		[SecurityCritical]
		private int GetTypeRefNested(Type type, Module refedModule, string strRefedModuleFileName)
		{
			Type declaringType = type.DeclaringType;
			int tkResolution = 0;
			string text = type.FullName;
			if (declaringType != null)
			{
				tkResolution = this.GetTypeRefNested(declaringType, refedModule, strRefedModuleFileName);
				text = ModuleBuilder.UnmangleTypeName(text);
			}
			if (this.ContainingAssemblyBuilder.ProfileAPICheck)
			{
				RuntimeType runtimeType = type as RuntimeType;
				if (runtimeType != null && (runtimeType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", new object[] { runtimeType.FullName }));
				}
			}
			return ModuleBuilder.GetTypeRef(this.GetNativeHandle(), text, ModuleBuilder.GetRuntimeModuleFromModule(refedModule).GetNativeHandle(), strRefedModuleFileName, tkResolution);
		}

		// Token: 0x06004C30 RID: 19504 RVA: 0x0011384C File Offset: 0x00111A4C
		[SecurityCritical]
		internal MethodToken InternalGetConstructorToken(ConstructorInfo con, bool usingRef)
		{
			if (con == null)
			{
				throw new ArgumentNullException("con");
			}
			ConstructorBuilder constructorBuilder;
			int str;
			ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation;
			RuntimeConstructorInfo method;
			if ((constructorBuilder = con as ConstructorBuilder) != null)
			{
				if (!usingRef && constructorBuilder.Module.Equals(this))
				{
					return constructorBuilder.GetToken();
				}
				int token = this.GetTypeTokenInternal(con.ReflectedType).Token;
				str = this.GetMemberRef(con.ReflectedType.Module, token, constructorBuilder.GetToken().Token);
			}
			else if ((constructorOnTypeBuilderInstantiation = con as ConstructorOnTypeBuilderInstantiation) != null)
			{
				if (usingRef)
				{
					throw new InvalidOperationException();
				}
				int token = this.GetTypeTokenInternal(con.DeclaringType).Token;
				str = this.GetMemberRef(con.DeclaringType.Module, token, constructorOnTypeBuilderInstantiation.MetadataTokenInternal);
			}
			else if ((method = con as RuntimeConstructorInfo) != null && !con.ReflectedType.IsArray)
			{
				int token = this.GetTypeTokenInternal(con.ReflectedType).Token;
				str = this.GetMemberRefOfMethodInfo(token, method);
			}
			else
			{
				ParameterInfo[] parameters = con.GetParameters();
				if (parameters == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidConstructorInfo"));
				}
				int num = parameters.Length;
				Type[] array = new Type[num];
				Type[][] array2 = new Type[num][];
				Type[][] array3 = new Type[num][];
				for (int i = 0; i < num; i++)
				{
					if (parameters[i] == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_InvalidConstructorInfo"));
					}
					array[i] = parameters[i].ParameterType;
					array2[i] = parameters[i].GetRequiredCustomModifiers();
					array3[i] = parameters[i].GetOptionalCustomModifiers();
				}
				int token = this.GetTypeTokenInternal(con.ReflectedType).Token;
				SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, con.CallingConvention, null, null, null, array, array2, array3);
				int length;
				byte[] signature = methodSigHelper.InternalGetSignature(out length);
				str = this.GetMemberRefFromSignature(token, con.Name, signature, length);
			}
			return new MethodToken(str);
		}

		// Token: 0x06004C31 RID: 19505 RVA: 0x00113A4D File Offset: 0x00111C4D
		[SecurityCritical]
		internal void Init(string strModuleName, string strFileName, int tkFile)
		{
			this.m_moduleData = new ModuleBuilderData(this, strModuleName, strFileName, tkFile);
			this.m_TypeBuilderDict = new Dictionary<string, Type>();
		}

		// Token: 0x06004C32 RID: 19506 RVA: 0x00113A69 File Offset: 0x00111C69
		[SecurityCritical]
		internal void ModifyModuleName(string name)
		{
			this.m_moduleData.ModifyModuleName(name);
			ModuleBuilder.SetModuleName(this.GetNativeHandle(), name);
		}

		// Token: 0x06004C33 RID: 19507 RVA: 0x00113A83 File Offset: 0x00111C83
		internal void SetSymWriter(ISymbolWriter writer)
		{
			this.m_iSymWriter = writer;
		}

		// Token: 0x17000BF0 RID: 3056
		// (get) Token: 0x06004C34 RID: 19508 RVA: 0x00113A8C File Offset: 0x00111C8C
		internal object SyncRoot
		{
			get
			{
				return this.ContainingAssemblyBuilder.SyncRoot;
			}
		}

		// Token: 0x17000BF1 RID: 3057
		// (get) Token: 0x06004C35 RID: 19509 RVA: 0x00113A99 File Offset: 0x00111C99
		internal InternalModuleBuilder InternalModule
		{
			get
			{
				return this.m_internalModuleBuilder;
			}
		}

		// Token: 0x06004C36 RID: 19510 RVA: 0x00113AA1 File Offset: 0x00111CA1
		internal override ModuleHandle GetModuleHandle()
		{
			return new ModuleHandle(this.GetNativeHandle());
		}

		// Token: 0x06004C37 RID: 19511 RVA: 0x00113AAE File Offset: 0x00111CAE
		internal RuntimeModule GetNativeHandle()
		{
			return this.InternalModule.GetNativeHandle();
		}

		// Token: 0x06004C38 RID: 19512 RVA: 0x00113ABC File Offset: 0x00111CBC
		private static RuntimeModule GetRuntimeModuleFromModule(Module m)
		{
			ModuleBuilder moduleBuilder = m as ModuleBuilder;
			if (moduleBuilder != null)
			{
				return moduleBuilder.InternalModule;
			}
			return m as RuntimeModule;
		}

		// Token: 0x06004C39 RID: 19513 RVA: 0x00113AE8 File Offset: 0x00111CE8
		[SecurityCritical]
		private int GetMemberRefToken(MethodBase method, IEnumerable<Type> optionalParameterTypes)
		{
			int cGenericParameters = 0;
			if (method.IsGenericMethod)
			{
				if (!method.IsGenericMethodDefinition)
				{
					throw new InvalidOperationException();
				}
				cGenericParameters = method.GetGenericArguments().Length;
			}
			if (optionalParameterTypes != null && (method.CallingConvention & CallingConventions.VarArgs) == (CallingConventions)0)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAVarArgCallingConvention"));
			}
			MethodInfo methodInfo = method as MethodInfo;
			Type[] parameterTypes;
			Type methodBaseReturnType;
			if (method.DeclaringType.IsGenericType)
			{
				MethodOnTypeBuilderInstantiation methodOnTypeBuilderInstantiation;
				MethodBase methodBase;
				ConstructorOnTypeBuilderInstantiation constructorOnTypeBuilderInstantiation;
				if ((methodOnTypeBuilderInstantiation = method as MethodOnTypeBuilderInstantiation) != null)
				{
					methodBase = methodOnTypeBuilderInstantiation.m_method;
				}
				else if ((constructorOnTypeBuilderInstantiation = method as ConstructorOnTypeBuilderInstantiation) != null)
				{
					methodBase = constructorOnTypeBuilderInstantiation.m_ctor;
				}
				else if (method is MethodBuilder || method is ConstructorBuilder)
				{
					methodBase = method;
				}
				else if (method.IsGenericMethod)
				{
					methodBase = methodInfo.GetGenericMethodDefinition();
					methodBase = methodBase.Module.ResolveMethod(method.MetadataToken, (methodBase.DeclaringType != null) ? methodBase.DeclaringType.GetGenericArguments() : null, methodBase.GetGenericArguments());
				}
				else
				{
					methodBase = method.Module.ResolveMethod(method.MetadataToken, (method.DeclaringType != null) ? method.DeclaringType.GetGenericArguments() : null, null);
				}
				parameterTypes = methodBase.GetParameterTypes();
				methodBaseReturnType = MethodBuilder.GetMethodBaseReturnType(methodBase);
			}
			else
			{
				parameterTypes = method.GetParameterTypes();
				methodBaseReturnType = MethodBuilder.GetMethodBaseReturnType(method);
			}
			int length;
			byte[] signature = this.GetMemberRefSignature(method.CallingConvention, methodBaseReturnType, parameterTypes, optionalParameterTypes, cGenericParameters).InternalGetSignature(out length);
			int tr;
			if (method.DeclaringType.IsGenericType)
			{
				int length2;
				byte[] signature2 = SignatureHelper.GetTypeSigToken(this, method.DeclaringType).InternalGetSignature(out length2);
				tr = this.GetTokenFromTypeSpec(signature2, length2);
			}
			else if (!method.Module.Equals(this))
			{
				tr = this.GetTypeToken(method.DeclaringType).Token;
			}
			else if (methodInfo != null)
			{
				tr = this.GetMethodToken(methodInfo).Token;
			}
			else
			{
				tr = this.GetConstructorToken(method as ConstructorInfo).Token;
			}
			return this.GetMemberRefFromSignature(tr, method.Name, signature, length);
		}

		// Token: 0x06004C3A RID: 19514 RVA: 0x00113CF0 File Offset: 0x00111EF0
		[SecurityCritical]
		internal SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, IEnumerable<Type> optionalParameterTypes, int cGenericParameters)
		{
			int num = ((parameterTypes == null) ? 0 : parameterTypes.Length);
			SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, call, returnType, cGenericParameters);
			for (int i = 0; i < num; i++)
			{
				methodSigHelper.AddArgument(parameterTypes[i]);
			}
			if (optionalParameterTypes != null)
			{
				int num2 = 0;
				foreach (Type clsArgument in optionalParameterTypes)
				{
					if (num2 == 0)
					{
						methodSigHelper.AddSentinel();
					}
					methodSigHelper.AddArgument(clsArgument);
					num2++;
				}
			}
			return methodSigHelper;
		}

		// Token: 0x06004C3B RID: 19515 RVA: 0x00113D80 File Offset: 0x00111F80
		public override bool Equals(object obj)
		{
			return this.InternalModule.Equals(obj);
		}

		// Token: 0x06004C3C RID: 19516 RVA: 0x00113D8E File Offset: 0x00111F8E
		public override int GetHashCode()
		{
			return this.InternalModule.GetHashCode();
		}

		// Token: 0x06004C3D RID: 19517 RVA: 0x00113D9B File Offset: 0x00111F9B
		public override object[] GetCustomAttributes(bool inherit)
		{
			return this.InternalModule.GetCustomAttributes(inherit);
		}

		// Token: 0x06004C3E RID: 19518 RVA: 0x00113DA9 File Offset: 0x00111FA9
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			return this.InternalModule.GetCustomAttributes(attributeType, inherit);
		}

		// Token: 0x06004C3F RID: 19519 RVA: 0x00113DB8 File Offset: 0x00111FB8
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return this.InternalModule.IsDefined(attributeType, inherit);
		}

		// Token: 0x06004C40 RID: 19520 RVA: 0x00113DC7 File Offset: 0x00111FC7
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return this.InternalModule.GetCustomAttributesData();
		}

		// Token: 0x06004C41 RID: 19521 RVA: 0x00113DD4 File Offset: 0x00111FD4
		public override Type[] GetTypes()
		{
			object syncRoot = this.SyncRoot;
			Type[] typesNoLock;
			lock (syncRoot)
			{
				typesNoLock = this.GetTypesNoLock();
			}
			return typesNoLock;
		}

		// Token: 0x06004C42 RID: 19522 RVA: 0x00113E18 File Offset: 0x00112018
		internal Type[] GetTypesNoLock()
		{
			int count = this.m_TypeBuilderDict.Count;
			Type[] array = new Type[this.m_TypeBuilderDict.Count];
			int num = 0;
			foreach (Type type in this.m_TypeBuilderDict.Values)
			{
				EnumBuilder enumBuilder = type as EnumBuilder;
				TypeBuilder typeBuilder;
				if (enumBuilder != null)
				{
					typeBuilder = enumBuilder.m_typeBuilder;
				}
				else
				{
					typeBuilder = (TypeBuilder)type;
				}
				if (typeBuilder.IsCreated())
				{
					array[num++] = typeBuilder.UnderlyingSystemType;
				}
				else
				{
					array[num++] = type;
				}
			}
			return array;
		}

		// Token: 0x06004C43 RID: 19523 RVA: 0x00113ED4 File Offset: 0x001120D4
		[ComVisible(true)]
		public override Type GetType(string className)
		{
			return this.GetType(className, false, false);
		}

		// Token: 0x06004C44 RID: 19524 RVA: 0x00113EDF File Offset: 0x001120DF
		[ComVisible(true)]
		public override Type GetType(string className, bool ignoreCase)
		{
			return this.GetType(className, false, ignoreCase);
		}

		// Token: 0x06004C45 RID: 19525 RVA: 0x00113EEC File Offset: 0x001120EC
		[ComVisible(true)]
		public override Type GetType(string className, bool throwOnError, bool ignoreCase)
		{
			object syncRoot = this.SyncRoot;
			Type typeNoLock;
			lock (syncRoot)
			{
				typeNoLock = this.GetTypeNoLock(className, throwOnError, ignoreCase);
			}
			return typeNoLock;
		}

		// Token: 0x06004C46 RID: 19526 RVA: 0x00113F34 File Offset: 0x00112134
		private Type GetTypeNoLock(string className, bool throwOnError, bool ignoreCase)
		{
			Type type = this.InternalModule.GetType(className, throwOnError, ignoreCase);
			if (type != null)
			{
				return type;
			}
			string text = null;
			string text2 = null;
			int num;
			for (int i = 0; i <= className.Length; i = num + 1)
			{
				num = className.IndexOfAny(new char[] { '[', '*', '&' }, i);
				if (num == -1)
				{
					text = className;
					text2 = null;
					break;
				}
				int num2 = 0;
				int num3 = num - 1;
				while (num3 >= 0 && className[num3] == '\\')
				{
					num2++;
					num3--;
				}
				if (num2 % 2 != 1)
				{
					text = className.Substring(0, num);
					text2 = className.Substring(num);
					break;
				}
			}
			if (text == null)
			{
				text = className;
				text2 = null;
			}
			text = text.Replace("\\\\", "\\").Replace("\\[", "[").Replace("\\*", "*")
				.Replace("\\&", "&");
			if (text2 != null)
			{
				type = this.InternalModule.GetType(text, false, ignoreCase);
			}
			if (type == null)
			{
				type = this.FindTypeBuilderWithName(text, ignoreCase);
				if (type == null && this.Assembly is AssemblyBuilder)
				{
					List<ModuleBuilder> moduleBuilderList = this.ContainingAssemblyBuilder.m_assemblyData.m_moduleBuilderList;
					int count = moduleBuilderList.Count;
					int num4 = 0;
					while (num4 < count && type == null)
					{
						ModuleBuilder moduleBuilder = moduleBuilderList[num4];
						type = moduleBuilder.FindTypeBuilderWithName(text, ignoreCase);
						num4++;
					}
				}
				if (type == null)
				{
					return null;
				}
			}
			if (text2 == null)
			{
				return type;
			}
			return this.GetType(text2, type);
		}

		// Token: 0x17000BF2 RID: 3058
		// (get) Token: 0x06004C47 RID: 19527 RVA: 0x001140C0 File Offset: 0x001122C0
		public override string FullyQualifiedName
		{
			[SecuritySafeCritical]
			get
			{
				string text = this.m_moduleData.m_strFileName;
				if (text == null)
				{
					return null;
				}
				if (this.ContainingAssemblyBuilder.m_assemblyData.m_strDir != null)
				{
					text = Path.Combine(this.ContainingAssemblyBuilder.m_assemblyData.m_strDir, text);
					text = Path.UnsafeGetFullPath(text);
				}
				if (this.ContainingAssemblyBuilder.m_assemblyData.m_strDir != null && text != null)
				{
					new FileIOPermission(FileIOPermissionAccess.PathDiscovery, text).Demand();
				}
				return text;
			}
		}

		// Token: 0x06004C48 RID: 19528 RVA: 0x00114130 File Offset: 0x00112330
		public override byte[] ResolveSignature(int metadataToken)
		{
			return this.InternalModule.ResolveSignature(metadataToken);
		}

		// Token: 0x06004C49 RID: 19529 RVA: 0x0011413E File Offset: 0x0011233E
		public override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			return this.InternalModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
		}

		// Token: 0x06004C4A RID: 19530 RVA: 0x0011414E File Offset: 0x0011234E
		public override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			return this.InternalModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
		}

		// Token: 0x06004C4B RID: 19531 RVA: 0x0011415E File Offset: 0x0011235E
		public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			return this.InternalModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
		}

		// Token: 0x06004C4C RID: 19532 RVA: 0x0011416E File Offset: 0x0011236E
		public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			return this.InternalModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
		}

		// Token: 0x06004C4D RID: 19533 RVA: 0x0011417E File Offset: 0x0011237E
		public override string ResolveString(int metadataToken)
		{
			return this.InternalModule.ResolveString(metadataToken);
		}

		// Token: 0x06004C4E RID: 19534 RVA: 0x0011418C File Offset: 0x0011238C
		public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
		{
			this.InternalModule.GetPEKind(out peKind, out machine);
		}

		// Token: 0x17000BF3 RID: 3059
		// (get) Token: 0x06004C4F RID: 19535 RVA: 0x0011419B File Offset: 0x0011239B
		public override int MDStreamVersion
		{
			get
			{
				return this.InternalModule.MDStreamVersion;
			}
		}

		// Token: 0x17000BF4 RID: 3060
		// (get) Token: 0x06004C50 RID: 19536 RVA: 0x001141A8 File Offset: 0x001123A8
		public override Guid ModuleVersionId
		{
			get
			{
				return this.InternalModule.ModuleVersionId;
			}
		}

		// Token: 0x17000BF5 RID: 3061
		// (get) Token: 0x06004C51 RID: 19537 RVA: 0x001141B5 File Offset: 0x001123B5
		public override int MetadataToken
		{
			get
			{
				return this.InternalModule.MetadataToken;
			}
		}

		// Token: 0x06004C52 RID: 19538 RVA: 0x001141C2 File Offset: 0x001123C2
		public override bool IsResource()
		{
			return this.InternalModule.IsResource();
		}

		// Token: 0x06004C53 RID: 19539 RVA: 0x001141CF File Offset: 0x001123CF
		public override FieldInfo[] GetFields(BindingFlags bindingFlags)
		{
			return this.InternalModule.GetFields(bindingFlags);
		}

		// Token: 0x06004C54 RID: 19540 RVA: 0x001141DD File Offset: 0x001123DD
		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			return this.InternalModule.GetField(name, bindingAttr);
		}

		// Token: 0x06004C55 RID: 19541 RVA: 0x001141EC File Offset: 0x001123EC
		public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
		{
			return this.InternalModule.GetMethods(bindingFlags);
		}

		// Token: 0x06004C56 RID: 19542 RVA: 0x001141FA File Offset: 0x001123FA
		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			return this.InternalModule.GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
		}

		// Token: 0x17000BF6 RID: 3062
		// (get) Token: 0x06004C57 RID: 19543 RVA: 0x00114210 File Offset: 0x00112410
		public override string ScopeName
		{
			get
			{
				return this.InternalModule.ScopeName;
			}
		}

		// Token: 0x17000BF7 RID: 3063
		// (get) Token: 0x06004C58 RID: 19544 RVA: 0x0011421D File Offset: 0x0011241D
		public override string Name
		{
			get
			{
				return this.InternalModule.Name;
			}
		}

		// Token: 0x17000BF8 RID: 3064
		// (get) Token: 0x06004C59 RID: 19545 RVA: 0x0011422A File Offset: 0x0011242A
		public override Assembly Assembly
		{
			get
			{
				return this.m_assemblyBuilder;
			}
		}

		// Token: 0x06004C5A RID: 19546 RVA: 0x00114232 File Offset: 0x00112432
		public override X509Certificate GetSignerCertificate()
		{
			return this.InternalModule.GetSignerCertificate();
		}

		// Token: 0x06004C5B RID: 19547 RVA: 0x00114240 File Offset: 0x00112440
		[SecuritySafeCritical]
		public TypeBuilder DefineType(string name)
		{
			object syncRoot = this.SyncRoot;
			TypeBuilder result;
			lock (syncRoot)
			{
				result = this.DefineTypeNoLock(name, TypeAttributes.NotPublic, null, null, PackingSize.Unspecified, 0);
			}
			return result;
		}

		// Token: 0x06004C5C RID: 19548 RVA: 0x00114288 File Offset: 0x00112488
		[SecuritySafeCritical]
		public TypeBuilder DefineType(string name, TypeAttributes attr)
		{
			object syncRoot = this.SyncRoot;
			TypeBuilder result;
			lock (syncRoot)
			{
				result = this.DefineTypeNoLock(name, attr, null, null, PackingSize.Unspecified, 0);
			}
			return result;
		}

		// Token: 0x06004C5D RID: 19549 RVA: 0x001142D0 File Offset: 0x001124D0
		[SecuritySafeCritical]
		public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent)
		{
			object syncRoot = this.SyncRoot;
			TypeBuilder result;
			lock (syncRoot)
			{
				this.CheckContext(new Type[] { parent });
				result = this.DefineTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, 0);
			}
			return result;
		}

		// Token: 0x06004C5E RID: 19550 RVA: 0x00114328 File Offset: 0x00112528
		[SecuritySafeCritical]
		public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, int typesize)
		{
			object syncRoot = this.SyncRoot;
			TypeBuilder result;
			lock (syncRoot)
			{
				result = this.DefineTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, typesize);
			}
			return result;
		}

		// Token: 0x06004C5F RID: 19551 RVA: 0x00114374 File Offset: 0x00112574
		[SecuritySafeCritical]
		public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, PackingSize packingSize, int typesize)
		{
			object syncRoot = this.SyncRoot;
			TypeBuilder result;
			lock (syncRoot)
			{
				result = this.DefineTypeNoLock(name, attr, parent, null, packingSize, typesize);
			}
			return result;
		}

		// Token: 0x06004C60 RID: 19552 RVA: 0x001143C0 File Offset: 0x001125C0
		[SecuritySafeCritical]
		[ComVisible(true)]
		public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, Type[] interfaces)
		{
			object syncRoot = this.SyncRoot;
			TypeBuilder result;
			lock (syncRoot)
			{
				result = this.DefineTypeNoLock(name, attr, parent, interfaces, PackingSize.Unspecified, 0);
			}
			return result;
		}

		// Token: 0x06004C61 RID: 19553 RVA: 0x0011440C File Offset: 0x0011260C
		[SecurityCritical]
		private TypeBuilder DefineTypeNoLock(string name, TypeAttributes attr, Type parent, Type[] interfaces, PackingSize packingSize, int typesize)
		{
			return new TypeBuilder(name, attr, parent, interfaces, this, packingSize, typesize, null);
		}

		// Token: 0x06004C62 RID: 19554 RVA: 0x00114420 File Offset: 0x00112620
		[SecuritySafeCritical]
		public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, PackingSize packsize)
		{
			object syncRoot = this.SyncRoot;
			TypeBuilder result;
			lock (syncRoot)
			{
				result = this.DefineTypeNoLock(name, attr, parent, packsize);
			}
			return result;
		}

		// Token: 0x06004C63 RID: 19555 RVA: 0x00114468 File Offset: 0x00112668
		[SecurityCritical]
		private TypeBuilder DefineTypeNoLock(string name, TypeAttributes attr, Type parent, PackingSize packsize)
		{
			return new TypeBuilder(name, attr, parent, null, this, packsize, 0, null);
		}

		// Token: 0x06004C64 RID: 19556 RVA: 0x00114478 File Offset: 0x00112678
		[SecuritySafeCritical]
		public EnumBuilder DefineEnum(string name, TypeAttributes visibility, Type underlyingType)
		{
			this.CheckContext(new Type[] { underlyingType });
			object syncRoot = this.SyncRoot;
			EnumBuilder result;
			lock (syncRoot)
			{
				EnumBuilder enumBuilder = this.DefineEnumNoLock(name, visibility, underlyingType);
				this.m_TypeBuilderDict[name] = enumBuilder;
				result = enumBuilder;
			}
			return result;
		}

		// Token: 0x06004C65 RID: 19557 RVA: 0x001144DC File Offset: 0x001126DC
		[SecurityCritical]
		private EnumBuilder DefineEnumNoLock(string name, TypeAttributes visibility, Type underlyingType)
		{
			return new EnumBuilder(name, underlyingType, visibility, this);
		}

		// Token: 0x06004C66 RID: 19558 RVA: 0x001144E7 File Offset: 0x001126E7
		public IResourceWriter DefineResource(string name, string description)
		{
			return this.DefineResource(name, description, ResourceAttributes.Public);
		}

		// Token: 0x06004C67 RID: 19559 RVA: 0x001144F4 File Offset: 0x001126F4
		public IResourceWriter DefineResource(string name, string description, ResourceAttributes attribute)
		{
			object syncRoot = this.SyncRoot;
			IResourceWriter result;
			lock (syncRoot)
			{
				result = this.DefineResourceNoLock(name, description, attribute);
			}
			return result;
		}

		// Token: 0x06004C68 RID: 19560 RVA: 0x0011453C File Offset: 0x0011273C
		private IResourceWriter DefineResourceNoLock(string name, string description, ResourceAttributes attribute)
		{
			if (this.IsTransient())
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));
			}
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
			}
			if (this.m_assemblyBuilder.IsPersistable())
			{
				this.m_assemblyBuilder.m_assemblyData.CheckResNameConflict(name);
				MemoryStream memoryStream = new MemoryStream();
				ResourceWriter resourceWriter = new ResourceWriter(memoryStream);
				ResWriterData resWriterData = new ResWriterData(resourceWriter, memoryStream, name, string.Empty, string.Empty, attribute);
				resWriterData.m_nextResWriter = this.m_moduleData.m_embeddedRes;
				this.m_moduleData.m_embeddedRes = resWriterData;
				return resourceWriter;
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));
		}

		// Token: 0x06004C69 RID: 19561 RVA: 0x001145FC File Offset: 0x001127FC
		public void DefineManifestResource(string name, Stream stream, ResourceAttributes attribute)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			object syncRoot = this.SyncRoot;
			lock (syncRoot)
			{
				this.DefineManifestResourceNoLock(name, stream, attribute);
			}
		}

		// Token: 0x06004C6A RID: 19562 RVA: 0x0011465C File Offset: 0x0011285C
		private void DefineManifestResourceNoLock(string name, Stream stream, ResourceAttributes attribute)
		{
			if (this.IsTransient())
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));
			}
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
			}
			if (this.m_assemblyBuilder.IsPersistable())
			{
				this.m_assemblyBuilder.m_assemblyData.CheckResNameConflict(name);
				ResWriterData resWriterData = new ResWriterData(null, stream, name, string.Empty, string.Empty, attribute);
				resWriterData.m_nextResWriter = this.m_moduleData.m_embeddedRes;
				this.m_moduleData.m_embeddedRes = resWriterData;
				return;
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));
		}

		// Token: 0x06004C6B RID: 19563 RVA: 0x0011470C File Offset: 0x0011290C
		public void DefineUnmanagedResource(byte[] resource)
		{
			object syncRoot = this.SyncRoot;
			lock (syncRoot)
			{
				this.DefineUnmanagedResourceInternalNoLock(resource);
			}
		}

		// Token: 0x06004C6C RID: 19564 RVA: 0x00114750 File Offset: 0x00112950
		internal void DefineUnmanagedResourceInternalNoLock(byte[] resource)
		{
			if (resource == null)
			{
				throw new ArgumentNullException("resource");
			}
			if (this.m_moduleData.m_strResourceFileName != null || this.m_moduleData.m_resourceBytes != null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
			}
			this.m_moduleData.m_resourceBytes = new byte[resource.Length];
			Array.Copy(resource, this.m_moduleData.m_resourceBytes, resource.Length);
		}

		// Token: 0x06004C6D RID: 19565 RVA: 0x001147BC File Offset: 0x001129BC
		[SecuritySafeCritical]
		public void DefineUnmanagedResource(string resourceFileName)
		{
			object syncRoot = this.SyncRoot;
			lock (syncRoot)
			{
				this.DefineUnmanagedResourceFileInternalNoLock(resourceFileName);
			}
		}

		// Token: 0x06004C6E RID: 19566 RVA: 0x00114800 File Offset: 0x00112A00
		[SecurityCritical]
		internal void DefineUnmanagedResourceFileInternalNoLock(string resourceFileName)
		{
			if (resourceFileName == null)
			{
				throw new ArgumentNullException("resourceFileName");
			}
			if (this.m_moduleData.m_resourceBytes != null || this.m_moduleData.m_strResourceFileName != null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
			}
			string text = Path.UnsafeGetFullPath(resourceFileName);
			new FileIOPermission(FileIOPermissionAccess.Read, text).Demand();
			new EnvironmentPermission(PermissionState.Unrestricted).Assert();
			try
			{
				if (!File.UnsafeExists(resourceFileName))
				{
					throw new FileNotFoundException(Environment.GetResourceString("IO.FileNotFound_FileName", new object[] { resourceFileName }), resourceFileName);
				}
			}
			finally
			{
				CodeAccessPermission.RevertAssert();
			}
			this.m_moduleData.m_strResourceFileName = text;
		}

		// Token: 0x06004C6F RID: 19567 RVA: 0x001148AC File Offset: 0x00112AAC
		public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
		{
			return this.DefineGlobalMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
		}

		// Token: 0x06004C70 RID: 19568 RVA: 0x001148BC File Offset: 0x00112ABC
		public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
		{
			return this.DefineGlobalMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
		}

		// Token: 0x06004C71 RID: 19569 RVA: 0x001148DC File Offset: 0x00112ADC
		public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
		{
			object syncRoot = this.SyncRoot;
			MethodBuilder result;
			lock (syncRoot)
			{
				result = this.DefineGlobalMethodNoLock(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
			}
			return result;
		}

		// Token: 0x06004C72 RID: 19570 RVA: 0x00114930 File Offset: 0x00112B30
		private MethodBuilder DefineGlobalMethodNoLock(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
		{
			if (this.m_moduleData.m_fGlobalBeenCreated)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
			}
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
			}
			if ((attributes & MethodAttributes.Static) == MethodAttributes.PrivateScope)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_GlobalFunctionHasToBeStatic"));
			}
			this.CheckContext(new Type[] { returnType });
			this.CheckContext(new Type[][] { requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes });
			this.CheckContext(requiredParameterTypeCustomModifiers);
			this.CheckContext(optionalParameterTypeCustomModifiers);
			this.m_moduleData.m_fHasGlobal = true;
			return this.m_moduleData.m_globalTypeBuilder.DefineMethod(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
		}

		// Token: 0x06004C73 RID: 19571 RVA: 0x00114A04 File Offset: 0x00112C04
		public MethodBuilder DefinePInvokeMethod(string name, string dllName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
		{
			return this.DefinePInvokeMethod(name, dllName, name, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
		}

		// Token: 0x06004C74 RID: 19572 RVA: 0x00114A28 File Offset: 0x00112C28
		public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
		{
			object syncRoot = this.SyncRoot;
			MethodBuilder result;
			lock (syncRoot)
			{
				result = this.DefinePInvokeMethodNoLock(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
			}
			return result;
		}

		// Token: 0x06004C75 RID: 19573 RVA: 0x00114A7C File Offset: 0x00112C7C
		private MethodBuilder DefinePInvokeMethodNoLock(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
		{
			if ((attributes & MethodAttributes.Static) == MethodAttributes.PrivateScope)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_GlobalFunctionHasToBeStatic"));
			}
			this.CheckContext(new Type[] { returnType });
			this.CheckContext(parameterTypes);
			this.m_moduleData.m_fHasGlobal = true;
			return this.m_moduleData.m_globalTypeBuilder.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
		}

		// Token: 0x06004C76 RID: 19574 RVA: 0x00114AE4 File Offset: 0x00112CE4
		public void CreateGlobalFunctions()
		{
			object syncRoot = this.SyncRoot;
			lock (syncRoot)
			{
				this.CreateGlobalFunctionsNoLock();
			}
		}

		// Token: 0x06004C77 RID: 19575 RVA: 0x00114B24 File Offset: 0x00112D24
		private void CreateGlobalFunctionsNoLock()
		{
			if (this.m_moduleData.m_fGlobalBeenCreated)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
			}
			this.m_moduleData.m_globalTypeBuilder.CreateType();
			this.m_moduleData.m_fGlobalBeenCreated = true;
		}

		// Token: 0x06004C78 RID: 19576 RVA: 0x00114B60 File Offset: 0x00112D60
		public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
		{
			object syncRoot = this.SyncRoot;
			FieldBuilder result;
			lock (syncRoot)
			{
				result = this.DefineInitializedDataNoLock(name, data, attributes);
			}
			return result;
		}

		// Token: 0x06004C79 RID: 19577 RVA: 0x00114BA8 File Offset: 0x00112DA8
		private FieldBuilder DefineInitializedDataNoLock(string name, byte[] data, FieldAttributes attributes)
		{
			if (this.m_moduleData.m_fGlobalBeenCreated)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
			}
			this.m_moduleData.m_fHasGlobal = true;
			return this.m_moduleData.m_globalTypeBuilder.DefineInitializedData(name, data, attributes);
		}

		// Token: 0x06004C7A RID: 19578 RVA: 0x00114BE8 File Offset: 0x00112DE8
		public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
		{
			object syncRoot = this.SyncRoot;
			FieldBuilder result;
			lock (syncRoot)
			{
				result = this.DefineUninitializedDataNoLock(name, size, attributes);
			}
			return result;
		}

		// Token: 0x06004C7B RID: 19579 RVA: 0x00114C30 File Offset: 0x00112E30
		private FieldBuilder DefineUninitializedDataNoLock(string name, int size, FieldAttributes attributes)
		{
			if (this.m_moduleData.m_fGlobalBeenCreated)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
			}
			this.m_moduleData.m_fHasGlobal = true;
			return this.m_moduleData.m_globalTypeBuilder.DefineUninitializedData(name, size, attributes);
		}

		// Token: 0x06004C7C RID: 19580 RVA: 0x00114C6E File Offset: 0x00112E6E
		[SecurityCritical]
		internal TypeToken GetTypeTokenInternal(Type type)
		{
			return this.GetTypeTokenInternal(type, false);
		}

		// Token: 0x06004C7D RID: 19581 RVA: 0x00114C78 File Offset: 0x00112E78
		[SecurityCritical]
		private TypeToken GetTypeTokenInternal(Type type, bool getGenericDefinition)
		{
			object syncRoot = this.SyncRoot;
			TypeToken typeTokenWorkerNoLock;
			lock (syncRoot)
			{
				typeTokenWorkerNoLock = this.GetTypeTokenWorkerNoLock(type, getGenericDefinition);
			}
			return typeTokenWorkerNoLock;
		}

		// Token: 0x06004C7E RID: 19582 RVA: 0x00114CBC File Offset: 0x00112EBC
		[SecuritySafeCritical]
		public TypeToken GetTypeToken(Type type)
		{
			return this.GetTypeTokenInternal(type, true);
		}

		// Token: 0x06004C7F RID: 19583 RVA: 0x00114CC8 File Offset: 0x00112EC8
		[SecurityCritical]
		private TypeToken GetTypeTokenWorkerNoLock(Type type, bool getGenericDefinition)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			this.CheckContext(new Type[] { type });
			if (type.IsByRef)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_CannotGetTypeTokenForByRef"));
			}
			if ((type.IsGenericType && (!type.IsGenericTypeDefinition || !getGenericDefinition)) || type.IsGenericParameter || type.IsArray || type.IsPointer)
			{
				int length;
				byte[] signature = SignatureHelper.GetTypeSigToken(this, type).InternalGetSignature(out length);
				return new TypeToken(this.GetTokenFromTypeSpec(signature, length));
			}
			Module module = type.Module;
			if (module.Equals(this))
			{
				EnumBuilder enumBuilder = type as EnumBuilder;
				TypeBuilder typeBuilder;
				if (enumBuilder != null)
				{
					typeBuilder = enumBuilder.m_typeBuilder;
				}
				else
				{
					typeBuilder = type as TypeBuilder;
				}
				if (typeBuilder != null)
				{
					return typeBuilder.TypeToken;
				}
				GenericTypeParameterBuilder genericTypeParameterBuilder;
				if ((genericTypeParameterBuilder = type as GenericTypeParameterBuilder) != null)
				{
					return new TypeToken(genericTypeParameterBuilder.MetadataTokenInternal);
				}
				return new TypeToken(this.GetTypeRefNested(type, this, string.Empty));
			}
			else
			{
				ModuleBuilder moduleBuilder = module as ModuleBuilder;
				bool flag = ((moduleBuilder != null) ? moduleBuilder.IsTransient() : ((RuntimeModule)module).IsTransientInternal());
				if (!this.IsTransient() && flag)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadTransientModuleReference"));
				}
				string strRefedModuleFileName = string.Empty;
				if (module.Assembly.Equals(this.Assembly))
				{
					if (moduleBuilder == null)
					{
						moduleBuilder = this.ContainingAssemblyBuilder.GetModuleBuilder((InternalModuleBuilder)module);
					}
					strRefedModuleFileName = moduleBuilder.m_moduleData.m_strFileName;
				}
				return new TypeToken(this.GetTypeRefNested(type, module, strRefedModuleFileName));
			}
		}

		// Token: 0x06004C80 RID: 19584 RVA: 0x00114E69 File Offset: 0x00113069
		public TypeToken GetTypeToken(string name)
		{
			return this.GetTypeToken(this.InternalModule.GetType(name, false, true));
		}

		// Token: 0x06004C81 RID: 19585 RVA: 0x00114E80 File Offset: 0x00113080
		[SecuritySafeCritical]
		public MethodToken GetMethodToken(MethodInfo method)
		{
			object syncRoot = this.SyncRoot;
			MethodToken methodTokenNoLock;
			lock (syncRoot)
			{
				methodTokenNoLock = this.GetMethodTokenNoLock(method, true);
			}
			return methodTokenNoLock;
		}

		// Token: 0x06004C82 RID: 19586 RVA: 0x00114EC4 File Offset: 0x001130C4
		[SecurityCritical]
		internal MethodToken GetMethodTokenInternal(MethodInfo method)
		{
			object syncRoot = this.SyncRoot;
			MethodToken methodTokenNoLock;
			lock (syncRoot)
			{
				methodTokenNoLock = this.GetMethodTokenNoLock(method, false);
			}
			return methodTokenNoLock;
		}

		// Token: 0x06004C83 RID: 19587 RVA: 0x00114F08 File Offset: 0x00113108
		[SecurityCritical]
		private MethodToken GetMethodTokenNoLock(MethodInfo method, bool getGenericTypeDefinition)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			MethodBuilder methodBuilder;
			int str;
			if ((methodBuilder = method as MethodBuilder) != null)
			{
				int metadataTokenInternal = methodBuilder.MetadataTokenInternal;
				if (method.Module.Equals(this))
				{
					return new MethodToken(metadataTokenInternal);
				}
				if (method.DeclaringType == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
				}
				int tr = (getGenericTypeDefinition ? this.GetTypeToken(method.DeclaringType).Token : this.GetTypeTokenInternal(method.DeclaringType).Token);
				str = this.GetMemberRef(method.DeclaringType.Module, tr, metadataTokenInternal);
			}
			else
			{
				if (method is MethodOnTypeBuilderInstantiation)
				{
					return new MethodToken(this.GetMemberRefToken(method, null));
				}
				SymbolMethod symbolMethod;
				if ((symbolMethod = method as SymbolMethod) != null)
				{
					if (symbolMethod.GetModule() == this)
					{
						return symbolMethod.GetToken();
					}
					return symbolMethod.GetToken(this);
				}
				else
				{
					Type declaringType = method.DeclaringType;
					if (declaringType == null)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
					}
					if (declaringType.IsArray)
					{
						ParameterInfo[] parameters = method.GetParameters();
						Type[] array = new Type[parameters.Length];
						for (int i = 0; i < parameters.Length; i++)
						{
							array[i] = parameters[i].ParameterType;
						}
						return this.GetArrayMethodToken(declaringType, method.Name, method.CallingConvention, method.ReturnType, array);
					}
					RuntimeMethodInfo method2;
					if ((method2 = method as RuntimeMethodInfo) != null)
					{
						int tr = (getGenericTypeDefinition ? this.GetTypeToken(method.DeclaringType).Token : this.GetTypeTokenInternal(method.DeclaringType).Token);
						str = this.GetMemberRefOfMethodInfo(tr, method2);
					}
					else
					{
						ParameterInfo[] parameters2 = method.GetParameters();
						Type[] array2 = new Type[parameters2.Length];
						Type[][] array3 = new Type[array2.Length][];
						Type[][] array4 = new Type[array2.Length][];
						for (int j = 0; j < parameters2.Length; j++)
						{
							array2[j] = parameters2[j].ParameterType;
							array3[j] = parameters2[j].GetRequiredCustomModifiers();
							array4[j] = parameters2[j].GetOptionalCustomModifiers();
						}
						int tr = (getGenericTypeDefinition ? this.GetTypeToken(method.DeclaringType).Token : this.GetTypeTokenInternal(method.DeclaringType).Token);
						SignatureHelper methodSigHelper;
						try
						{
							methodSigHelper = SignatureHelper.GetMethodSigHelper(this, method.CallingConvention, method.ReturnType, method.ReturnParameter.GetRequiredCustomModifiers(), method.ReturnParameter.GetOptionalCustomModifiers(), array2, array3, array4);
						}
						catch (NotImplementedException)
						{
							methodSigHelper = SignatureHelper.GetMethodSigHelper(this, method.ReturnType, array2);
						}
						int length;
						byte[] signature = methodSigHelper.InternalGetSignature(out length);
						str = this.GetMemberRefFromSignature(tr, method.Name, signature, length);
					}
				}
			}
			return new MethodToken(str);
		}

		// Token: 0x06004C84 RID: 19588 RVA: 0x001151EC File Offset: 0x001133EC
		[SecuritySafeCritical]
		public MethodToken GetConstructorToken(ConstructorInfo constructor, IEnumerable<Type> optionalParameterTypes)
		{
			if (constructor == null)
			{
				throw new ArgumentNullException("constructor");
			}
			object syncRoot = this.SyncRoot;
			MethodToken result;
			lock (syncRoot)
			{
				result = new MethodToken(this.GetMethodTokenInternal(constructor, optionalParameterTypes, false));
			}
			return result;
		}

		// Token: 0x06004C85 RID: 19589 RVA: 0x0011524C File Offset: 0x0011344C
		[SecuritySafeCritical]
		public MethodToken GetMethodToken(MethodInfo method, IEnumerable<Type> optionalParameterTypes)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			object syncRoot = this.SyncRoot;
			MethodToken result;
			lock (syncRoot)
			{
				result = new MethodToken(this.GetMethodTokenInternal(method, optionalParameterTypes, true));
			}
			return result;
		}

		// Token: 0x06004C86 RID: 19590 RVA: 0x001152AC File Offset: 0x001134AC
		[SecurityCritical]
		internal int GetMethodTokenInternal(MethodBase method, IEnumerable<Type> optionalParameterTypes, bool useMethodDef)
		{
			MethodInfo methodInfo = method as MethodInfo;
			int num;
			if (method.IsGenericMethod)
			{
				MethodInfo methodInfo2 = methodInfo;
				bool isGenericMethodDefinition = methodInfo.IsGenericMethodDefinition;
				if (!isGenericMethodDefinition)
				{
					methodInfo2 = methodInfo.GetGenericMethodDefinition();
				}
				if (!this.Equals(methodInfo2.Module) || (methodInfo2.DeclaringType != null && methodInfo2.DeclaringType.IsGenericType))
				{
					num = this.GetMemberRefToken(methodInfo2, null);
				}
				else
				{
					num = this.GetMethodTokenInternal(methodInfo2).Token;
				}
				if (isGenericMethodDefinition && useMethodDef)
				{
					return num;
				}
				int sigLength;
				byte[] signature = SignatureHelper.GetMethodSpecSigHelper(this, methodInfo.GetGenericArguments()).InternalGetSignature(out sigLength);
				num = TypeBuilder.DefineMethodSpec(this.GetNativeHandle(), num, signature, sigLength);
			}
			else if ((method.CallingConvention & CallingConventions.VarArgs) == (CallingConventions)0 && (method.DeclaringType == null || !method.DeclaringType.IsGenericType))
			{
				if (methodInfo != null)
				{
					num = this.GetMethodTokenInternal(methodInfo).Token;
				}
				else
				{
					num = this.GetConstructorToken(method as ConstructorInfo).Token;
				}
			}
			else
			{
				num = this.GetMemberRefToken(method, optionalParameterTypes);
			}
			return num;
		}

		// Token: 0x06004C87 RID: 19591 RVA: 0x001153B8 File Offset: 0x001135B8
		[SecuritySafeCritical]
		public MethodToken GetArrayMethodToken(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
		{
			object syncRoot = this.SyncRoot;
			MethodToken arrayMethodTokenNoLock;
			lock (syncRoot)
			{
				arrayMethodTokenNoLock = this.GetArrayMethodTokenNoLock(arrayClass, methodName, callingConvention, returnType, parameterTypes);
			}
			return arrayMethodTokenNoLock;
		}

		// Token: 0x06004C88 RID: 19592 RVA: 0x00115404 File Offset: 0x00113604
		[SecurityCritical]
		private MethodToken GetArrayMethodTokenNoLock(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
		{
			if (arrayClass == null)
			{
				throw new ArgumentNullException("arrayClass");
			}
			if (methodName == null)
			{
				throw new ArgumentNullException("methodName");
			}
			if (methodName.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "methodName");
			}
			if (!arrayClass.IsArray)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_HasToBeArrayClass"));
			}
			this.CheckContext(new Type[] { returnType, arrayClass });
			this.CheckContext(parameterTypes);
			SignatureHelper methodSigHelper = SignatureHelper.GetMethodSigHelper(this, callingConvention, returnType, null, null, parameterTypes, null, null);
			int sigLength;
			byte[] signature = methodSigHelper.InternalGetSignature(out sigLength);
			TypeToken typeTokenInternal = this.GetTypeTokenInternal(arrayClass);
			return new MethodToken(ModuleBuilder.GetArrayMethodToken(this.GetNativeHandle(), typeTokenInternal.Token, methodName, signature, sigLength));
		}

		// Token: 0x06004C89 RID: 19593 RVA: 0x001154C0 File Offset: 0x001136C0
		[SecuritySafeCritical]
		public MethodInfo GetArrayMethod(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
		{
			this.CheckContext(new Type[] { returnType, arrayClass });
			this.CheckContext(parameterTypes);
			MethodToken arrayMethodToken = this.GetArrayMethodToken(arrayClass, methodName, callingConvention, returnType, parameterTypes);
			return new SymbolMethod(this, arrayMethodToken, arrayClass, methodName, callingConvention, returnType, parameterTypes);
		}

		// Token: 0x06004C8A RID: 19594 RVA: 0x00115506 File Offset: 0x00113706
		[SecuritySafeCritical]
		[ComVisible(true)]
		public MethodToken GetConstructorToken(ConstructorInfo con)
		{
			return this.InternalGetConstructorToken(con, false);
		}

		// Token: 0x06004C8B RID: 19595 RVA: 0x00115510 File Offset: 0x00113710
		[SecuritySafeCritical]
		public FieldToken GetFieldToken(FieldInfo field)
		{
			object syncRoot = this.SyncRoot;
			FieldToken fieldTokenNoLock;
			lock (syncRoot)
			{
				fieldTokenNoLock = this.GetFieldTokenNoLock(field);
			}
			return fieldTokenNoLock;
		}

		// Token: 0x06004C8C RID: 19596 RVA: 0x00115554 File Offset: 0x00113754
		[SecurityCritical]
		private FieldToken GetFieldTokenNoLock(FieldInfo field)
		{
			if (field == null)
			{
				throw new ArgumentNullException("con");
			}
			FieldBuilder fieldBuilder;
			int field2;
			RuntimeFieldInfo runtimeField;
			FieldOnTypeBuilderInstantiation fieldOnTypeBuilderInstantiation;
			if ((fieldBuilder = field as FieldBuilder) != null)
			{
				if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
				{
					int length;
					byte[] signature = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
					int num = this.GetTokenFromTypeSpec(signature, length);
					field2 = this.GetMemberRef(this, num, fieldBuilder.GetToken().Token);
				}
				else
				{
					if (fieldBuilder.Module.Equals(this))
					{
						return fieldBuilder.GetToken();
					}
					if (field.DeclaringType == null)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
					}
					int num = this.GetTypeTokenInternal(field.DeclaringType).Token;
					field2 = this.GetMemberRef(field.ReflectedType.Module, num, fieldBuilder.GetToken().Token);
				}
			}
			else if ((runtimeField = field as RuntimeFieldInfo) != null)
			{
				if (field.DeclaringType == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
				}
				if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
				{
					int length2;
					byte[] signature2 = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length2);
					int num = this.GetTokenFromTypeSpec(signature2, length2);
					field2 = this.GetMemberRefOfFieldInfo(num, field.DeclaringType.GetTypeHandleInternal(), runtimeField);
				}
				else
				{
					int num = this.GetTypeTokenInternal(field.DeclaringType).Token;
					field2 = this.GetMemberRefOfFieldInfo(num, field.DeclaringType.GetTypeHandleInternal(), runtimeField);
				}
			}
			else if ((fieldOnTypeBuilderInstantiation = field as FieldOnTypeBuilderInstantiation) != null)
			{
				FieldInfo fieldInfo = fieldOnTypeBuilderInstantiation.FieldInfo;
				int length3;
				byte[] signature3 = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length3);
				int num = this.GetTokenFromTypeSpec(signature3, length3);
				field2 = this.GetMemberRef(fieldInfo.ReflectedType.Module, num, fieldOnTypeBuilderInstantiation.MetadataTokenInternal);
			}
			else
			{
				int num = this.GetTypeTokenInternal(field.ReflectedType).Token;
				SignatureHelper fieldSigHelper = SignatureHelper.GetFieldSigHelper(this);
				fieldSigHelper.AddArgument(field.FieldType, field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers());
				int length4;
				byte[] signature4 = fieldSigHelper.InternalGetSignature(out length4);
				field2 = this.GetMemberRefFromSignature(num, field.Name, signature4, length4);
			}
			return new FieldToken(field2, field.GetType());
		}

		// Token: 0x06004C8D RID: 19597 RVA: 0x001157C4 File Offset: 0x001139C4
		[SecuritySafeCritical]
		public StringToken GetStringConstant(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			return new StringToken(ModuleBuilder.GetStringConstant(this.GetNativeHandle(), str, str.Length));
		}

		// Token: 0x06004C8E RID: 19598 RVA: 0x001157EC File Offset: 0x001139EC
		[SecuritySafeCritical]
		public SignatureToken GetSignatureToken(SignatureHelper sigHelper)
		{
			if (sigHelper == null)
			{
				throw new ArgumentNullException("sigHelper");
			}
			int sigLength;
			byte[] signature = sigHelper.InternalGetSignature(out sigLength);
			return new SignatureToken(TypeBuilder.GetTokenFromSig(this.GetNativeHandle(), signature, sigLength), this);
		}

		// Token: 0x06004C8F RID: 19599 RVA: 0x00115824 File Offset: 0x00113A24
		[SecuritySafeCritical]
		public SignatureToken GetSignatureToken(byte[] sigBytes, int sigLength)
		{
			if (sigBytes == null)
			{
				throw new ArgumentNullException("sigBytes");
			}
			byte[] array = new byte[sigBytes.Length];
			Array.Copy(sigBytes, array, sigBytes.Length);
			return new SignatureToken(TypeBuilder.GetTokenFromSig(this.GetNativeHandle(), array, sigLength), this);
		}

		// Token: 0x06004C90 RID: 19600 RVA: 0x00115868 File Offset: 0x00113A68
		[SecuritySafeCritical]
		[ComVisible(true)]
		public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
		{
			if (con == null)
			{
				throw new ArgumentNullException("con");
			}
			if (binaryAttribute == null)
			{
				throw new ArgumentNullException("binaryAttribute");
			}
			TypeBuilder.DefineCustomAttribute(this, 1, this.GetConstructorToken(con).Token, binaryAttribute, false, false);
		}

		// Token: 0x06004C91 RID: 19601 RVA: 0x001158B0 File Offset: 0x00113AB0
		[SecuritySafeCritical]
		public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
		{
			if (customBuilder == null)
			{
				throw new ArgumentNullException("customBuilder");
			}
			customBuilder.CreateCustomAttribute(this, 1);
		}

		// Token: 0x06004C92 RID: 19602 RVA: 0x001158C8 File Offset: 0x00113AC8
		public ISymbolWriter GetSymWriter()
		{
			return this.m_iSymWriter;
		}

		// Token: 0x06004C93 RID: 19603 RVA: 0x001158D0 File Offset: 0x00113AD0
		public ISymbolDocumentWriter DefineDocument(string url, Guid language, Guid languageVendor, Guid documentType)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}
			object syncRoot = this.SyncRoot;
			ISymbolDocumentWriter result;
			lock (syncRoot)
			{
				result = this.DefineDocumentNoLock(url, language, languageVendor, documentType);
			}
			return result;
		}

		// Token: 0x06004C94 RID: 19604 RVA: 0x00115928 File Offset: 0x00113B28
		private ISymbolDocumentWriter DefineDocumentNoLock(string url, Guid language, Guid languageVendor, Guid documentType)
		{
			if (this.m_iSymWriter == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
			}
			return this.m_iSymWriter.DefineDocument(url, language, languageVendor, documentType);
		}

		// Token: 0x06004C95 RID: 19605 RVA: 0x00115954 File Offset: 0x00113B54
		[SecuritySafeCritical]
		public void SetUserEntryPoint(MethodInfo entryPoint)
		{
			object syncRoot = this.SyncRoot;
			lock (syncRoot)
			{
				this.SetUserEntryPointNoLock(entryPoint);
			}
		}

		// Token: 0x06004C96 RID: 19606 RVA: 0x00115998 File Offset: 0x00113B98
		[SecurityCritical]
		private void SetUserEntryPointNoLock(MethodInfo entryPoint)
		{
			if (entryPoint == null)
			{
				throw new ArgumentNullException("entryPoint");
			}
			if (this.m_iSymWriter == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
			}
			if (entryPoint.DeclaringType != null)
			{
				if (!entryPoint.Module.Equals(this))
				{
					throw new InvalidOperationException(Environment.GetResourceString("Argument_NotInTheSameModuleBuilder"));
				}
			}
			else
			{
				MethodBuilder methodBuilder = entryPoint as MethodBuilder;
				if (methodBuilder != null && methodBuilder.GetModuleBuilder() != this)
				{
					throw new InvalidOperationException(Environment.GetResourceString("Argument_NotInTheSameModuleBuilder"));
				}
			}
			SymbolToken userEntryPoint = new SymbolToken(this.GetMethodTokenInternal(entryPoint).Token);
			this.m_iSymWriter.SetUserEntryPoint(userEntryPoint);
		}

		// Token: 0x06004C97 RID: 19607 RVA: 0x00115A50 File Offset: 0x00113C50
		public void SetSymCustomAttribute(string name, byte[] data)
		{
			object syncRoot = this.SyncRoot;
			lock (syncRoot)
			{
				this.SetSymCustomAttributeNoLock(name, data);
			}
		}

		// Token: 0x06004C98 RID: 19608 RVA: 0x00115A94 File Offset: 0x00113C94
		private void SetSymCustomAttributeNoLock(string name, byte[] data)
		{
			if (this.m_iSymWriter == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
			}
		}

		// Token: 0x06004C99 RID: 19609 RVA: 0x00115AAE File Offset: 0x00113CAE
		public bool IsTransient()
		{
			return this.InternalModule.IsTransientInternal();
		}

		// Token: 0x06004C9A RID: 19610 RVA: 0x00115ABB File Offset: 0x00113CBB
		void _ModuleBuilder.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004C9B RID: 19611 RVA: 0x00115AC2 File Offset: 0x00113CC2
		void _ModuleBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004C9C RID: 19612 RVA: 0x00115AC9 File Offset: 0x00113CC9
		void _ModuleBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004C9D RID: 19613 RVA: 0x00115AD0 File Offset: 0x00113CD0
		void _ModuleBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04001F56 RID: 8022
		private Dictionary<string, Type> m_TypeBuilderDict;

		// Token: 0x04001F57 RID: 8023
		private ISymbolWriter m_iSymWriter;

		// Token: 0x04001F58 RID: 8024
		internal ModuleBuilderData m_moduleData;

		// Token: 0x04001F59 RID: 8025
		private MethodToken m_EntryPoint;

		// Token: 0x04001F5A RID: 8026
		internal InternalModuleBuilder m_internalModuleBuilder;

		// Token: 0x04001F5B RID: 8027
		private AssemblyBuilder m_assemblyBuilder;
	}
}
