using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using Microsoft.Win32;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000976 RID: 2422
	[Guid("475E398F-8AFA-43a7-A3BE-F4EF8D6787C9")]
	[ClassInterface(ClassInterfaceType.None)]
	[ComVisible(true)]
	public class RegistrationServices : IRegistrationServices
	{
		// Token: 0x06006242 RID: 25154 RVA: 0x0014F880 File Offset: 0x0014DA80
		[SecurityCritical]
		public virtual bool RegisterAssembly(Assembly assembly, AssemblyRegistrationFlags flags)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (assembly.ReflectionOnly)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsmLoadedForReflectionOnly"));
			}
			RuntimeAssembly runtimeAssembly = assembly as RuntimeAssembly;
			if (runtimeAssembly == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
			}
			string fullName = assembly.FullName;
			if (fullName == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoAsmName"));
			}
			string text = null;
			if ((flags & AssemblyRegistrationFlags.SetCodeBase) != AssemblyRegistrationFlags.None)
			{
				text = runtimeAssembly.GetCodeBase(false);
				if (text == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoAsmCodeBase"));
				}
			}
			Type[] registrableTypesInAssembly = this.GetRegistrableTypesInAssembly(assembly);
			int num = registrableTypesInAssembly.Length;
			string strAsmVersion = runtimeAssembly.GetVersion().ToString();
			string imageRuntimeVersion = assembly.ImageRuntimeVersion;
			for (int i = 0; i < num; i++)
			{
				if (this.IsRegisteredAsValueType(registrableTypesInAssembly[i]))
				{
					this.RegisterValueType(registrableTypesInAssembly[i], fullName, strAsmVersion, text, imageRuntimeVersion);
				}
				else if (this.TypeRepresentsComType(registrableTypesInAssembly[i]))
				{
					this.RegisterComImportedType(registrableTypesInAssembly[i], fullName, strAsmVersion, text, imageRuntimeVersion);
				}
				else
				{
					this.RegisterManagedType(registrableTypesInAssembly[i], fullName, strAsmVersion, text, imageRuntimeVersion);
				}
				this.CallUserDefinedRegistrationMethod(registrableTypesInAssembly[i], true);
			}
			object[] customAttributes = assembly.GetCustomAttributes(typeof(PrimaryInteropAssemblyAttribute), false);
			int num2 = customAttributes.Length;
			for (int j = 0; j < num2; j++)
			{
				this.RegisterPrimaryInteropAssembly(runtimeAssembly, text, (PrimaryInteropAssemblyAttribute)customAttributes[j]);
			}
			return registrableTypesInAssembly.Length != 0 || num2 > 0;
		}

		// Token: 0x06006243 RID: 25155 RVA: 0x0014F9E8 File Offset: 0x0014DBE8
		[SecurityCritical]
		public virtual bool UnregisterAssembly(Assembly assembly)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (assembly.ReflectionOnly)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsmLoadedForReflectionOnly"));
			}
			RuntimeAssembly runtimeAssembly = assembly as RuntimeAssembly;
			if (runtimeAssembly == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
			}
			bool flag = true;
			Type[] registrableTypesInAssembly = this.GetRegistrableTypesInAssembly(assembly);
			int num = registrableTypesInAssembly.Length;
			string strAsmVersion = runtimeAssembly.GetVersion().ToString();
			for (int i = 0; i < num; i++)
			{
				this.CallUserDefinedRegistrationMethod(registrableTypesInAssembly[i], false);
				if (this.IsRegisteredAsValueType(registrableTypesInAssembly[i]))
				{
					if (!this.UnregisterValueType(registrableTypesInAssembly[i], strAsmVersion))
					{
						flag = false;
					}
				}
				else if (this.TypeRepresentsComType(registrableTypesInAssembly[i]))
				{
					if (!this.UnregisterComImportedType(registrableTypesInAssembly[i], strAsmVersion))
					{
						flag = false;
					}
				}
				else if (!this.UnregisterManagedType(registrableTypesInAssembly[i], strAsmVersion))
				{
					flag = false;
				}
			}
			object[] customAttributes = assembly.GetCustomAttributes(typeof(PrimaryInteropAssemblyAttribute), false);
			int num2 = customAttributes.Length;
			if (flag)
			{
				for (int j = 0; j < num2; j++)
				{
					this.UnregisterPrimaryInteropAssembly(assembly, (PrimaryInteropAssemblyAttribute)customAttributes[j]);
				}
			}
			return registrableTypesInAssembly.Length != 0 || num2 > 0;
		}

		// Token: 0x06006244 RID: 25156 RVA: 0x0014FB10 File Offset: 0x0014DD10
		[SecurityCritical]
		public virtual Type[] GetRegistrableTypesInAssembly(Assembly assembly)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			if (!(assembly is RuntimeAssembly))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "assembly");
			}
			Type[] exportedTypes = assembly.GetExportedTypes();
			int num = exportedTypes.Length;
			ArrayList arrayList = new ArrayList();
			for (int i = 0; i < num; i++)
			{
				Type type = exportedTypes[i];
				if (this.TypeRequiresRegistration(type))
				{
					arrayList.Add(type);
				}
			}
			Type[] array = new Type[arrayList.Count];
			arrayList.CopyTo(array);
			return array;
		}

		// Token: 0x06006245 RID: 25157 RVA: 0x0014FB9C File Offset: 0x0014DD9C
		[SecurityCritical]
		public virtual string GetProgIdForType(Type type)
		{
			return Marshal.GenerateProgIdForType(type);
		}

		// Token: 0x06006246 RID: 25158 RVA: 0x0014FBA4 File Offset: 0x0014DDA4
		[SecurityCritical]
		public virtual void RegisterTypeForComClients(Type type, ref Guid g)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (type as RuntimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "type");
			}
			if (!this.TypeRequiresRegistration(type))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustBeComCreatable"), "type");
			}
			RegistrationServices.RegisterTypeForComClientsNative(type, ref g);
		}

		// Token: 0x06006247 RID: 25159 RVA: 0x0014FC0D File Offset: 0x0014DE0D
		public virtual Guid GetManagedCategoryGuid()
		{
			return RegistrationServices.s_ManagedCategoryGuid;
		}

		// Token: 0x06006248 RID: 25160 RVA: 0x0014FC14 File Offset: 0x0014DE14
		[SecurityCritical]
		public virtual bool TypeRequiresRegistration(Type type)
		{
			return RegistrationServices.TypeRequiresRegistrationHelper(type);
		}

		// Token: 0x06006249 RID: 25161 RVA: 0x0014FC1C File Offset: 0x0014DE1C
		[SecuritySafeCritical]
		public virtual bool TypeRepresentsComType(Type type)
		{
			if (!type.IsCOMObject)
			{
				return false;
			}
			if (type.IsImport)
			{
				return true;
			}
			Type baseComImportType = this.GetBaseComImportType(type);
			return Marshal.GenerateGuidForType(type) == Marshal.GenerateGuidForType(baseComImportType);
		}

		// Token: 0x0600624A RID: 25162 RVA: 0x0014FC5C File Offset: 0x0014DE5C
		[SecurityCritical]
		[ComVisible(false)]
		public virtual int RegisterTypeForComClients(Type type, RegistrationClassContext classContext, RegistrationConnectionType flags)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (type as RuntimeType == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "type");
			}
			if (!this.TypeRequiresRegistration(type))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustBeComCreatable"), "type");
			}
			return RegistrationServices.RegisterTypeForComClientsExNative(type, classContext, flags);
		}

		// Token: 0x0600624B RID: 25163 RVA: 0x0014FCC6 File Offset: 0x0014DEC6
		[SecurityCritical]
		[ComVisible(false)]
		public virtual void UnregisterTypeForComClients(int cookie)
		{
			RegistrationServices.CoRevokeClassObject(cookie);
		}

		// Token: 0x0600624C RID: 25164 RVA: 0x0014FCD0 File Offset: 0x0014DED0
		[SecurityCritical]
		internal static bool TypeRequiresRegistrationHelper(Type type)
		{
			return (type.IsClass || type.IsValueType) && !type.IsAbstract && (type.IsValueType || !(type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null) == null)) && Marshal.IsTypeVisibleFromCom(type);
		}

		// Token: 0x0600624D RID: 25165 RVA: 0x0014FD24 File Offset: 0x0014DF24
		[SecurityCritical]
		private void RegisterValueType(Type type, string strAsmName, string strAsmVersion, string strAsmCodeBase, string strRuntimeVersion)
		{
			string subkey = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			using (RegistryKey registryKey = Registry.ClassesRoot.CreateSubKey("Record"))
			{
				using (RegistryKey registryKey2 = registryKey.CreateSubKey(subkey))
				{
					using (RegistryKey registryKey3 = registryKey2.CreateSubKey(strAsmVersion))
					{
						registryKey3.SetValue("Class", type.FullName);
						registryKey3.SetValue("Assembly", strAsmName);
						registryKey3.SetValue("RuntimeVersion", strRuntimeVersion);
						if (strAsmCodeBase != null)
						{
							registryKey3.SetValue("CodeBase", strAsmCodeBase);
						}
					}
				}
			}
		}

		// Token: 0x0600624E RID: 25166 RVA: 0x0014FE0C File Offset: 0x0014E00C
		[SecurityCritical]
		private void RegisterManagedType(Type type, string strAsmName, string strAsmVersion, string strAsmCodeBase, string strRuntimeVersion)
		{
			string value = type.FullName ?? "";
			string text = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			string progIdForType = this.GetProgIdForType(type);
			if (progIdForType != string.Empty)
			{
				using (RegistryKey registryKey = Registry.ClassesRoot.CreateSubKey(progIdForType))
				{
					registryKey.SetValue("", value);
					using (RegistryKey registryKey2 = registryKey.CreateSubKey("CLSID"))
					{
						registryKey2.SetValue("", text);
					}
				}
			}
			using (RegistryKey registryKey3 = Registry.ClassesRoot.CreateSubKey("CLSID"))
			{
				using (RegistryKey registryKey4 = registryKey3.CreateSubKey(text))
				{
					registryKey4.SetValue("", value);
					using (RegistryKey registryKey5 = registryKey4.CreateSubKey("InprocServer32"))
					{
						registryKey5.SetValue("", "mscoree.dll");
						registryKey5.SetValue("ThreadingModel", "Both");
						registryKey5.SetValue("Class", type.FullName);
						registryKey5.SetValue("Assembly", strAsmName);
						registryKey5.SetValue("RuntimeVersion", strRuntimeVersion);
						if (strAsmCodeBase != null)
						{
							registryKey5.SetValue("CodeBase", strAsmCodeBase);
						}
						using (RegistryKey registryKey6 = registryKey5.CreateSubKey(strAsmVersion))
						{
							registryKey6.SetValue("Class", type.FullName);
							registryKey6.SetValue("Assembly", strAsmName);
							registryKey6.SetValue("RuntimeVersion", strRuntimeVersion);
							if (strAsmCodeBase != null)
							{
								registryKey6.SetValue("CodeBase", strAsmCodeBase);
							}
						}
						if (progIdForType != string.Empty)
						{
							using (RegistryKey registryKey7 = registryKey4.CreateSubKey("ProgId"))
							{
								registryKey7.SetValue("", progIdForType);
							}
						}
					}
					using (RegistryKey registryKey8 = registryKey4.CreateSubKey("Implemented Categories"))
					{
						using (registryKey8.CreateSubKey("{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}"))
						{
						}
					}
				}
			}
			this.EnsureManagedCategoryExists();
		}

		// Token: 0x0600624F RID: 25167 RVA: 0x00150120 File Offset: 0x0014E320
		[SecurityCritical]
		private void RegisterComImportedType(Type type, string strAsmName, string strAsmVersion, string strAsmCodeBase, string strRuntimeVersion)
		{
			string subkey = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			using (RegistryKey registryKey = Registry.ClassesRoot.CreateSubKey("CLSID"))
			{
				using (RegistryKey registryKey2 = registryKey.CreateSubKey(subkey))
				{
					using (RegistryKey registryKey3 = registryKey2.CreateSubKey("InprocServer32"))
					{
						registryKey3.SetValue("Class", type.FullName);
						registryKey3.SetValue("Assembly", strAsmName);
						registryKey3.SetValue("RuntimeVersion", strRuntimeVersion);
						if (strAsmCodeBase != null)
						{
							registryKey3.SetValue("CodeBase", strAsmCodeBase);
						}
						using (RegistryKey registryKey4 = registryKey3.CreateSubKey(strAsmVersion))
						{
							registryKey4.SetValue("Class", type.FullName);
							registryKey4.SetValue("Assembly", strAsmName);
							registryKey4.SetValue("RuntimeVersion", strRuntimeVersion);
							if (strAsmCodeBase != null)
							{
								registryKey4.SetValue("CodeBase", strAsmCodeBase);
							}
						}
					}
				}
			}
		}

		// Token: 0x06006250 RID: 25168 RVA: 0x0015026C File Offset: 0x0014E46C
		[SecurityCritical]
		private bool UnregisterValueType(Type type, string strAsmVersion)
		{
			bool result = true;
			string text = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey("Record", true))
			{
				if (registryKey != null)
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(text, true))
					{
						if (registryKey2 != null)
						{
							using (RegistryKey registryKey3 = registryKey2.OpenSubKey(strAsmVersion, true))
							{
								if (registryKey3 != null)
								{
									registryKey3.DeleteValue("Assembly", false);
									registryKey3.DeleteValue("Class", false);
									registryKey3.DeleteValue("CodeBase", false);
									registryKey3.DeleteValue("RuntimeVersion", false);
									if (registryKey3.SubKeyCount == 0 && registryKey3.ValueCount == 0)
									{
										registryKey2.DeleteSubKey(strAsmVersion);
									}
								}
							}
							if (registryKey2.SubKeyCount != 0)
							{
								result = false;
							}
							if (registryKey2.SubKeyCount == 0 && registryKey2.ValueCount == 0)
							{
								registryKey.DeleteSubKey(text);
							}
						}
					}
					if (registryKey.SubKeyCount == 0 && registryKey.ValueCount == 0)
					{
						Registry.ClassesRoot.DeleteSubKey("Record");
					}
				}
			}
			return result;
		}

		// Token: 0x06006251 RID: 25169 RVA: 0x001503C4 File Offset: 0x0014E5C4
		[SecurityCritical]
		private bool UnregisterManagedType(Type type, string strAsmVersion)
		{
			bool flag = true;
			string text = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			string progIdForType = this.GetProgIdForType(type);
			using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey("CLSID", true))
			{
				if (registryKey != null)
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(text, true))
					{
						if (registryKey2 != null)
						{
							using (RegistryKey registryKey3 = registryKey2.OpenSubKey("InprocServer32", true))
							{
								if (registryKey3 != null)
								{
									using (RegistryKey registryKey4 = registryKey3.OpenSubKey(strAsmVersion, true))
									{
										if (registryKey4 != null)
										{
											registryKey4.DeleteValue("Assembly", false);
											registryKey4.DeleteValue("Class", false);
											registryKey4.DeleteValue("RuntimeVersion", false);
											registryKey4.DeleteValue("CodeBase", false);
											if (registryKey4.SubKeyCount == 0 && registryKey4.ValueCount == 0)
											{
												registryKey3.DeleteSubKey(strAsmVersion);
											}
										}
									}
									if (registryKey3.SubKeyCount != 0)
									{
										flag = false;
									}
									if (flag)
									{
										registryKey3.DeleteValue("", false);
										registryKey3.DeleteValue("ThreadingModel", false);
									}
									registryKey3.DeleteValue("Assembly", false);
									registryKey3.DeleteValue("Class", false);
									registryKey3.DeleteValue("RuntimeVersion", false);
									registryKey3.DeleteValue("CodeBase", false);
									if (registryKey3.SubKeyCount == 0 && registryKey3.ValueCount == 0)
									{
										registryKey2.DeleteSubKey("InprocServer32");
									}
								}
							}
							if (flag)
							{
								registryKey2.DeleteValue("", false);
								if (progIdForType != string.Empty)
								{
									using (RegistryKey registryKey5 = registryKey2.OpenSubKey("ProgId", true))
									{
										if (registryKey5 != null)
										{
											registryKey5.DeleteValue("", false);
											if (registryKey5.SubKeyCount == 0 && registryKey5.ValueCount == 0)
											{
												registryKey2.DeleteSubKey("ProgId");
											}
										}
									}
								}
								using (RegistryKey registryKey6 = registryKey2.OpenSubKey("Implemented Categories", true))
								{
									if (registryKey6 != null)
									{
										using (RegistryKey registryKey7 = registryKey6.OpenSubKey("{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}", true))
										{
											if (registryKey7 != null && registryKey7.SubKeyCount == 0 && registryKey7.ValueCount == 0)
											{
												registryKey6.DeleteSubKey("{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}");
											}
										}
										if (registryKey6.SubKeyCount == 0 && registryKey6.ValueCount == 0)
										{
											registryKey2.DeleteSubKey("Implemented Categories");
										}
									}
								}
							}
							if (registryKey2.SubKeyCount == 0 && registryKey2.ValueCount == 0)
							{
								registryKey.DeleteSubKey(text);
							}
						}
					}
					if (registryKey.SubKeyCount == 0 && registryKey.ValueCount == 0)
					{
						Registry.ClassesRoot.DeleteSubKey("CLSID");
					}
				}
				if (flag && progIdForType != string.Empty)
				{
					using (RegistryKey registryKey8 = Registry.ClassesRoot.OpenSubKey(progIdForType, true))
					{
						if (registryKey8 != null)
						{
							registryKey8.DeleteValue("", false);
							using (RegistryKey registryKey9 = registryKey8.OpenSubKey("CLSID", true))
							{
								if (registryKey9 != null)
								{
									registryKey9.DeleteValue("", false);
									if (registryKey9.SubKeyCount == 0 && registryKey9.ValueCount == 0)
									{
										registryKey8.DeleteSubKey("CLSID");
									}
								}
							}
							if (registryKey8.SubKeyCount == 0 && registryKey8.ValueCount == 0)
							{
								Registry.ClassesRoot.DeleteSubKey(progIdForType);
							}
						}
					}
				}
			}
			return flag;
		}

		// Token: 0x06006252 RID: 25170 RVA: 0x00150808 File Offset: 0x0014EA08
		[SecurityCritical]
		private bool UnregisterComImportedType(Type type, string strAsmVersion)
		{
			bool result = true;
			string text = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey("CLSID", true))
			{
				if (registryKey != null)
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(text, true))
					{
						if (registryKey2 != null)
						{
							using (RegistryKey registryKey3 = registryKey2.OpenSubKey("InprocServer32", true))
							{
								if (registryKey3 != null)
								{
									registryKey3.DeleteValue("Assembly", false);
									registryKey3.DeleteValue("Class", false);
									registryKey3.DeleteValue("RuntimeVersion", false);
									registryKey3.DeleteValue("CodeBase", false);
									using (RegistryKey registryKey4 = registryKey3.OpenSubKey(strAsmVersion, true))
									{
										if (registryKey4 != null)
										{
											registryKey4.DeleteValue("Assembly", false);
											registryKey4.DeleteValue("Class", false);
											registryKey4.DeleteValue("RuntimeVersion", false);
											registryKey4.DeleteValue("CodeBase", false);
											if (registryKey4.SubKeyCount == 0 && registryKey4.ValueCount == 0)
											{
												registryKey3.DeleteSubKey(strAsmVersion);
											}
										}
									}
									if (registryKey3.SubKeyCount != 0)
									{
										result = false;
									}
									if (registryKey3.SubKeyCount == 0 && registryKey3.ValueCount == 0)
									{
										registryKey2.DeleteSubKey("InprocServer32");
									}
								}
							}
							if (registryKey2.SubKeyCount == 0 && registryKey2.ValueCount == 0)
							{
								registryKey.DeleteSubKey(text);
							}
						}
					}
					if (registryKey.SubKeyCount == 0 && registryKey.ValueCount == 0)
					{
						Registry.ClassesRoot.DeleteSubKey("CLSID");
					}
				}
			}
			return result;
		}

		// Token: 0x06006253 RID: 25171 RVA: 0x00150A10 File Offset: 0x0014EC10
		[SecurityCritical]
		private void RegisterPrimaryInteropAssembly(RuntimeAssembly assembly, string strAsmCodeBase, PrimaryInteropAssemblyAttribute attr)
		{
			if (assembly.GetPublicKey().Length == 0)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_PIAMustBeStrongNamed"));
			}
			string subkey = "{" + Marshal.GetTypeLibGuidForAssembly(assembly).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			string subkey2 = attr.MajorVersion.ToString("x", CultureInfo.InvariantCulture) + "." + attr.MinorVersion.ToString("x", CultureInfo.InvariantCulture);
			using (RegistryKey registryKey = Registry.ClassesRoot.CreateSubKey("TypeLib"))
			{
				using (RegistryKey registryKey2 = registryKey.CreateSubKey(subkey))
				{
					using (RegistryKey registryKey3 = registryKey2.CreateSubKey(subkey2))
					{
						registryKey3.SetValue("PrimaryInteropAssemblyName", assembly.FullName);
						if (strAsmCodeBase != null)
						{
							registryKey3.SetValue("PrimaryInteropAssemblyCodeBase", strAsmCodeBase);
						}
					}
				}
			}
		}

		// Token: 0x06006254 RID: 25172 RVA: 0x00150B34 File Offset: 0x0014ED34
		[SecurityCritical]
		private void UnregisterPrimaryInteropAssembly(Assembly assembly, PrimaryInteropAssemblyAttribute attr)
		{
			string text = "{" + Marshal.GetTypeLibGuidForAssembly(assembly).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			string text2 = attr.MajorVersion.ToString("x", CultureInfo.InvariantCulture) + "." + attr.MinorVersion.ToString("x", CultureInfo.InvariantCulture);
			using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey("TypeLib", true))
			{
				if (registryKey != null)
				{
					using (RegistryKey registryKey2 = registryKey.OpenSubKey(text, true))
					{
						if (registryKey2 != null)
						{
							using (RegistryKey registryKey3 = registryKey2.OpenSubKey(text2, true))
							{
								if (registryKey3 != null)
								{
									registryKey3.DeleteValue("PrimaryInteropAssemblyName", false);
									registryKey3.DeleteValue("PrimaryInteropAssemblyCodeBase", false);
									if (registryKey3.SubKeyCount == 0 && registryKey3.ValueCount == 0)
									{
										registryKey2.DeleteSubKey(text2);
									}
								}
							}
							if (registryKey2.SubKeyCount == 0 && registryKey2.ValueCount == 0)
							{
								registryKey.DeleteSubKey(text);
							}
						}
					}
					if (registryKey.SubKeyCount == 0 && registryKey.ValueCount == 0)
					{
						Registry.ClassesRoot.DeleteSubKey("TypeLib");
					}
				}
			}
		}

		// Token: 0x06006255 RID: 25173 RVA: 0x00150CA4 File Offset: 0x0014EEA4
		private void EnsureManagedCategoryExists()
		{
			if (!RegistrationServices.ManagedCategoryExists())
			{
				using (RegistryKey registryKey = Registry.ClassesRoot.CreateSubKey("Component Categories"))
				{
					using (RegistryKey registryKey2 = registryKey.CreateSubKey("{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}"))
					{
						registryKey2.SetValue("0", ".NET Category");
					}
				}
			}
		}

		// Token: 0x06006256 RID: 25174 RVA: 0x00150D18 File Offset: 0x0014EF18
		private static bool ManagedCategoryExists()
		{
			using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey("Component Categories", RegistryKeyPermissionCheck.ReadSubTree))
			{
				if (registryKey == null)
				{
					return false;
				}
				using (RegistryKey registryKey2 = registryKey.OpenSubKey("{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}", RegistryKeyPermissionCheck.ReadSubTree))
				{
					if (registryKey2 == null)
					{
						return false;
					}
					object value = registryKey2.GetValue("0");
					if (value == null || value.GetType() != typeof(string))
					{
						return false;
					}
					string a = (string)value;
					if (a != ".NET Category")
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06006257 RID: 25175 RVA: 0x00150DCC File Offset: 0x0014EFCC
		[SecurityCritical]
		private void CallUserDefinedRegistrationMethod(Type type, bool bRegister)
		{
			bool flag = false;
			Type typeFromHandle;
			if (bRegister)
			{
				typeFromHandle = typeof(ComRegisterFunctionAttribute);
			}
			else
			{
				typeFromHandle = typeof(ComUnregisterFunctionAttribute);
			}
			Type type2 = type;
			while (!flag && type2 != null)
			{
				MethodInfo[] methods = type2.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				int num = methods.Length;
				for (int i = 0; i < num; i++)
				{
					MethodInfo methodInfo = methods[i];
					if (methodInfo.GetCustomAttributes(typeFromHandle, true).Length != 0)
					{
						if (!methodInfo.IsStatic)
						{
							if (bRegister)
							{
								throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NonStaticComRegFunction", new object[] { methodInfo.Name, type2.Name }));
							}
							throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NonStaticComUnRegFunction", new object[] { methodInfo.Name, type2.Name }));
						}
						else
						{
							ParameterInfo[] parameters = methodInfo.GetParameters();
							if (methodInfo.ReturnType != typeof(void) || parameters == null || parameters.Length != 1 || (parameters[0].ParameterType != typeof(string) && parameters[0].ParameterType != typeof(Type)))
							{
								if (bRegister)
								{
									throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InvalidComRegFunctionSig", new object[] { methodInfo.Name, type2.Name }));
								}
								throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InvalidComUnRegFunctionSig", new object[] { methodInfo.Name, type2.Name }));
							}
							else if (flag)
							{
								if (bRegister)
								{
									throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MultipleComRegFunctions", new object[] { type2.Name }));
								}
								throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MultipleComUnRegFunctions", new object[] { type2.Name }));
							}
							else
							{
								object[] array = new object[1];
								if (parameters[0].ParameterType == typeof(string))
								{
									array[0] = "HKEY_CLASSES_ROOT\\CLSID\\{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
								}
								else
								{
									array[0] = type;
								}
								methodInfo.Invoke(null, array);
								flag = true;
							}
						}
					}
				}
				type2 = type2.BaseType;
			}
		}

		// Token: 0x06006258 RID: 25176 RVA: 0x0015100E File Offset: 0x0014F20E
		private Type GetBaseComImportType(Type type)
		{
			while (type != null && !type.IsImport)
			{
				type = type.BaseType;
			}
			return type;
		}

		// Token: 0x06006259 RID: 25177 RVA: 0x0015102C File Offset: 0x0014F22C
		private bool IsRegisteredAsValueType(Type type)
		{
			return type.IsValueType;
		}

		// Token: 0x0600625A RID: 25178
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void RegisterTypeForComClientsNative(Type type, ref Guid g);

		// Token: 0x0600625B RID: 25179
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern int RegisterTypeForComClientsExNative(Type t, RegistrationClassContext clsContext, RegistrationConnectionType flags);

		// Token: 0x0600625C RID: 25180
		[DllImport("ole32.dll", CharSet = CharSet.Auto, PreserveSig = false)]
		private static extern void CoRevokeClassObject(int cookie);

		// Token: 0x04002BD4 RID: 11220
		private const string strManagedCategoryGuid = "{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}";

		// Token: 0x04002BD5 RID: 11221
		private const string strDocStringPrefix = "";

		// Token: 0x04002BD6 RID: 11222
		private const string strManagedTypeThreadingModel = "Both";

		// Token: 0x04002BD7 RID: 11223
		private const string strComponentCategorySubKey = "Component Categories";

		// Token: 0x04002BD8 RID: 11224
		private const string strManagedCategoryDescription = ".NET Category";

		// Token: 0x04002BD9 RID: 11225
		private const string strImplementedCategoriesSubKey = "Implemented Categories";

		// Token: 0x04002BDA RID: 11226
		private const string strMsCorEEFileName = "mscoree.dll";

		// Token: 0x04002BDB RID: 11227
		private const string strRecordRootName = "Record";

		// Token: 0x04002BDC RID: 11228
		private const string strClsIdRootName = "CLSID";

		// Token: 0x04002BDD RID: 11229
		private const string strTlbRootName = "TypeLib";

		// Token: 0x04002BDE RID: 11230
		private static Guid s_ManagedCategoryGuid = new Guid("{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}");
	}
}
