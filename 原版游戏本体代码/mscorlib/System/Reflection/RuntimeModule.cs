using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

namespace System.Reflection
{
	// Token: 0x0200060E RID: 1550
	[Serializable]
	internal class RuntimeModule : Module
	{
		// Token: 0x060047C1 RID: 18369 RVA: 0x001050CB File Offset: 0x001032CB
		internal RuntimeModule()
		{
			throw new NotSupportedException();
		}

		// Token: 0x060047C2 RID: 18370
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetType(RuntimeModule module, string className, bool ignoreCase, bool throwOnError, ObjectHandleOnStack type);

		// Token: 0x060047C3 RID: 18371
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall")]
		private static extern bool nIsTransientInternal(RuntimeModule module);

		// Token: 0x060047C4 RID: 18372
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetScopeName(RuntimeModule module, StringHandleOnStack retString);

		// Token: 0x060047C5 RID: 18373
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetFullyQualifiedName(RuntimeModule module, StringHandleOnStack retString);

		// Token: 0x060047C6 RID: 18374
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern RuntimeType[] GetTypes(RuntimeModule module);

		// Token: 0x060047C7 RID: 18375 RVA: 0x001050D8 File Offset: 0x001032D8
		[SecuritySafeCritical]
		internal RuntimeType[] GetDefinedTypes()
		{
			return RuntimeModule.GetTypes(this.GetNativeHandle());
		}

		// Token: 0x060047C8 RID: 18376
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern bool IsResource(RuntimeModule module);

		// Token: 0x060047C9 RID: 18377
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetSignerCertificate(RuntimeModule module, ObjectHandleOnStack retData);

		// Token: 0x060047CA RID: 18378 RVA: 0x001050E8 File Offset: 0x001032E8
		private static RuntimeTypeHandle[] ConvertToTypeHandleArray(Type[] genericArguments)
		{
			if (genericArguments == null)
			{
				return null;
			}
			int num = genericArguments.Length;
			RuntimeTypeHandle[] array = new RuntimeTypeHandle[num];
			for (int i = 0; i < num; i++)
			{
				Type type = genericArguments[i];
				if (type == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray"));
				}
				type = type.UnderlyingSystemType;
				if (type == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray"));
				}
				if (!(type is RuntimeType))
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray"));
				}
				array[i] = type.GetTypeHandleInternal();
			}
			return array;
		}

