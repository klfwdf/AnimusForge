using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.TCEAdapterGen;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using Microsoft.Win32;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000979 RID: 2425
	[Guid("F1C3BF79-C3E4-11d3-88E7-00902754C43A")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public sealed class TypeLibConverter : ITypeLibConverter
	{
		// Token: 0x06006267 RID: 25191 RVA: 0x001510F4 File Offset: 0x0014F2F4
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public AssemblyBuilder ConvertTypeLibToAssembly([MarshalAs(UnmanagedType.Interface)] object typeLib, string asmFileName, int flags, ITypeLibImporterNotifySink notifySink, byte[] publicKey, StrongNameKeyPair keyPair, bool unsafeInterfaces)
		{
			return this.ConvertTypeLibToAssembly(typeLib, asmFileName, unsafeInterfaces ? TypeLibImporterFlags.UnsafeInterfaces : TypeLibImporterFlags.None, notifySink, publicKey, keyPair, null, null);
		}

		// Token: 0x06006268 RID: 25192 RVA: 0x0015111C File Offset: 0x0014F31C
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public AssemblyBuilder ConvertTypeLibToAssembly([MarshalAs(UnmanagedType.Interface)] object typeLib, string asmFileName, TypeLibImporterFlags flags, ITypeLibImporterNotifySink notifySink, byte[] publicKey, StrongNameKeyPair keyPair, string asmNamespace, Version asmVersion)
		{
			if (typeLib == null)
			{
				throw new ArgumentNullException("typeLib");
			}
			if (asmFileName == null)
			{
				throw new ArgumentNullException("asmFileName");
			}
			if (notifySink == null)
			{
				throw new ArgumentNullException("notifySink");
			}
			if (string.Empty.Equals(asmFileName))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileName"), "asmFileName");
			}
			if (asmFileName.Length > 260)
			{
				throw new ArgumentException(Environment.GetResourceString("IO.PathTooLong"), asmFileName);
			}
			if ((flags & TypeLibImporterFlags.PrimaryInteropAssembly) != TypeLibImporterFlags.None && publicKey == null && keyPair == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_PIAMustBeStrongNamed"));
			}
			ArrayList arrayList = null;
			AssemblyNameFlags asmNameFlags = AssemblyNameFlags.None;
			AssemblyName assemblyNameFromTypelib = TypeLibConverter.GetAssemblyNameFromTypelib(typeLib, asmFileName, publicKey, keyPair, asmVersion, asmNameFlags);
			AssemblyBuilder assemblyBuilder = TypeLibConverter.CreateAssemblyForTypeLib(typeLib, asmFileName, assemblyNameFromTypelib, (flags & TypeLibImporterFlags.PrimaryInteropAssembly) > TypeLibImporterFlags.None, (flags & TypeLibImporterFlags.ReflectionOnlyLoading) > TypeLibImporterFlags.None, (flags & TypeLibImporterFlags.NoDefineVersionResource) > TypeLibImporterFlags.None);
			string fileName = Path.GetFileName(asmFileName);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(fileName, fileName);
			if (asmNamespace == null)
			{
				asmNamespace = assemblyNameFromTypelib.Name;
			}
			TypeLibConverter.TypeResolveHandler typeResolveHandler = new TypeLibConverter.TypeResolveHandler(moduleBuilder, notifySink);
			AppDomain domain = Thread.GetDomain();
			ResolveEventHandler value = new ResolveEventHandler(typeResolveHandler.ResolveEvent);
			ResolveEventHandler value2 = new ResolveEventHandler(typeResolveHandler.ResolveAsmEvent);
			ResolveEventHandler value3 = new ResolveEventHandler(typeResolveHandler.ResolveROAsmEvent);
			domain.TypeResolve += value;
			domain.AssemblyResolve += value2;
			domain.ReflectionOnlyAssemblyResolve += value3;
			TypeLibConverter.nConvertTypeLibToMetadata(typeLib, assemblyBuilder.InternalAssembly, moduleBuilder.InternalModule, asmNamespace, flags, typeResolveHandler, out arrayList);
			TypeLibConverter.UpdateComTypesInAssembly(assemblyBuilder, moduleBuilder);
			if (arrayList.Count > 0)
			{
				new TCEAdapterGenerator().Process(moduleBuilder, arrayList);
			}
			domain.TypeResolve -= value;
			domain.AssemblyResolve -= value2;
			domain.ReflectionOnlyAssemblyResolve -= value3;
			return assemblyBuilder;
		}

		// Token: 0x06006269 RID: 25193 RVA: 0x001512B8 File Offset: 0x0014F4B8
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		[return: MarshalAs(UnmanagedType.Interface)]
		public object ConvertAssemblyToTypeLib(Assembly assembly, string strTypeLibName, TypeLibExporterFlags flags, ITypeLibExporterNotifySink notifySink)
		{
			AssemblyBuilder assemblyBuilder = assembly as AssemblyBuilder;
			RuntimeAssembly assembly2;
			if (assemblyBuilder != null)
			{
				assembly2 = assemblyBuilder.InternalAssembly;
			}
			else
			{
				assembly2 = assembly as RuntimeAssembly;
			}
			return TypeLibConverter.nConvertAssemblyToTypeLib(assembly2, strTypeLibName, flags, notifySink);
		}

		// Token: 0x0600626A RID: 25194 RVA: 0x001512F0 File Offset: 0x0014F4F0
		public bool GetPrimaryInteropAssembly(Guid g, int major, int minor, int lcid, out string asmName, out string asmCodeBase)
		{
			string name = "{" + g.ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			string name2 = major.ToString("x", CultureInfo.InvariantCulture) + "." + minor.ToString("x", CultureInfo.InvariantCulture);
			asmName = null;
			asmCodeBase = null;
			using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey("TypeLib", false))
			{
				if (registryKey != null)
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(name))
					{
						if (registryKey2 != null)
						{
							using (RegistryKey registryKey3 = registryKey2.OpenSubKey(name2, false))
							{
								if (registryKey3 != null)
								{
									asmName = (string)registryKey3.GetValue("PrimaryInteropAssemblyName");
									asmCodeBase = (string)registryKey3.GetValue("PrimaryInteropAssemblyCodeBase");
								}
							}
						}
					}
				}
			}
			return asmName != null;
		}

		// Token: 0x0600626B RID: 25195 RVA: 0x00151404 File Offset: 0x0014F604
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static AssemblyBuilder CreateAssemblyForTypeLib(object typeLib, string asmFileName, AssemblyName asmName, bool bPrimaryInteropAssembly, bool bReflectionOnly, bool bNoDefineVersionResource)
		{
			AppDomain domain = Thread.GetDomain();
			string text = null;
			if (asmFileName != null)
			{
				text = Path.GetDirectoryName(asmFileName);
				if (string.IsNullOrEmpty(text))
				{
					text = null;
				}
			}
			AssemblyBuilderAccess access;
			if (bReflectionOnly)
			{
				access = AssemblyBuilderAccess.ReflectionOnly;
			}
			else
			{
				access = AssemblyBuilderAccess.RunAndSave;
			}
			List<CustomAttributeBuilder> list = new List<CustomAttributeBuilder>();
			ConstructorInfo constructor = typeof(SecurityRulesAttribute).GetConstructor(new Type[] { typeof(SecurityRuleSet) });
			CustomAttributeBuilder item = new CustomAttributeBuilder(constructor, new object[] { SecurityRuleSet.Level2 });
			list.Add(item);
			AssemblyBuilder assemblyBuilder = domain.DefineDynamicAssembly(asmName, access, text, false, list);
			TypeLibConverter.SetGuidAttributeOnAssembly(assemblyBuilder, typeLib);
			TypeLibConverter.SetImportedFromTypeLibAttrOnAssembly(assemblyBuilder, typeLib);
			if (bNoDefineVersionResource)
			{
				TypeLibConverter.SetTypeLibVersionAttribute(assemblyBuilder, typeLib);
			}
			else
			{
				TypeLibConverter.SetVersionInformation(assemblyBuilder, typeLib, asmName);
			}
			if (bPrimaryInteropAssembly)
			{
				TypeLibConverter.SetPIAAttributeOnAssembly(assemblyBuilder, typeLib);
			}
			return assemblyBuilder;
		}

		// Token: 0x0600626C RID: 25196 RVA: 0x001514C0 File Offset: 0x0014F6C0
		[SecurityCritical]
		internal static AssemblyName GetAssemblyNameFromTypelib(object typeLib, string asmFileName, byte[] publicKey, StrongNameKeyPair keyPair, Version asmVersion, AssemblyNameFlags asmNameFlags)
		{
			string text = null;
			string text2 = null;
			int num = 0;
			string text3 = null;
			ITypeLib typeLib2 = (ITypeLib)typeLib;
			typeLib2.GetDocumentation(-1, out text, out text2, out num, out text3);
			if (asmFileName == null)
			{
				asmFileName = text;
			}
			else
			{
				string fileName = Path.GetFileName(asmFileName);
				string extension = Path.GetExtension(asmFileName);
				if (!".dll".Equals(extension, StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileExtension"));
				}
				asmFileName = fileName.Substring(0, fileName.Length - ".dll".Length);
			}
			if (asmVersion == null)
			{
				int major;
				int minor;
				Marshal.GetTypeLibVersion(typeLib2, out major, out minor);
				asmVersion = new Version(major, minor, 0, 0);
			}
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.Init(asmFileName, publicKey, null, asmVersion, null, AssemblyHashAlgorithm.None, AssemblyVersionCompatibility.SameMachine, null, asmNameFlags, keyPair);
			return assemblyName;
		}

		// Token: 0x0600626D RID: 25197 RVA: 0x00151584 File Offset: 0x0014F784
		private static void UpdateComTypesInAssembly(AssemblyBuilder asmBldr, ModuleBuilder modBldr)
		{
			AssemblyBuilderData assemblyData = asmBldr.m_assemblyData;
			Type[] types = modBldr.GetTypes();
			int num = types.Length;
			for (int i = 0; i < num; i++)
			{
				assemblyData.AddPublicComType(types[i]);
			}
		}

		// Token: 0x0600626E RID: 25198 RVA: 0x001515B8 File Offset: 0x0014F7B8
		[SecurityCritical]
		private static void SetGuidAttributeOnAssembly(AssemblyBuilder asmBldr, object typeLib)
		{
			Type[] types = new Type[] { typeof(string) };
			ConstructorInfo constructor = typeof(GuidAttribute).GetConstructor(types);
			object[] constructorArgs = new object[] { Marshal.GetTypeLibGuid((ITypeLib)typeLib).ToString() };
			CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(constructor, constructorArgs);
			asmBldr.SetCustomAttribute(customAttribute);
		}

		// Token: 0x0600626F RID: 25199 RVA: 0x00151620 File Offset: 0x0014F820
		[SecurityCritical]
		private static void SetImportedFromTypeLibAttrOnAssembly(AssemblyBuilder asmBldr, object typeLib)
		{
			Type[] types = new Type[] { typeof(string) };
			ConstructorInfo constructor = typeof(ImportedFromTypeLibAttribute).GetConstructor(types);
			string typeLibName = Marshal.GetTypeLibName((ITypeLib)typeLib);
			object[] constructorArgs = new object[] { typeLibName };
			CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(constructor, constructorArgs);
			asmBldr.SetCustomAttribute(customAttribute);
		}

		// Token: 0x06006270 RID: 25200 RVA: 0x0015167C File Offset: 0x0014F87C
		[SecurityCritical]
		private static void SetTypeLibVersionAttribute(AssemblyBuilder asmBldr, object typeLib)
		{
			Type[] types = new Type[]
			{
				typeof(int),
				typeof(int)
			};
			ConstructorInfo constructor = typeof(TypeLibVersionAttribute).GetConstructor(types);
			int num;
			int num2;
			Marshal.GetTypeLibVersion((ITypeLib)typeLib, out num, out num2);
			object[] constructorArgs = new object[] { num, num2 };
			CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(constructor, constructorArgs);
			asmBldr.SetCustomAttribute(customAttribute);
		}

		// Token: 0x06006271 RID: 25201 RVA: 0x001516F8 File Offset: 0x0014F8F8
		[SecurityCritical]
		private static void SetVersionInformation(AssemblyBuilder asmBldr, object typeLib, AssemblyName asmName)
		{
			string arg = null;
			string text = null;
			int num = 0;
			string text2 = null;
			ITypeLib typeLib2 = (ITypeLib)typeLib;
			typeLib2.GetDocumentation(-1, out arg, out text, out num, out text2);
			string product = string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("TypeLibConverter_ImportedTypeLibProductName"), arg);
			asmBldr.DefineVersionInfoResource(product, asmName.Version.ToString(), null, null, null);
			TypeLibConverter.SetTypeLibVersionAttribute(asmBldr, typeLib);
		}

		// Token: 0x06006272 RID: 25202 RVA: 0x0015175C File Offset: 0x0014F95C
		[SecurityCritical]
		private static void SetPIAAttributeOnAssembly(AssemblyBuilder asmBldr, object typeLib)
		{
			IntPtr zero = IntPtr.Zero;
			ITypeLib typeLib2 = (ITypeLib)typeLib;
			int num = 0;
			int num2 = 0;
			Type[] types = new Type[]
			{
				typeof(int),
				typeof(int)
			};
			ConstructorInfo constructor = typeof(PrimaryInteropAssemblyAttribute).GetConstructor(types);
			try
			{
				typeLib2.GetLibAttr(out zero);
				TYPELIBATTR typelibattr = (TYPELIBATTR)Marshal.PtrToStructure(zero, typeof(TYPELIBATTR));
				num = (int)typelibattr.wMajorVerNum;
				num2 = (int)typelibattr.wMinorVerNum;
			}
			finally
			{
				if (zero != IntPtr.Zero)
				{
					typeLib2.ReleaseTLibAttr(zero);
				}
			}
			object[] constructorArgs = new object[] { num, num2 };
			CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(constructor, constructorArgs);
			asmBldr.SetCustomAttribute(customAttribute);
		}

		// Token: 0x06006273 RID: 25203
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void nConvertTypeLibToMetadata(object typeLib, RuntimeAssembly asmBldr, RuntimeModule modBldr, string nameSpace, TypeLibImporterFlags flags, ITypeLibImporterNotifySink notifySink, out ArrayList eventItfInfoList);

		// Token: 0x06006274 RID: 25204
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern object nConvertAssemblyToTypeLib(RuntimeAssembly assembly, string strTypeLibName, TypeLibExporterFlags flags, ITypeLibExporterNotifySink notifySink);

		// Token: 0x06006275 RID: 25205
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void LoadInMemoryTypeByName(RuntimeModule module, string className);

		// Token: 0x04002BDF RID: 11231
		private const string s_strTypeLibAssemblyTitlePrefix = "TypeLib ";

		// Token: 0x04002BE0 RID: 11232
		private const string s_strTypeLibAssemblyDescPrefix = "Assembly generated from typelib ";

		// Token: 0x04002BE1 RID: 11233
		private const int MAX_NAMESPACE_LENGTH = 1024;

		// Token: 0x02000C99 RID: 3225
		private class TypeResolveHandler : ITypeLibImporterNotifySink
		{
			// Token: 0x0600711A RID: 28954 RVA: 0x00185462 File Offset: 0x00183662
			public TypeResolveHandler(ModuleBuilder mod, ITypeLibImporterNotifySink userSink)
			{
				this.m_Module = mod;
				this.m_UserSink = userSink;
			}

			// Token: 0x0600711B RID: 28955 RVA: 0x00185483 File Offset: 0x00183683
			public void ReportEvent(ImporterEventKind eventKind, int eventCode, string eventMsg)
			{
				this.m_UserSink.ReportEvent(eventKind, eventCode, eventMsg);
			}

			// Token: 0x0600711C RID: 28956 RVA: 0x00185494 File Offset: 0x00183694
			public Assembly ResolveRef(object typeLib)
			{
				Assembly assembly = this.m_UserSink.ResolveRef(typeLib);
				if (assembly == null)
				{
					throw new ArgumentNullException();
				}
				RuntimeAssembly runtimeAssembly = assembly as RuntimeAssembly;
				if (runtimeAssembly == null)
				{
					AssemblyBuilder assemblyBuilder = assembly as AssemblyBuilder;
					if (assemblyBuilder != null)
					{
						runtimeAssembly = assemblyBuilder.InternalAssembly;
					}
				}
				if (runtimeAssembly == null)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
				}
				this.m_AsmList.Add(runtimeAssembly);
				return runtimeAssembly;
			}

			// Token: 0x0600711D RID: 28957 RVA: 0x0018550C File Offset: 0x0018370C
			[SecurityCritical]
			public Assembly ResolveEvent(object sender, ResolveEventArgs args)
			{
				try
				{
					TypeLibConverter.LoadInMemoryTypeByName(this.m_Module.GetNativeHandle(), args.Name);
					return this.m_Module.Assembly;
				}
				catch (TypeLoadException ex)
				{
					if (ex.ResourceId != -2146233054)
					{
						throw;
					}
				}
				foreach (RuntimeAssembly runtimeAssembly in this.m_AsmList)
				{
					try
					{
						runtimeAssembly.GetType(args.Name, true, false);
						return runtimeAssembly;
					}
					catch (TypeLoadException ex2)
					{
						if (ex2._HResult != -2146233054)
						{
							throw;
						}
					}
				}
				return null;
			}

			// Token: 0x0600711E RID: 28958 RVA: 0x001855D0 File Offset: 0x001837D0
			public Assembly ResolveAsmEvent(object sender, ResolveEventArgs args)
			{
				foreach (RuntimeAssembly runtimeAssembly in this.m_AsmList)
				{
					if (string.Compare(runtimeAssembly.FullName, args.Name, StringComparison.OrdinalIgnoreCase) == 0)
					{
						return runtimeAssembly;
					}
				}
				return null;
			}

			// Token: 0x0600711F RID: 28959 RVA: 0x00185638 File Offset: 0x00183838
			public Assembly ResolveROAsmEvent(object sender, ResolveEventArgs args)
			{
				foreach (RuntimeAssembly runtimeAssembly in this.m_AsmList)
				{
					if (string.Compare(runtimeAssembly.FullName, args.Name, StringComparison.OrdinalIgnoreCase) == 0)
					{
						return runtimeAssembly;
					}
				}
				string assemblyString = AppDomain.CurrentDomain.ApplyPolicy(args.Name);
				return Assembly.ReflectionOnlyLoad(assemblyString);
			}

			// Token: 0x04003857 RID: 14423
			private ModuleBuilder m_Module;

			// Token: 0x04003858 RID: 14424
			private ITypeLibImporterNotifySink m_UserSink;

			// Token: 0x04003859 RID: 14425
			private List<RuntimeAssembly> m_AsmList = new List<RuntimeAssembly>();
		}
	}
}
