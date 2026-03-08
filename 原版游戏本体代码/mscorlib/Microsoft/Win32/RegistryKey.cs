using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32
{
	// Token: 0x02000012 RID: 18
	[ComVisible(true)]
	public sealed class RegistryKey : MarshalByRefObject, IDisposable
	{
		// Token: 0x060000F6 RID: 246 RVA: 0x0000284C File Offset: 0x00000A4C
		[SecurityCritical]
		private RegistryKey(SafeRegistryHandle hkey, bool writable, RegistryView view)
			: this(hkey, writable, false, false, false, view)
		{
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000285C File Offset: 0x00000A5C
		[SecurityCritical]
		private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey, bool remoteKey, bool isPerfData, RegistryView view)
		{
			this.hkey = hkey;
			this.keyName = "";
			this.remoteKey = remoteKey;
			this.regView = view;
			if (systemkey)
			{
				this.state |= 2;
			}
			if (writable)
			{
				this.state |= 4;
			}
			if (isPerfData)
			{
				this.state |= 8;
			}
			RegistryKey.ValidateKeyView(view);
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x000028E0 File Offset: 0x00000AE0
		public void Close()
		{
			this.Dispose(true);
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x000028EC File Offset: 0x00000AEC
		[SecuritySafeCritical]
		private void Dispose(bool disposing)
		{
			if (this.hkey != null)
			{
				if (!this.IsSystemKey())
				{
					try
					{
						this.hkey.Dispose();
						return;
					}
					catch (IOException)
					{
						return;
					}
					finally
					{
						this.hkey = null;
					}
				}
				if (disposing && this.IsPerfDataKey())
				{
					SafeRegistryHandle.RegCloseKey(RegistryKey.HKEY_PERFORMANCE_DATA);
				}
			}
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000295C File Offset: 0x00000B5C
		[SecuritySafeCritical]
		public void Flush()
		{
			if (this.hkey != null && this.IsDirty())
			{
				Win32Native.RegFlushKey(this.hkey);
			}
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000297E File Offset: 0x00000B7E
		public void Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00002987 File Offset: 0x00000B87
		public RegistryKey CreateSubKey(string subkey)
		{
			return this.CreateSubKey(subkey, this.checkMode);
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00002998 File Offset: 0x00000B98
		[ComVisible(false)]
		public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck)
		{
			return this.CreateSubKeyInternal(subkey, permissionCheck, null, RegistryOptions.None);
		}

		// Token: 0x060000FE RID: 254 RVA: 0x000029A4 File Offset: 0x00000BA4
		[ComVisible(false)]
		public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions options)
		{
			return this.CreateSubKeyInternal(subkey, permissionCheck, null, options);
		}

		// Token: 0x060000FF RID: 255 RVA: 0x000029B0 File Offset: 0x00000BB0
		[ComVisible(false)]
		public RegistryKey CreateSubKey(string subkey, bool writable)
		{
			return this.CreateSubKeyInternal(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, null, RegistryOptions.None);
		}

		// Token: 0x06000100 RID: 256 RVA: 0x000029C2 File Offset: 0x00000BC2
		[ComVisible(false)]
		public RegistryKey CreateSubKey(string subkey, bool writable, RegistryOptions options)
		{
			return this.CreateSubKeyInternal(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, null, options);
		}

		// Token: 0x06000101 RID: 257 RVA: 0x000029D4 File Offset: 0x00000BD4
		[ComVisible(false)]
		public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistrySecurity registrySecurity)
		{
			return this.CreateSubKeyInternal(subkey, permissionCheck, registrySecurity, RegistryOptions.None);
		}

		// Token: 0x06000102 RID: 258 RVA: 0x000029E0 File Offset: 0x00000BE0
		[ComVisible(false)]
		public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions registryOptions, RegistrySecurity registrySecurity)
		{
			return this.CreateSubKeyInternal(subkey, permissionCheck, registrySecurity, registryOptions);
		}

		// Token: 0x06000103 RID: 259 RVA: 0x000029F0 File Offset: 0x00000BF0
		[SecuritySafeCritical]
		[ComVisible(false)]
		private unsafe RegistryKey CreateSubKeyInternal(string subkey, RegistryKeyPermissionCheck permissionCheck, object registrySecurityObj, RegistryOptions registryOptions)
		{
			RegistryKey.ValidateKeyOptions(registryOptions);
			RegistryKey.ValidateKeyName(subkey);
			RegistryKey.ValidateKeyMode(permissionCheck);
			this.EnsureWriteable();
			subkey = RegistryKey.FixupName(subkey);
			if (!this.remoteKey)
			{
				RegistryKey registryKey = this.InternalOpenSubKey(subkey, permissionCheck != RegistryKeyPermissionCheck.ReadSubTree);
				if (registryKey != null)
				{
					this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckSubKeyWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);
					this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckSubTreePermission, subkey, false, permissionCheck);
					registryKey.checkMode = permissionCheck;
					return registryKey;
				}
			}
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckSubKeyCreatePermission, subkey, false, RegistryKeyPermissionCheck.Default);
			Win32Native.SECURITY_ATTRIBUTES security_ATTRIBUTES = null;
			RegistrySecurity registrySecurity = (RegistrySecurity)registrySecurityObj;
			if (registrySecurity != null)
			{
				security_ATTRIBUTES = new Win32Native.SECURITY_ATTRIBUTES();
				security_ATTRIBUTES.nLength = Marshal.SizeOf<Win32Native.SECURITY_ATTRIBUTES>(security_ATTRIBUTES);
				byte[] securityDescriptorBinaryForm = registrySecurity.GetSecurityDescriptorBinaryForm();
				byte* ptr = stackalloc byte[(UIntPtr)securityDescriptorBinaryForm.Length];
				Buffer.Memcpy(ptr, 0, securityDescriptorBinaryForm, 0, securityDescriptorBinaryForm.Length);
				security_ATTRIBUTES.pSecurityDescriptor = ptr;
			}
			int num = 0;
			SafeRegistryHandle safeRegistryHandle = null;
			int num2 = Win32Native.RegCreateKeyEx(this.hkey, subkey, 0, null, (int)registryOptions, RegistryKey.GetRegistryKeyAccess(permissionCheck != RegistryKeyPermissionCheck.ReadSubTree) | (int)this.regView, security_ATTRIBUTES, out safeRegistryHandle, out num);
			if (num2 == 0 && !safeRegistryHandle.IsInvalid)
			{
				RegistryKey registryKey2 = new RegistryKey(safeRegistryHandle, permissionCheck != RegistryKeyPermissionCheck.ReadSubTree, false, this.remoteKey, false, this.regView);
				this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckSubTreePermission, subkey, false, permissionCheck);
				registryKey2.checkMode = permissionCheck;
				if (subkey.Length == 0)
				{
					registryKey2.keyName = this.keyName;
				}
				else
				{
					registryKey2.keyName = this.keyName + "\\" + subkey;
				}
				return registryKey2;
			}
			if (num2 != 0)
			{
				this.Win32Error(num2, this.keyName + "\\" + subkey);
			}
			return null;
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00002B7C File Offset: 0x00000D7C
		public void DeleteSubKey(string subkey)
		{
			this.DeleteSubKey(subkey, true);
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00002B88 File Offset: 0x00000D88
		[SecuritySafeCritical]
		public void DeleteSubKey(string subkey, bool throwOnMissingSubKey)
		{
			RegistryKey.ValidateKeyName(subkey);
			this.EnsureWriteable();
			subkey = RegistryKey.FixupName(subkey);
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckSubKeyWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);
			RegistryKey registryKey = this.InternalOpenSubKey(subkey, false);
			if (registryKey != null)
			{
				try
				{
					if (registryKey.InternalSubKeyCount() > 0)
					{
						ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_RegRemoveSubKey);
					}
				}
				finally
				{
					registryKey.Close();
				}
				int num;
				try
				{
					num = Win32Native.RegDeleteKeyEx(this.hkey, subkey, (int)this.regView, 0);
				}
				catch (EntryPointNotFoundException)
				{
					num = Win32Native.RegDeleteKey(this.hkey, subkey);
				}
				if (num != 0)
				{
					if (num != 2)
					{
						this.Win32Error(num, null);
						return;
					}
					if (throwOnMissingSubKey)
					{
						ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
						return;
					}
				}
			}
			else if (throwOnMissingSubKey)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
			}
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00002C48 File Offset: 0x00000E48
		public void DeleteSubKeyTree(string subkey)
		{
			this.DeleteSubKeyTree(subkey, true);
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00002C54 File Offset: 0x00000E54
		[SecuritySafeCritical]
		[ComVisible(false)]
		public void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey)
		{
			RegistryKey.ValidateKeyName(subkey);
			if (subkey.Length == 0 && this.IsSystemKey())
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyDelHive);
			}
			this.EnsureWriteable();
			subkey = RegistryKey.FixupName(subkey);
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckSubTreeWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);
			RegistryKey registryKey = this.InternalOpenSubKey(subkey, true);
			if (registryKey != null)
			{
				try
				{
					if (registryKey.InternalSubKeyCount() > 0)
					{
						string[] array = registryKey.InternalGetSubKeyNames();
						for (int i = 0; i < array.Length; i++)
						{
							registryKey.DeleteSubKeyTreeInternal(array[i]);
						}
					}
				}
				finally
				{
					registryKey.Close();
				}
				int num;
				try
				{
					num = Win32Native.RegDeleteKeyEx(this.hkey, subkey, (int)this.regView, 0);
				}
				catch (EntryPointNotFoundException)
				{
					num = Win32Native.RegDeleteKey(this.hkey, subkey);
				}
				if (num != 0)
				{
					this.Win32Error(num, null);
					return;
				}
			}
			else if (throwOnMissingSubKey)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
			}
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00002D30 File Offset: 0x00000F30
		[SecurityCritical]
		private void DeleteSubKeyTreeInternal(string subkey)
		{
			RegistryKey registryKey = this.InternalOpenSubKey(subkey, true);
			if (registryKey != null)
			{
				try
				{
					if (registryKey.InternalSubKeyCount() > 0)
					{
						string[] array = registryKey.InternalGetSubKeyNames();
						for (int i = 0; i < array.Length; i++)
						{
							registryKey.DeleteSubKeyTreeInternal(array[i]);
						}
					}
				}
				finally
				{
					registryKey.Close();
				}
				int num;
				try
				{
					num = Win32Native.RegDeleteKeyEx(this.hkey, subkey, (int)this.regView, 0);
				}
				catch (EntryPointNotFoundException)
				{
					num = Win32Native.RegDeleteKey(this.hkey, subkey);
				}
				if (num != 0)
				{
					this.Win32Error(num, null);
					return;
				}
			}
			else
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
			}
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00002DD8 File Offset: 0x00000FD8
		public void DeleteValue(string name)
		{
			this.DeleteValue(name, true);
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00002DE4 File Offset: 0x00000FE4
		[SecuritySafeCritical]
		public void DeleteValue(string name, bool throwOnMissingValue)
		{
			this.EnsureWriteable();
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckValueWritePermission, name, false, RegistryKeyPermissionCheck.Default);
			int num = Win32Native.RegDeleteValue(this.hkey, name);
			if ((num == 2 || num == 206) && throwOnMissingValue)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyValueAbsent);
			}
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00002E26 File Offset: 0x00001026
		[SecurityCritical]
		internal static RegistryKey GetBaseKey(IntPtr hKey)
		{
			return RegistryKey.GetBaseKey(hKey, RegistryView.Default);
		}

		// Token: 0x0600010C RID: 268 RVA: 0x00002E30 File Offset: 0x00001030
		[SecurityCritical]
		internal static RegistryKey GetBaseKey(IntPtr hKey, RegistryView view)
		{
			int num = (int)hKey & 268435455;
			bool flag = hKey == RegistryKey.HKEY_PERFORMANCE_DATA;
			SafeRegistryHandle safeRegistryHandle = new SafeRegistryHandle(hKey, flag);
			return new RegistryKey(safeRegistryHandle, true, true, false, flag, view)
			{
				checkMode = RegistryKeyPermissionCheck.Default,
				keyName = RegistryKey.hkeyNames[num]
			};
		}

		// Token: 0x0600010D RID: 269 RVA: 0x00002E83 File Offset: 0x00001083
		[SecuritySafeCritical]
		[ComVisible(false)]
		public static RegistryKey OpenBaseKey(RegistryHive hKey, RegistryView view)
		{
			RegistryKey.ValidateKeyView(view);
			RegistryKey.CheckUnmanagedCodePermission();
			return RegistryKey.GetBaseKey((IntPtr)((int)hKey), view);
		}

		// Token: 0x0600010E RID: 270 RVA: 0x00002E9C File Offset: 0x0000109C
		public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, string machineName)
		{
			return RegistryKey.OpenRemoteBaseKey(hKey, machineName, RegistryView.Default);
		}

		// Token: 0x0600010F RID: 271 RVA: 0x00002EA8 File Offset: 0x000010A8
		[SecuritySafeCritical]
		[ComVisible(false)]
		public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, string machineName, RegistryView view)
		{
			if (machineName == null)
			{
				throw new ArgumentNullException("machineName");
			}
			int num = (int)(hKey & (RegistryHive)268435455);
			if (num < 0 || num >= RegistryKey.hkeyNames.Length || ((long)hKey & (long)((ulong)(-16))) != (long)((ulong)(-2147483648)))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyOutOfRange"));
			}
			RegistryKey.ValidateKeyView(view);
			RegistryKey.CheckUnmanagedCodePermission();
			SafeRegistryHandle safeRegistryHandle = null;
			int num2 = Win32Native.RegConnectRegistry(machineName, new SafeRegistryHandle(new IntPtr((int)hKey), false), out safeRegistryHandle);
			if (num2 == 1114)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_DllInitFailure"));
			}
			if (num2 != 0)
			{
				RegistryKey.Win32ErrorStatic(num2, null);
			}
			if (safeRegistryHandle.IsInvalid)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyNoRemoteConnect", new object[] { machineName }));
			}
			return new RegistryKey(safeRegistryHandle, true, false, true, (IntPtr)((int)hKey) == RegistryKey.HKEY_PERFORMANCE_DATA, view)
			{
				checkMode = RegistryKeyPermissionCheck.Default,
				keyName = RegistryKey.hkeyNames[num]
			};
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00002F94 File Offset: 0x00001194
		[SecuritySafeCritical]
		public RegistryKey OpenSubKey(string name, bool writable)
		{
			RegistryKey.ValidateKeyName(name);
			this.EnsureNotDisposed();
			name = RegistryKey.FixupName(name);
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckOpenSubKeyWithWritablePermission, name, writable, RegistryKeyPermissionCheck.Default);
			SafeRegistryHandle safeRegistryHandle = null;
			int num = Win32Native.RegOpenKeyEx(this.hkey, name, 0, RegistryKey.GetRegistryKeyAccess(writable) | (int)this.regView, out safeRegistryHandle);
			if (num == 0 && !safeRegistryHandle.IsInvalid)
			{
				return new RegistryKey(safeRegistryHandle, writable, false, this.remoteKey, false, this.regView)
				{
					checkMode = this.GetSubKeyPermissonCheck(writable),
					keyName = this.keyName + "\\" + name
				};
			}
			if (num == 5 || num == 1346)
			{
				ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
			}
			return null;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00003048 File Offset: 0x00001248
		[SecuritySafeCritical]
		[ComVisible(false)]
		public RegistryKey OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck)
		{
			RegistryKey.ValidateKeyMode(permissionCheck);
			return this.InternalOpenSubKey(name, permissionCheck, RegistryKey.GetRegistryKeyAccess(permissionCheck));
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000305E File Offset: 0x0000125E
		[SecuritySafeCritical]
		[ComVisible(false)]
		public RegistryKey OpenSubKey(string name, RegistryRights rights)
		{
			return this.InternalOpenSubKey(name, this.checkMode, (int)rights);
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00003070 File Offset: 0x00001270
		[SecuritySafeCritical]
		[ComVisible(false)]
		public RegistryKey OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck, RegistryRights rights)
		{
			return this.InternalOpenSubKey(name, permissionCheck, (int)rights);
		}

		// Token: 0x06000114 RID: 276 RVA: 0x0000307C File Offset: 0x0000127C
		[SecurityCritical]
		private RegistryKey InternalOpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck, int rights)
		{
			RegistryKey.ValidateKeyName(name);
			RegistryKey.ValidateKeyMode(permissionCheck);
			RegistryKey.ValidateKeyRights(rights);
			this.EnsureNotDisposed();
			name = RegistryKey.FixupName(name);
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckOpenSubKeyPermission, name, false, permissionCheck);
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckSubTreePermission, name, false, permissionCheck);
			SafeRegistryHandle safeRegistryHandle = null;
			int num = Win32Native.RegOpenKeyEx(this.hkey, name, 0, rights | (int)this.regView, out safeRegistryHandle);
			if (num == 0 && !safeRegistryHandle.IsInvalid)
			{
				return new RegistryKey(safeRegistryHandle, permissionCheck == RegistryKeyPermissionCheck.ReadWriteSubTree, false, this.remoteKey, false, this.regView)
				{
					keyName = this.keyName + "\\" + name,
					checkMode = permissionCheck
				};
			}
			if (num == 5 || num == 1346)
			{
				ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
			}
			return null;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00003140 File Offset: 0x00001340
		[SecurityCritical]
		internal RegistryKey InternalOpenSubKey(string name, bool writable)
		{
			RegistryKey.ValidateKeyName(name);
			this.EnsureNotDisposed();
			SafeRegistryHandle safeRegistryHandle = null;
			if (Win32Native.RegOpenKeyEx(this.hkey, name, 0, RegistryKey.GetRegistryKeyAccess(writable) | (int)this.regView, out safeRegistryHandle) == 0 && !safeRegistryHandle.IsInvalid)
			{
				return new RegistryKey(safeRegistryHandle, writable, false, this.remoteKey, false, this.regView)
				{
					keyName = this.keyName + "\\" + name
				};
			}
			return null;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x000031BF File Offset: 0x000013BF
		public RegistryKey OpenSubKey(string name)
		{
			return this.OpenSubKey(name, false);
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000117 RID: 279 RVA: 0x000031C9 File Offset: 0x000013C9
		public int SubKeyCount
		{
			[SecuritySafeCritical]
			get
			{
				this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
				return this.InternalSubKeyCount();
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000118 RID: 280 RVA: 0x000031DC File Offset: 0x000013DC
		[ComVisible(false)]
		public RegistryView View
		{
			[SecuritySafeCritical]
			get
			{
				this.EnsureNotDisposed();
				return this.regView;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000119 RID: 281 RVA: 0x000031EC File Offset: 0x000013EC
		[ComVisible(false)]
		public SafeRegistryHandle Handle
		{
			[SecurityCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				this.EnsureNotDisposed();
				int num = 6;
				if (!this.IsSystemKey())
				{
					return this.hkey;
				}
				IntPtr hKey = (IntPtr)0;
				string text = this.keyName;
				uint num2 = <PrivateImplementationDetails>.ComputeStringHash(text);
				if (num2 <= 1097425318U)
				{
					if (num2 != 126972219U)
					{
						if (num2 != 457190004U)
						{
							if (num2 == 1097425318U)
							{
								if (text == "HKEY_CLASSES_ROOT")
								{
									hKey = RegistryKey.HKEY_CLASSES_ROOT;
									goto IL_13D;
								}
							}
						}
						else if (text == "HKEY_LOCAL_MACHINE")
						{
							hKey = RegistryKey.HKEY_LOCAL_MACHINE;
							goto IL_13D;
						}
					}
					else if (text == "HKEY_CURRENT_CONFIG")
					{
						hKey = RegistryKey.HKEY_CURRENT_CONFIG;
						goto IL_13D;
					}
				}
				else if (num2 <= 1568329430U)
				{
					if (num2 != 1198714601U)
					{
						if (num2 == 1568329430U)
						{
							if (text == "HKEY_CURRENT_USER")
							{
								hKey = RegistryKey.HKEY_CURRENT_USER;
								goto IL_13D;
							}
						}
					}
					else if (text == "HKEY_USERS")
					{
						hKey = RegistryKey.HKEY_USERS;
						goto IL_13D;
					}
				}
				else if (num2 != 2823865611U)
				{
					if (num2 == 3554990456U)
					{
						if (text == "HKEY_PERFORMANCE_DATA")
						{
							hKey = RegistryKey.HKEY_PERFORMANCE_DATA;
							goto IL_13D;
						}
					}
				}
				else if (text == "HKEY_DYN_DATA")
				{
					hKey = RegistryKey.HKEY_DYN_DATA;
					goto IL_13D;
				}
				this.Win32Error(num, null);
				IL_13D:
				SafeRegistryHandle safeRegistryHandle;
				num = Win32Native.RegOpenKeyEx(hKey, null, 0, RegistryKey.GetRegistryKeyAccess(this.IsWritable()) | (int)this.regView, out safeRegistryHandle);
				if (num == 0 && !safeRegistryHandle.IsInvalid)
				{
					return safeRegistryHandle;
				}
				this.Win32Error(num, null);
				throw new IOException(Win32Native.GetMessage(num), num);
			}
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00003381 File Offset: 0x00001581
		[SecurityCritical]
		[ComVisible(false)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static RegistryKey FromHandle(SafeRegistryHandle handle)
		{
			return RegistryKey.FromHandle(handle, RegistryView.Default);
		}

		// Token: 0x0600011B RID: 283 RVA: 0x0000338A File Offset: 0x0000158A
		[SecurityCritical]
		[ComVisible(false)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static RegistryKey FromHandle(SafeRegistryHandle handle, RegistryView view)
		{
			if (handle == null)
			{
				throw new ArgumentNullException("handle");
			}
			RegistryKey.ValidateKeyView(view);
			return new RegistryKey(handle, true, view);
		}

		// Token: 0x0600011C RID: 284 RVA: 0x000033A8 File Offset: 0x000015A8
		[SecurityCritical]
		internal int InternalSubKeyCount()
		{
			this.EnsureNotDisposed();
			int result = 0;
			int num = 0;
			int num2 = Win32Native.RegQueryInfoKey(this.hkey, null, null, IntPtr.Zero, ref result, null, null, ref num, null, null, null, null);
			if (num2 != 0)
			{
				this.Win32Error(num2, null);
			}
			return result;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x000033EA File Offset: 0x000015EA
		[SecuritySafeCritical]
		public string[] GetSubKeyNames()
		{
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
			return this.InternalGetSubKeyNames();
		}

		// Token: 0x0600011E RID: 286 RVA: 0x00003400 File Offset: 0x00001600
		[SecurityCritical]
		internal unsafe string[] InternalGetSubKeyNames()
		{
			this.EnsureNotDisposed();
			int num = this.InternalSubKeyCount();
			string[] array = new string[num];
			if (num > 0)
			{
				char[] array2 = new char[256];
				fixed (char* ptr = &array2[0])
				{
					char* ptr2 = ptr;
					for (int i = 0; i < num; i++)
					{
						int num2 = array2.Length;
						int num3 = Win32Native.RegEnumKeyEx(this.hkey, i, ptr2, ref num2, null, null, null, null);
						if (num3 != 0)
						{
							this.Win32Error(num3, null);
						}
						array[i] = new string(ptr2);
					}
				}
			}
			return array;
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600011F RID: 287 RVA: 0x00003488 File Offset: 0x00001688
		public int ValueCount
		{
			[SecuritySafeCritical]
			get
			{
				this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
				return this.InternalValueCount();
			}
		}

		// Token: 0x06000120 RID: 288 RVA: 0x0000349C File Offset: 0x0000169C
		[SecurityCritical]
		internal int InternalValueCount()
		{
			this.EnsureNotDisposed();
			int result = 0;
			int num = 0;
			int num2 = Win32Native.RegQueryInfoKey(this.hkey, null, null, IntPtr.Zero, ref num, null, null, ref result, null, null, null, null);
			if (num2 != 0)
			{
				this.Win32Error(num2, null);
			}
			return result;
		}

		// Token: 0x06000121 RID: 289 RVA: 0x000034E0 File Offset: 0x000016E0
		[SecuritySafeCritical]
		public unsafe string[] GetValueNames()
		{
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
			this.EnsureNotDisposed();
			int num = this.InternalValueCount();
			string[] array = new string[num];
			if (num > 0)
			{
				char[] array2 = new char[16384];
				fixed (char* ptr = &array2[0])
				{
					char* ptr2 = ptr;
					for (int i = 0; i < num; i++)
					{
						int num2 = array2.Length;
						int num3 = Win32Native.RegEnumValue(this.hkey, i, ptr2, ref num2, IntPtr.Zero, null, null, null);
						if (num3 != 0 && (!this.IsPerfDataKey() || num3 != 234))
						{
							this.Win32Error(num3, null);
						}
						array[i] = new string(ptr2);
					}
				}
			}
			return array;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00003588 File Offset: 0x00001788
		[SecuritySafeCritical]
		public object GetValue(string name)
		{
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
			return this.InternalGetValue(name, null, false, true);
		}

		// Token: 0x06000123 RID: 291 RVA: 0x0000359E File Offset: 0x0000179E
		[SecuritySafeCritical]
		public object GetValue(string name, object defaultValue)
		{
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
			return this.InternalGetValue(name, defaultValue, false, true);
		}

		// Token: 0x06000124 RID: 292 RVA: 0x000035B4 File Offset: 0x000017B4
		[SecuritySafeCritical]
		[ComVisible(false)]
		public object GetValue(string name, object defaultValue, RegistryValueOptions options)
		{
			if (options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)options }), "options");
			}
			bool doNotExpand = options == RegistryValueOptions.DoNotExpandEnvironmentNames;
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
			return this.InternalGetValue(name, defaultValue, doNotExpand, true);
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00003608 File Offset: 0x00001808
		[SecurityCritical]
		internal object InternalGetValue(string name, object defaultValue, bool doNotExpand, bool checkSecurity)
		{
			if (checkSecurity)
			{
				this.EnsureNotDisposed();
			}
			object obj = defaultValue;
			int num = 0;
			int num2 = 0;
			int num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, null, ref num2);
			if (num3 != 0)
			{
				if (this.IsPerfDataKey())
				{
					int num4 = 65000;
					int num5 = num4;
					byte[] array = new byte[num4];
					int num6;
					while (234 == (num6 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, array, ref num5)))
					{
						if (num4 == 2147483647)
						{
							this.Win32Error(num6, name);
						}
						else if (num4 > 1073741823)
						{
							num4 = int.MaxValue;
						}
						else
						{
							num4 *= 2;
						}
						num5 = num4;
						array = new byte[num4];
					}
					if (num6 != 0)
					{
						this.Win32Error(num6, name);
					}
					return array;
				}
				if (num3 != 234)
				{
					return obj;
				}
			}
			if (num2 < 0)
			{
				num2 = 0;
			}
			switch (num)
			{
			case 0:
			case 3:
			case 5:
				break;
			case 1:
			{
				char[] array2;
				checked
				{
					if (num2 % 2 == 1)
					{
						try
						{
							num2++;
						}
						catch (OverflowException innerException)
						{
							throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), innerException);
						}
					}
					array2 = new char[num2 / 2];
					num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, array2, ref num2);
				}
				if (array2.Length != 0 && array2[array2.Length - 1] == '\0')
				{
					return new string(array2, 0, array2.Length - 1);
				}
				return new string(array2);
			}
			case 2:
			{
				char[] array3;
				checked
				{
					if (num2 % 2 == 1)
					{
						try
						{
							num2++;
						}
						catch (OverflowException innerException2)
						{
							throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), innerException2);
						}
					}
					array3 = new char[num2 / 2];
					num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, array3, ref num2);
				}
				if (array3.Length != 0 && array3[array3.Length - 1] == '\0')
				{
					obj = new string(array3, 0, array3.Length - 1);
				}
				else
				{
					obj = new string(array3);
				}
				if (!doNotExpand)
				{
					return Environment.ExpandEnvironmentVariables((string)obj);
				}
				return obj;
			}
			case 4:
				if (num2 <= 4)
				{
					int num7 = 0;
					num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, ref num7, ref num2);
					return num7;
				}
				goto IL_122;
			case 6:
			case 8:
			case 9:
			case 10:
				return obj;
			case 7:
			{
				char[] array4;
				checked
				{
					if (num2 % 2 == 1)
					{
						try
						{
							num2++;
						}
						catch (OverflowException innerException3)
						{
							throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), innerException3);
						}
					}
					array4 = new char[num2 / 2];
					num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, array4, ref num2);
				}
				if (array4.Length != 0 && array4[array4.Length - 1] != '\0')
				{
					try
					{
						char[] array5 = new char[checked(array4.Length + 1)];
						for (int i = 0; i < array4.Length; i++)
						{
							array5[i] = array4[i];
						}
						array5[array5.Length - 1] = '\0';
						array4 = array5;
					}
					catch (OverflowException innerException4)
					{
						throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), innerException4);
					}
					array4[array4.Length - 1] = '\0';
				}
				IList<string> list = new List<string>();
				int num8 = 0;
				int num9 = array4.Length;
				while (num3 == 0 && num8 < num9)
				{
					int num10 = num8;
					while (num10 < num9 && array4[num10] != '\0')
					{
						num10++;
					}
					if (num10 < num9)
					{
						if (num10 - num8 > 0)
						{
							list.Add(new string(array4, num8, num10 - num8));
						}
						else if (num10 != num9 - 1)
						{
							list.Add(string.Empty);
						}
					}
					else
					{
						list.Add(new string(array4, num8, num9 - num8));
					}
					num8 = num10 + 1;
				}
				obj = new string[list.Count];
				list.CopyTo((string[])obj, 0);
				return obj;
			}
			case 11:
				goto IL_122;
			default:
				return obj;
			}
			IL_FC:
			byte[] array6 = new byte[num2];
			num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, array6, ref num2);
			return array6;
			IL_122:
			if (num2 > 8)
			{
				goto IL_FC;
			}
			long num11 = 0L;
			num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, ref num11, ref num2);
			obj = num11;
			return obj;
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00003A00 File Offset: 0x00001C00
		[SecuritySafeCritical]
		[ComVisible(false)]
		public RegistryValueKind GetValueKind(string name)
		{
			this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
			this.EnsureNotDisposed();
			int num = 0;
			int num2 = 0;
			int num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, null, ref num2);
			if (num3 != 0)
			{
				this.Win32Error(num3, null);
			}
			if (num == 0)
			{
				return RegistryValueKind.None;
			}
			if (!Enum.IsDefined(typeof(RegistryValueKind), num))
			{
				return RegistryValueKind.Unknown;
			}
			return (RegistryValueKind)num;
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00003A60 File Offset: 0x00001C60
		private bool IsDirty()
		{
			return (this.state & 1) != 0;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x00003A6F File Offset: 0x00001C6F
		private bool IsSystemKey()
		{
			return (this.state & 2) != 0;
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00003A7E File Offset: 0x00001C7E
		private bool IsWritable()
		{
			return (this.state & 4) != 0;
		}

		// Token: 0x0600012A RID: 298 RVA: 0x00003A8D File Offset: 0x00001C8D
		private bool IsPerfDataKey()
		{
			return (this.state & 8) != 0;
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600012B RID: 299 RVA: 0x00003A9C File Offset: 0x00001C9C
		public string Name
		{
			[SecuritySafeCritical]
			get
			{
				this.EnsureNotDisposed();
				return this.keyName;
			}
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00003AAC File Offset: 0x00001CAC
		private void SetDirty()
		{
			this.state |= 1;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00003AC0 File Offset: 0x00001CC0
		public void SetValue(string name, object value)
		{
			this.SetValue(name, value, RegistryValueKind.Unknown);
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00003ACC File Offset: 0x00001CCC
		[SecuritySafeCritical]
		[ComVisible(false)]
		public unsafe void SetValue(string name, object value, RegistryValueKind valueKind)
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
			}
			if (name != null && name.Length > 16383)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RegValStrLenBug"));
			}
			if (!Enum.IsDefined(typeof(RegistryValueKind), valueKind))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_RegBadKeyKind"), "valueKind");
			}
			this.EnsureWriteable();
			if (!this.remoteKey && this.ContainsRegistryValue(name))
			{
				this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckValueWritePermission, name, false, RegistryKeyPermissionCheck.Default);
			}
			else
			{
				this.CheckPermission(RegistryKey.RegistryInternalCheck.CheckValueCreatePermission, name, false, RegistryKeyPermissionCheck.Default);
			}
			if (valueKind == RegistryValueKind.Unknown)
			{
				valueKind = this.CalculateValueKind(value);
			}
			int num = 0;
			try
			{
				switch (valueKind)
				{
				case RegistryValueKind.None:
				case RegistryValueKind.Binary:
					break;
				case RegistryValueKind.Unknown:
				case (RegistryValueKind)5:
				case (RegistryValueKind)6:
				case (RegistryValueKind)8:
				case (RegistryValueKind)9:
				case (RegistryValueKind)10:
					goto IL_273;
				case RegistryValueKind.String:
				case RegistryValueKind.ExpandString:
				{
					string text = value.ToString();
					num = Win32Native.RegSetValueEx(this.hkey, name, 0, valueKind, text, checked(text.Length * 2 + 2));
					goto IL_273;
				}
				case RegistryValueKind.DWord:
				{
					int num2 = Convert.ToInt32(value, CultureInfo.InvariantCulture);
					num = Win32Native.RegSetValueEx(this.hkey, name, 0, RegistryValueKind.DWord, ref num2, 4);
					goto IL_273;
				}
				case RegistryValueKind.MultiString:
				{
					string[] array = (string[])((string[])value).Clone();
					int num3 = 0;
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i] == null)
						{
							ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetStrArrNull);
						}
						checked
						{
							num3 += (array[i].Length + 1) * 2;
						}
					}
					byte[] array2;
					checked
					{
						num3 += 2;
						array2 = new byte[num3];
					}
					try
					{
						byte[] array3;
						byte* value2;
						if ((array3 = array2) == null || array3.Length == 0)
						{
							value2 = null;
						}
						else
						{
							value2 = &array3[0];
						}
						IntPtr intPtr = new IntPtr((void*)value2);
						for (int j = 0; j < array.Length; j++)
						{
							string.InternalCopy(array[j], intPtr, checked(array[j].Length * 2));
							intPtr = new IntPtr((long)intPtr + (long)(checked(array[j].Length * 2)));
							*(short*)intPtr.ToPointer() = 0;
							intPtr = new IntPtr((long)intPtr + 2L);
						}
						*(short*)intPtr.ToPointer() = 0;
						intPtr = new IntPtr((long)intPtr + 2L);
						num = Win32Native.RegSetValueEx(this.hkey, name, 0, RegistryValueKind.MultiString, array2, num3);
						goto IL_273;
					}
					finally
					{
						byte[] array3 = null;
					}
					break;
				}
				case RegistryValueKind.QWord:
				{
					long num4 = Convert.ToInt64(value, CultureInfo.InvariantCulture);
					num = Win32Native.RegSetValueEx(this.hkey, name, 0, RegistryValueKind.QWord, ref num4, 8);
					goto IL_273;
				}
				default:
					goto IL_273;
				}
				byte[] array4 = (byte[])value;
				num = Win32Native.RegSetValueEx(this.hkey, name, 0, (valueKind == RegistryValueKind.None) ? RegistryValueKind.Unknown : RegistryValueKind.Binary, array4, array4.Length);
				IL_273:;
			}
			catch (OverflowException)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
			}
			catch (InvalidOperationException)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
			}
			catch (FormatException)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
			}
			catch (InvalidCastException)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
			}
			if (num == 0)
			{
				this.SetDirty();
				return;
			}
			this.Win32Error(num, null);
		}

		// Token: 0x0600012F RID: 303 RVA: 0x00003E04 File Offset: 0x00002004
		private RegistryValueKind CalculateValueKind(object value)
		{
			if (value is int)
			{
				return RegistryValueKind.DWord;
			}
			if (!(value is Array))
			{
				return RegistryValueKind.String;
			}
			if (value is byte[])
			{
				return RegistryValueKind.Binary;
			}
			if (value is string[])
			{
				return RegistryValueKind.MultiString;
			}
			throw new ArgumentException(Environment.GetResourceString("Arg_RegSetBadArrType", new object[] { value.GetType().Name }));
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00003E5C File Offset: 0x0000205C
		[SecuritySafeCritical]
		public override string ToString()
		{
			this.EnsureNotDisposed();
			return this.keyName;
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00003E6C File Offset: 0x0000206C
		public RegistrySecurity GetAccessControl()
		{
			return this.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00003E76 File Offset: 0x00002076
		[SecuritySafeCritical]
		public RegistrySecurity GetAccessControl(AccessControlSections includeSections)
		{
			this.EnsureNotDisposed();
			return new RegistrySecurity(this.hkey, this.keyName, includeSections);
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00003E94 File Offset: 0x00002094
		[SecuritySafeCritical]
		public void SetAccessControl(RegistrySecurity registrySecurity)
		{
			this.EnsureWriteable();
			if (registrySecurity == null)
			{
				throw new ArgumentNullException("registrySecurity");
			}
			registrySecurity.Persist(this.hkey, this.keyName);
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00003EC0 File Offset: 0x000020C0
		[SecuritySafeCritical]
		internal void Win32Error(int errorCode, string str)
		{
			switch (errorCode)
			{
			case 2:
				throw new IOException(Environment.GetResourceString("Arg_RegKeyNotFound"), errorCode);
			case 5:
				if (str != null)
				{
					throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", new object[] { str }));
				}
				throw new UnauthorizedAccessException();
			case 6:
				if (!this.IsPerfDataKey())
				{
					this.hkey.SetHandleAsInvalid();
					this.hkey = null;
				}
				break;
			}
			throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00003F4B File Offset: 0x0000214B
		[SecuritySafeCritical]
		internal static void Win32ErrorStatic(int errorCode, string str)
		{
			if (errorCode != 5)
			{
				throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
			}
			if (str != null)
			{
				throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", new object[] { str }));
			}
			throw new UnauthorizedAccessException();
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00003F80 File Offset: 0x00002180
		internal static string FixupName(string name)
		{
			if (name.IndexOf('\\') == -1)
			{
				return name;
			}
			StringBuilder stringBuilder = new StringBuilder(name);
			RegistryKey.FixupPath(stringBuilder);
			int num = stringBuilder.Length - 1;
			if (num >= 0 && stringBuilder[num] == '\\')
			{
				stringBuilder.Length = num;
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00003FCC File Offset: 0x000021CC
		private static void FixupPath(StringBuilder path)
		{
			int length = path.Length;
			bool flag = false;
			char maxValue = char.MaxValue;
			for (int i = 1; i < length - 1; i++)
			{
				if (path[i] == '\\')
				{
					i++;
					while (i < length && path[i] == '\\')
					{
						path[i] = maxValue;
						i++;
						flag = true;
					}
				}
			}
			if (flag)
			{
				int i = 0;
				int num = 0;
				while (i < length)
				{
					if (path[i] == maxValue)
					{
						i++;
					}
					else
					{
						path[num] = path[i];
						i++;
						num++;
					}
				}
				path.Length += num - i;
			}
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0000406C File Offset: 0x0000226C
		private void GetSubKeyReadPermission(string subkeyName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Read;
			path = this.keyName + "\\" + subkeyName + "\\.";
		}

		// Token: 0x06000139 RID: 313 RVA: 0x0000408B File Offset: 0x0000228B
		private void GetSubKeyWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Write;
			path = this.keyName + "\\" + subkeyName + "\\.";
		}

		// Token: 0x0600013A RID: 314 RVA: 0x000040AA File Offset: 0x000022AA
		private void GetSubKeyCreatePermission(string subkeyName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Create;
			path = this.keyName + "\\" + subkeyName + "\\.";
		}

		// Token: 0x0600013B RID: 315 RVA: 0x000040C9 File Offset: 0x000022C9
		private void GetSubTreeReadPermission(string subkeyName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Read;
			path = this.keyName + "\\" + subkeyName + "\\";
		}

		// Token: 0x0600013C RID: 316 RVA: 0x000040E8 File Offset: 0x000022E8
		private void GetSubTreeWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Write;
			path = this.keyName + "\\" + subkeyName + "\\";
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00004107 File Offset: 0x00002307
		private void GetSubTreeReadWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Read | RegistryPermissionAccess.Write;
			path = this.keyName + "\\" + subkeyName;
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00004121 File Offset: 0x00002321
		private void GetValueReadPermission(string valueName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Read;
			path = this.keyName + "\\" + valueName;
		}

		// Token: 0x0600013F RID: 319 RVA: 0x0000413B File Offset: 0x0000233B
		private void GetValueWritePermission(string valueName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Write;
			path = this.keyName + "\\" + valueName;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00004155 File Offset: 0x00002355
		private void GetValueCreatePermission(string valueName, out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Create;
			path = this.keyName + "\\" + valueName;
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0000416F File Offset: 0x0000236F
		private void GetKeyReadPermission(out RegistryPermissionAccess access, out string path)
		{
			access = RegistryPermissionAccess.Read;
			path = this.keyName + "\\.";
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00004188 File Offset: 0x00002388
		[SecurityCritical]
		private void CheckPermission(RegistryKey.RegistryInternalCheck check, string item, bool subKeyWritable, RegistryKeyPermissionCheck subKeyCheck)
		{
			bool flag = false;
			RegistryPermissionAccess access = RegistryPermissionAccess.NoAccess;
			string pathList = null;
			if (CodeAccessSecurityEngine.QuickCheckForAllDemands())
			{
				return;
			}
			switch (check)
			{
			case RegistryKey.RegistryInternalCheck.CheckSubKeyWritePermission:
				if (this.remoteKey)
				{
					RegistryKey.CheckUnmanagedCodePermission();
				}
				else if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					flag = true;
					this.GetSubKeyWritePermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckSubKeyReadPermission:
				if (this.remoteKey)
				{
					RegistryKey.CheckUnmanagedCodePermission();
				}
				else
				{
					flag = true;
					this.GetSubKeyReadPermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckSubKeyCreatePermission:
				if (this.remoteKey)
				{
					RegistryKey.CheckUnmanagedCodePermission();
				}
				else if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					flag = true;
					this.GetSubKeyCreatePermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckSubTreeReadPermission:
				if (this.remoteKey)
				{
					RegistryKey.CheckUnmanagedCodePermission();
				}
				else if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					flag = true;
					this.GetSubTreeReadPermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckSubTreeWritePermission:
				if (this.remoteKey)
				{
					RegistryKey.CheckUnmanagedCodePermission();
				}
				else if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					flag = true;
					this.GetSubTreeWritePermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckSubTreeReadWritePermission:
				if (this.remoteKey)
				{
					RegistryKey.CheckUnmanagedCodePermission();
				}
				else
				{
					flag = true;
					this.GetSubTreeReadWritePermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckValueWritePermission:
				if (this.remoteKey)
				{
					RegistryKey.CheckUnmanagedCodePermission();
				}
				else if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					flag = true;
					this.GetValueWritePermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckValueCreatePermission:
				if (this.remoteKey)
				{
					RegistryKey.CheckUnmanagedCodePermission();
				}
				else if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					flag = true;
					this.GetValueCreatePermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckValueReadPermission:
				if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					flag = true;
					this.GetValueReadPermission(item, out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckKeyReadPermission:
				if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					flag = true;
					this.GetKeyReadPermission(out access, out pathList);
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckSubTreePermission:
				if (subKeyCheck == RegistryKeyPermissionCheck.ReadSubTree)
				{
					if (this.checkMode == RegistryKeyPermissionCheck.Default)
					{
						if (this.remoteKey)
						{
							RegistryKey.CheckUnmanagedCodePermission();
						}
						else
						{
							flag = true;
							this.GetSubTreeReadPermission(item, out access, out pathList);
						}
					}
				}
				else if (subKeyCheck == RegistryKeyPermissionCheck.ReadWriteSubTree && this.checkMode != RegistryKeyPermissionCheck.ReadWriteSubTree)
				{
					if (this.remoteKey)
					{
						RegistryKey.CheckUnmanagedCodePermission();
					}
					else
					{
						flag = true;
						this.GetSubTreeReadWritePermission(item, out access, out pathList);
					}
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckOpenSubKeyWithWritablePermission:
				if (this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					if (this.remoteKey)
					{
						RegistryKey.CheckUnmanagedCodePermission();
					}
					else
					{
						flag = true;
						this.GetSubKeyReadPermission(item, out access, out pathList);
					}
				}
				else if (subKeyWritable && this.checkMode == RegistryKeyPermissionCheck.ReadSubTree)
				{
					if (this.remoteKey)
					{
						RegistryKey.CheckUnmanagedCodePermission();
					}
					else
					{
						flag = true;
						this.GetSubTreeReadWritePermission(item, out access, out pathList);
					}
				}
				break;
			case RegistryKey.RegistryInternalCheck.CheckOpenSubKeyPermission:
				if (subKeyCheck == RegistryKeyPermissionCheck.Default && this.checkMode == RegistryKeyPermissionCheck.Default)
				{
					if (this.remoteKey)
					{
						RegistryKey.CheckUnmanagedCodePermission();
					}
					else
					{
						flag = true;
						this.GetSubKeyReadPermission(item, out access, out pathList);
					}
				}
				break;
			}
			if (flag)
			{
				new RegistryPermission(access, pathList).Demand();
			}
		}

		// Token: 0x06000143 RID: 323 RVA: 0x000044A4 File Offset: 0x000026A4
		[SecurityCritical]
		private static void CheckUnmanagedCodePermission()
		{
			new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
		}

		// Token: 0x06000144 RID: 324 RVA: 0x000044B4 File Offset: 0x000026B4
		[SecurityCritical]
		private bool ContainsRegistryValue(string name)
		{
			int num = 0;
			int num2 = 0;
			int num3 = Win32Native.RegQueryValueEx(this.hkey, name, null, ref num, null, ref num2);
			return num3 == 0;
		}

		// Token: 0x06000145 RID: 325 RVA: 0x000044DE File Offset: 0x000026DE
		[SecurityCritical]
		private void EnsureNotDisposed()
		{
			if (this.hkey == null)
			{
				ThrowHelper.ThrowObjectDisposedException(this.keyName, ExceptionResource.ObjectDisposed_RegKeyClosed);
			}
		}

		// Token: 0x06000146 RID: 326 RVA: 0x000044F9 File Offset: 0x000026F9
		[SecurityCritical]
		private void EnsureWriteable()
		{
			this.EnsureNotDisposed();
			if (!this.IsWritable())
			{
				ThrowHelper.ThrowUnauthorizedAccessException(ExceptionResource.UnauthorizedAccess_RegistryNoWrite);
			}
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00004510 File Offset: 0x00002710
		private static int GetRegistryKeyAccess(bool isWritable)
		{
			int result;
			if (!isWritable)
			{
				result = 131097;
			}
			else
			{
				result = 131103;
			}
			return result;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00004530 File Offset: 0x00002730
		private static int GetRegistryKeyAccess(RegistryKeyPermissionCheck mode)
		{
			int result = 0;
			if (mode > RegistryKeyPermissionCheck.ReadSubTree)
			{
				if (mode == RegistryKeyPermissionCheck.ReadWriteSubTree)
				{
					result = 131103;
				}
			}
			else
			{
				result = 131097;
			}
			return result;
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00004558 File Offset: 0x00002758
		private RegistryKeyPermissionCheck GetSubKeyPermissonCheck(bool subkeyWritable)
		{
			if (this.checkMode == RegistryKeyPermissionCheck.Default)
			{
				return this.checkMode;
			}
			if (subkeyWritable)
			{
				return RegistryKeyPermissionCheck.ReadWriteSubTree;
			}
			return RegistryKeyPermissionCheck.ReadSubTree;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00004574 File Offset: 0x00002774
		private static void ValidateKeyName(string name)
		{
			if (name == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
			}
			int num = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
			int num2 = 0;
			while (num != -1)
			{
				if (num - num2 > 255)
				{
					ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);
				}
				num2 = num + 1;
				num = name.IndexOf("\\", num2, StringComparison.OrdinalIgnoreCase);
			}
			if (name.Length - num2 > 255)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);
			}
		}

		// Token: 0x0600014B RID: 331 RVA: 0x000045D9 File Offset: 0x000027D9
		private static void ValidateKeyMode(RegistryKeyPermissionCheck mode)
		{
			if (mode < RegistryKeyPermissionCheck.Default || mode > RegistryKeyPermissionCheck.ReadWriteSubTree)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryKeyPermissionCheck, ExceptionArgument.mode);
			}
		}

		// Token: 0x0600014C RID: 332 RVA: 0x000045EB File Offset: 0x000027EB
		private static void ValidateKeyOptions(RegistryOptions options)
		{
			if (options < RegistryOptions.None || options > RegistryOptions.Volatile)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryOptionsCheck, ExceptionArgument.options);
			}
		}

		// Token: 0x0600014D RID: 333 RVA: 0x000045FE File Offset: 0x000027FE
		private static void ValidateKeyView(RegistryView view)
		{
			if (view != RegistryView.Default && view != RegistryView.Registry32 && view != RegistryView.Registry64)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryViewCheck, ExceptionArgument.view);
			}
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0000461C File Offset: 0x0000281C
		private static void ValidateKeyRights(int rights)
		{
			if ((rights & -983104) != 0)
			{
				ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
			}
		}

		// Token: 0x04000180 RID: 384
		internal static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(int.MinValue);

		// Token: 0x04000181 RID: 385
		internal static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(-2147483647);

		// Token: 0x04000182 RID: 386
		internal static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(-2147483646);

		// Token: 0x04000183 RID: 387
		internal static readonly IntPtr HKEY_USERS = new IntPtr(-2147483645);

		// Token: 0x04000184 RID: 388
		internal static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(-2147483644);

		// Token: 0x04000185 RID: 389
		internal static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(-2147483643);

		// Token: 0x04000186 RID: 390
		internal static readonly IntPtr HKEY_DYN_DATA = new IntPtr(-2147483642);

		// Token: 0x04000187 RID: 391
		private const int STATE_DIRTY = 1;

		// Token: 0x04000188 RID: 392
		private const int STATE_SYSTEMKEY = 2;

		// Token: 0x04000189 RID: 393
		private const int STATE_WRITEACCESS = 4;

		// Token: 0x0400018A RID: 394
		private const int STATE_PERF_DATA = 8;

		// Token: 0x0400018B RID: 395
		private static readonly string[] hkeyNames = new string[] { "HKEY_CLASSES_ROOT", "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE", "HKEY_USERS", "HKEY_PERFORMANCE_DATA", "HKEY_CURRENT_CONFIG", "HKEY_DYN_DATA" };

		// Token: 0x0400018C RID: 396
		private const int MaxKeyLength = 255;

		// Token: 0x0400018D RID: 397
		private const int MaxValueLength = 16383;

		// Token: 0x0400018E RID: 398
		[SecurityCritical]
		private volatile SafeRegistryHandle hkey;

		// Token: 0x0400018F RID: 399
		private volatile int state;

		// Token: 0x04000190 RID: 400
		private volatile string keyName;

		// Token: 0x04000191 RID: 401
		private volatile bool remoteKey;

		// Token: 0x04000192 RID: 402
		private volatile RegistryKeyPermissionCheck checkMode;

		// Token: 0x04000193 RID: 403
		private volatile RegistryView regView;

		// Token: 0x04000194 RID: 404
		private const int FORMAT_MESSAGE_IGNORE_INSERTS = 512;

		// Token: 0x04000195 RID: 405
		private const int FORMAT_MESSAGE_FROM_SYSTEM = 4096;

		// Token: 0x04000196 RID: 406
		private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 8192;

		// Token: 0x02000ABD RID: 2749
		private enum RegistryInternalCheck
		{
			// Token: 0x040030AB RID: 12459
			CheckSubKeyWritePermission,
			// Token: 0x040030AC RID: 12460
			CheckSubKeyReadPermission,
			// Token: 0x040030AD RID: 12461
			CheckSubKeyCreatePermission,
			// Token: 0x040030AE RID: 12462
			CheckSubTreeReadPermission,
			// Token: 0x040030AF RID: 12463
			CheckSubTreeWritePermission,
			// Token: 0x040030B0 RID: 12464
			CheckSubTreeReadWritePermission,
			// Token: 0x040030B1 RID: 12465
			CheckValueWritePermission,
			// Token: 0x040030B2 RID: 12466
			CheckValueCreatePermission,
			// Token: 0x040030B3 RID: 12467
			CheckValueReadPermission,
			// Token: 0x040030B4 RID: 12468
			CheckKeyReadPermission,
			// Token: 0x040030B5 RID: 12469
			CheckSubTreePermission,
			// Token: 0x040030B6 RID: 12470
			CheckOpenSubKeyWithWritablePermission,
			// Token: 0x040030B7 RID: 12471
			CheckOpenSubKeyPermission
		}
	}
}