		// Token: 0x060047CB RID: 18379 RVA: 0x00105174 File Offset: 0x00103374
		[SecuritySafeCritical]
		public override byte[] ResolveSignature(int metadataToken)
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!this.MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", new object[] { metadataToken2, this }));
			}
			if (!metadataToken2.IsMemberRef && !metadataToken2.IsMethodDef && !metadataToken2.IsTypeSpec && !metadataToken2.IsSignature && !metadataToken2.IsFieldDef)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidToken", new object[] { metadataToken2, this }), "metadataToken");
			}
			ConstArray constArray;
			if (metadataToken2.IsMemberRef)
			{
				constArray = this.MetadataImport.GetMemberRefProps(metadataToken);
			}
			else
			{
				constArray = this.MetadataImport.GetSignatureFromToken(metadataToken);
			}
			byte[] array = new byte[constArray.Length];
			for (int i = 0; i < constArray.Length; i++)
			{
				array[i] = constArray[i];
			}
			return array;
		}

		// Token: 0x060047CC RID: 18380 RVA: 0x00105278 File Offset: 0x00103478
		[SecuritySafeCritical]
		public unsafe override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!this.MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", new object[] { metadataToken2, this }));
			}
			RuntimeTypeHandle[] typeInstantiationContext = RuntimeModule.ConvertToTypeHandleArray(genericTypeArguments);
			RuntimeTypeHandle[] methodInstantiationContext = RuntimeModule.ConvertToTypeHandleArray(genericMethodArguments);
			MethodBase methodBase;
			try
			{
				if (!metadataToken2.IsMethodDef && !metadataToken2.IsMethodSpec)
				{
					if (!metadataToken2.IsMemberRef)
					{
						throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveMethod", new object[] { metadataToken2, this }));
					}
					if (*(byte*)this.MetadataImport.GetMemberRefProps(metadataToken2).Signature.ToPointer() == 6)
					{
						throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveMethod", new object[] { metadataToken2, this }));
					}
				}
				IRuntimeMethodInfo runtimeMethodInfo = ModuleHandle.ResolveMethodHandleInternal(this.GetNativeHandle(), metadataToken2, typeInstantiationContext, methodInstantiationContext);
				Type type = RuntimeMethodHandle.GetDeclaringType(runtimeMethodInfo);
				if (type.IsGenericType || type.IsArray)
				{
					MetadataToken token = new MetadataToken(this.MetadataImport.GetParentToken(metadataToken2));
					if (metadataToken2.IsMethodSpec)
					{
						token = new MetadataToken(this.MetadataImport.GetParentToken(token));
					}
					type = this.ResolveType(token, genericTypeArguments, genericMethodArguments);
				}
				methodBase = RuntimeType.GetMethodBase(type as RuntimeType, runtimeMethodInfo);
			}
			catch (BadImageFormatException innerException)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), innerException);
			}
			return methodBase;
		}

		// Token: 0x060047CD RID: 18381 RVA: 0x0010543C File Offset: 0x0010363C
		[SecurityCritical]
		private FieldInfo ResolveLiteralField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!this.MetadataImport.IsValidToken(metadataToken2) || !metadataToken2.IsFieldDef)
			{
				throw new ArgumentOutOfRangeException("metadataToken", string.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_InvalidToken", new object[] { metadataToken2, this }), Array.Empty<object>()));
			}
			string name = this.MetadataImport.GetName(metadataToken2).ToString();
			int parentToken = this.MetadataImport.GetParentToken(metadataToken2);
			Type type = this.ResolveType(parentToken, genericTypeArguments, genericMethodArguments);
			type.GetFields();
			FieldInfo field;
			try
			{
				field = type.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			catch
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ResolveField", new object[] { metadataToken2, this }), "metadataToken");
			}
			return field;
		}

		// Token: 0x060047CE RID: 18382 RVA: 0x0010553C File Offset: 0x0010373C
		[SecuritySafeCritical]
		public unsafe override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!this.MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", new object[] { metadataToken2, this }));
			}
			RuntimeTypeHandle[] typeInstantiationContext = RuntimeModule.ConvertToTypeHandleArray(genericTypeArguments);
			RuntimeTypeHandle[] methodInstantiationContext = RuntimeModule.ConvertToTypeHandleArray(genericMethodArguments);
			FieldInfo result;
			try
			{
				IRuntimeFieldInfo runtimeFieldInfo;
				if (!metadataToken2.IsFieldDef)
				{
					if (!metadataToken2.IsMemberRef)
					{
						throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveField", new object[] { metadataToken2, this }));
					}
					if (*(byte*)this.MetadataImport.GetMemberRefProps(metadataToken2).Signature.ToPointer() != 6)
					{
						throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveField", new object[] { metadataToken2, this }));
					}
					runtimeFieldInfo = ModuleHandle.ResolveFieldHandleInternal(this.GetNativeHandle(), metadataToken2, typeInstantiationContext, methodInstantiationContext);
				}
				runtimeFieldInfo = ModuleHandle.ResolveFieldHandleInternal(this.GetNativeHandle(), metadataToken, typeInstantiationContext, methodInstantiationContext);
				RuntimeType runtimeType = RuntimeFieldHandle.GetApproxDeclaringType(runtimeFieldInfo.Value);
				if (runtimeType.IsGenericType || runtimeType.IsArray)
				{
					int parentToken = ModuleHandle.GetMetadataImport(this.GetNativeHandle()).GetParentToken(metadataToken);
					runtimeType = (RuntimeType)this.ResolveType(parentToken, genericTypeArguments, genericMethodArguments);
				}
				result = RuntimeType.GetFieldInfo(runtimeType, runtimeFieldInfo);
			}
			catch (MissingFieldException)
			{
				result = this.ResolveLiteralField(metadataToken2, genericTypeArguments, genericMethodArguments);
			}
			catch (BadImageFormatException innerException)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), innerException);
			}
			return result;
		}

		// Token: 0x060047CF RID: 18383 RVA: 0x00105708 File Offset: 0x00103908
		[SecuritySafeCritical]
		public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (metadataToken2.IsGlobalTypeDefToken)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ResolveModuleType", new object[] { metadataToken2 }), "metadataToken");
			}
			if (!this.MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", new object[] { metadataToken2, this }));
			}
			if (!metadataToken2.IsTypeDef && !metadataToken2.IsTypeSpec && !metadataToken2.IsTypeRef)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ResolveType", new object[] { metadataToken2, this }), "metadataToken");
			}
			RuntimeTypeHandle[] typeInstantiationContext = RuntimeModule.ConvertToTypeHandleArray(genericTypeArguments);
			RuntimeTypeHandle[] methodInstantiationContext = RuntimeModule.ConvertToTypeHandleArray(genericMethodArguments);
			Type result;
			try
			{
				Type runtimeType = this.GetModuleHandle().ResolveTypeHandle(metadataToken, typeInstantiationContext, methodInstantiationContext).GetRuntimeType();
				if (runtimeType == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_ResolveType", new object[] { metadataToken2, this }), "metadataToken");
				}
				result = runtimeType;
			}
			catch (BadImageFormatException innerException)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), innerException);
			}
			return result;
		}

		// Token: 0x060047D0 RID: 18384 RVA: 0x00105854 File Offset: 0x00103A54
		[SecuritySafeCritical]
		public unsafe override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (metadataToken2.IsProperty)
			{
				throw new ArgumentException(Environment.GetResourceString("InvalidOperation_PropertyInfoNotAvailable"));
			}
			if (metadataToken2.IsEvent)
			{
				throw new ArgumentException(Environment.GetResourceString("InvalidOperation_EventInfoNotAvailable"));
			}
			if (metadataToken2.IsMethodSpec || metadataToken2.IsMethodDef)
			{
				return this.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
			}
			if (metadataToken2.IsFieldDef)
			{
				return this.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
			}
			if (metadataToken2.IsTypeRef || metadataToken2.IsTypeDef || metadataToken2.IsTypeSpec)
			{
				return this.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
			}
			if (!metadataToken2.IsMemberRef)
			{
				throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveMember", new object[] { metadataToken2, this }));
			}
			if (!this.MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", new object[] { metadataToken2, this }));
			}
			if (*(byte*)this.MetadataImport.GetMemberRefProps(metadataToken2).Signature.ToPointer() == 6)
			{
				return this.ResolveField(metadataToken2, genericTypeArguments, genericMethodArguments);
			}
			return this.ResolveMethod(metadataToken2, genericTypeArguments, genericMethodArguments);
		}

		// Token: 0x060047D1 RID: 18385 RVA: 0x001059A8 File Offset: 0x00103BA8
		[SecuritySafeCritical]
		public override string ResolveString(int metadataToken)
		{
			MetadataToken metadataToken2 = new MetadataToken(metadataToken);
			if (!metadataToken2.IsString)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_ResolveString"), metadataToken, this.ToString()));
			}
			if (!this.MetadataImport.IsValidToken(metadataToken2))
			{
				throw new ArgumentOutOfRangeException("metadataToken", string.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_InvalidToken", new object[] { metadataToken2, this }), Array.Empty<object>()));
			}
			string userString = this.MetadataImport.GetUserString(metadataToken);
			if (userString == null)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_ResolveString"), metadataToken, this.ToString()));
			}
			return userString;
		}

		// Token: 0x060047D2 RID: 18386 RVA: 0x00105A73 File Offset: 0x00103C73
		public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
		{
			ModuleHandle.GetPEKind(this.GetNativeHandle(), out peKind, out machine);
		}

		// Token: 0x17000B07 RID: 2823
		// (get) Token: 0x060047D3 RID: 18387 RVA: 0x00105A82 File Offset: 0x00103C82
		public override int MDStreamVersion
		{
			[SecuritySafeCritical]
			get
			{
				return ModuleHandle.GetMDStreamVersion(this.GetNativeHandle());
			}
		}

		// Token: 0x060047D4 RID: 18388 RVA: 0x00105A8F File Offset: 0x00103C8F
		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			return this.GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
		}

		// Token: 0x060047D5 RID: 18389 RVA: 0x00105AA0 File Offset: 0x00103CA0
		internal MethodInfo GetMethodInternal(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			if (this.RuntimeType == null)
			{
				return null;
			}
			if (types == null)
			{
				return this.RuntimeType.GetMethod(name, bindingAttr);
			}
			return this.RuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
		}

		// Token: 0x17000B08 RID: 2824
		// (get) Token: 0x060047D6 RID: 18390 RVA: 0x00105AD8 File Offset: 0x00103CD8
		internal RuntimeType RuntimeType
		{
			get
			{
				if (this.m_runtimeType == null)
				{
					this.m_runtimeType = ModuleHandle.GetModuleType(this.GetNativeHandle());
				}
				return this.m_runtimeType;
			}
		}

		// Token: 0x060047D7 RID: 18391 RVA: 0x00105AFF File Offset: 0x00103CFF
		[SecuritySafeCritical]
		internal bool IsTransientInternal()
		{
			return RuntimeModule.nIsTransientInternal(this.GetNativeHandle());
		}

		// Token: 0x17000B09 RID: 2825
		// (get) Token: 0x060047D8 RID: 18392 RVA: 0x00105B0C File Offset: 0x00103D0C
		internal MetadataImport MetadataImport
		{
			[SecurityCritical]
			get
			{
				return ModuleHandle.GetMetadataImport(this.GetNativeHandle());
			}
		}

		// Token: 0x060047D9 RID: 18393 RVA: 0x00105B19 File Offset: 0x00103D19
		public override object[] GetCustomAttributes(bool inherit)
		{
			return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
		}

		// Token: 0x060047DA RID: 18394 RVA: 0x00105B30 File Offset: 0x00103D30
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
			return CustomAttribute.GetCustomAttributes(this, runtimeType);
		}

		// Token: 0x060047DB RID: 18395 RVA: 0x00105B84 File Offset: 0x00103D84
		[SecuritySafeCritical]
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
			return CustomAttribute.IsDefined(this, runtimeType);
		}

		// Token: 0x060047DC RID: 18396 RVA: 0x00105BD6 File Offset: 0x00103DD6
		public override IList<CustomAttributeData> GetCustomAttributesData()
		{
			return CustomAttributeData.GetCustomAttributesInternal(this);
		}

		// Token: 0x060047DD RID: 18397 RVA: 0x00105BDE File Offset: 0x00103DDE
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			UnitySerializationHolder.GetUnitySerializationInfo(info, 5, this.ScopeName, this.GetRuntimeAssembly());
		}

		// Token: 0x060047DE RID: 18398 RVA: 0x00105C04 File Offset: 0x00103E04
		[SecuritySafeCritical]
		[ComVisible(true)]
		public override Type GetType(string className, bool throwOnError, bool ignoreCase)
		{
			if (className == null)
			{
				throw new ArgumentNullException("className");
			}
			RuntimeType result = null;
			RuntimeModule.GetType(this.GetNativeHandle(), className, throwOnError, ignoreCase, JitHelpers.GetObjectHandleOnStack<RuntimeType>(ref result));
			return result;
		}

		// Token: 0x060047DF RID: 18399 RVA: 0x00105C38 File Offset: 0x00103E38
		[SecurityCritical]
		internal string GetFullyQualifiedName()
		{
			string result = null;
			RuntimeModule.GetFullyQualifiedName(this.GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref result));
			return result;
		}

		// Token: 0x17000B0A RID: 2826
		// (get) Token: 0x060047E0 RID: 18400 RVA: 0x00105C5C File Offset: 0x00103E5C
		public override string FullyQualifiedName
		{
			[SecuritySafeCritical]
			get
			{
				string fullyQualifiedName = this.GetFullyQualifiedName();
				if (fullyQualifiedName != null)
				{
					bool flag = true;
					try
					{
						Path.GetFullPathInternal(fullyQualifiedName);
					}
					catch (ArgumentException)
					{
						flag = false;
					}
					if (flag)
					{
						new FileIOPermission(FileIOPermissionAccess.PathDiscovery, fullyQualifiedName).Demand();
					}
				}
				return fullyQualifiedName;
			}
		}

		// Token: 0x060047E1 RID: 18401 RVA: 0x00105CA4 File Offset: 0x00103EA4
		[SecuritySafeCritical]
		public override Type[] GetTypes()
		{
			return RuntimeModule.GetTypes(this.GetNativeHandle());
		}

		// Token: 0x17000B0B RID: 2827
		// (get) Token: 0x060047E2 RID: 18402 RVA: 0x00105CC0 File Offset: 0x00103EC0
		public override Guid ModuleVersionId
		{
			[SecuritySafeCritical]
			get
			{
				Guid result;
				this.MetadataImport.GetScopeProps(out result);
				return result;
			}
		}

		// Token: 0x17000B0C RID: 2828
		// (get) Token: 0x060047E3 RID: 18403 RVA: 0x00105CDE File Offset: 0x00103EDE
		public override int MetadataToken
		{
			[SecuritySafeCritical]
			get
			{
				return ModuleHandle.GetToken(this.GetNativeHandle());
			}
		}

		// Token: 0x060047E4 RID: 18404 RVA: 0x00105CEB File Offset: 0x00103EEB
		public override bool IsResource()
		{
			return RuntimeModule.IsResource(this.GetNativeHandle());
		}

		// Token: 0x060047E5 RID: 18405 RVA: 0x00105CF8 File Offset: 0x00103EF8
		public override FieldInfo[] GetFields(BindingFlags bindingFlags)
		{
			if (this.RuntimeType == null)
			{
				return new FieldInfo[0];
			}
			return this.RuntimeType.GetFields(bindingFlags);
		}

		// Token: 0x060047E6 RID: 18406 RVA: 0x00105D1B File Offset: 0x00103F1B
		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (this.RuntimeType == null)
			{
				return null;
			}
			return this.RuntimeType.GetField(name, bindingAttr);
		}

		// Token: 0x060047E7 RID: 18407 RVA: 0x00105D48 File Offset: 0x00103F48
		public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
		{
			if (this.RuntimeType == null)
			{
				return new MethodInfo[0];
			}
			return this.RuntimeType.GetMethods(bindingFlags);
		}

		// Token: 0x17000B0D RID: 2829
		// (get) Token: 0x060047E8 RID: 18408 RVA: 0x00105D6C File Offset: 0x00103F6C
		public override string ScopeName
		{
			[SecuritySafeCritical]
			get
			{
				string result = null;
				RuntimeModule.GetScopeName(this.GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref result));
				return result;
			}
		}

		// Token: 0x17000B0E RID: 2830
		// (get) Token: 0x060047E9 RID: 18409 RVA: 0x00105D90 File Offset: 0x00103F90
		public override string Name
		{
			[SecuritySafeCritical]
			get
			{
				string fullyQualifiedName = this.GetFullyQualifiedName();
				int num = fullyQualifiedName.LastIndexOf('\\');
				if (num == -1)
				{
					return fullyQualifiedName;
				}
				return new string(fullyQualifiedName.ToCharArray(), num + 1, fullyQualifiedName.Length - num - 1);
			}
		}

		// Token: 0x17000B0F RID: 2831
		// (get) Token: 0x060047EA RID: 18410 RVA: 0x00105DCB File Offset: 0x00103FCB
		public override Assembly Assembly
		{
			get
			{
				return this.GetRuntimeAssembly();
			}
		}

		// Token: 0x060047EB RID: 18411 RVA: 0x00105DD3 File Offset: 0x00103FD3
		internal RuntimeAssembly GetRuntimeAssembly()
		{
			return this.m_runtimeAssembly;
		}

		// Token: 0x060047EC RID: 18412 RVA: 0x00105DDB File Offset: 0x00103FDB
		internal override ModuleHandle GetModuleHandle()
		{
			return new ModuleHandle(this);
		}

		// Token: 0x060047ED RID: 18413 RVA: 0x00105DE3 File Offset: 0x00103FE3
		internal RuntimeModule GetNativeHandle()
		{
			return this;
		}

		// Token: 0x060047EE RID: 18414 RVA: 0x00105DE8 File Offset: 0x00103FE8
		[SecuritySafeCritical]
		public override X509Certificate GetSignerCertificate()
		{
			byte[] array = null;
			RuntimeModule.GetSignerCertificate(this.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<byte[]>(ref array));
			if (array == null)
			{
				return null;
			}
			return new X509Certificate(array);
		}

		// Token: 0x04001DBE RID: 7614
		private RuntimeType m_runtimeType;

		// Token: 0x04001DBF RID: 7615
		private RuntimeAssembly m_runtimeAssembly;

		// Token: 0x04001DC0 RID: 7616
		private IntPtr m_pRefClass;

		// Token: 0x04001DC1 RID: 7617
		private IntPtr m_pData;

		// Token: 0x04001DC2 RID: 7618
		private IntPtr m_pGlobals;

		// Token: 0x04001DC3 RID: 7619
		private IntPtr m_pFields;
	}
}
