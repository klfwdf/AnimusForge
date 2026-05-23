using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO.IsolatedStorage
{
	// Token: 0x020001B5 RID: 437
	[ComVisible(true)]
	public sealed class IsolatedStorageFile : IsolatedStorage, IDisposable
	{
		// Token: 0x06001B58 RID: 7000 RVA: 0x0005CA2C File Offset: 0x0005AC2C
		internal IsolatedStorageFile()
		{
		}

		// Token: 0x06001B59 RID: 7001 RVA: 0x0005CA3F File Offset: 0x0005AC3F
		public static IsolatedStorageFile GetUserStoreForDomain()
		{
			return IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly, null, null);
		}

		// Token: 0x06001B5A RID: 7002 RVA: 0x0005CA49 File Offset: 0x0005AC49
		public static IsolatedStorageFile GetUserStoreForAssembly()
		{
			return IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
		}

		// Token: 0x06001B5B RID: 7003 RVA: 0x0005CA53 File Offset: 0x0005AC53
		public static IsolatedStorageFile GetUserStoreForApplication()
		{
			return IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Application, null);
		}

		// Token: 0x06001B5C RID: 7004 RVA: 0x0005CA5D File Offset: 0x0005AC5D
		[ComVisible(false)]
		public static IsolatedStorageFile GetUserStoreForSite()
		{
			throw new NotSupportedException(Environment.GetResourceString("IsolatedStorage_NotValidOnDesktop"));
		}

		// Token: 0x06001B5D RID: 7005 RVA: 0x0005CA6E File Offset: 0x0005AC6E
		public static IsolatedStorageFile GetMachineStoreForDomain()
		{
			return IsolatedStorageFile.GetStore(IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine, null, null);
		}

		// Token: 0x06001B5E RID: 7006 RVA: 0x0005CA79 File Offset: 0x0005AC79
		public static IsolatedStorageFile GetMachineStoreForAssembly()
		{
			return IsolatedStorageFile.GetStore(IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine, null, null);
		}

		// Token: 0x06001B5F RID: 7007 RVA: 0x0005CA84 File Offset: 0x0005AC84
		public static IsolatedStorageFile GetMachineStoreForApplication()
		{
			return IsolatedStorageFile.GetStore(IsolatedStorageScope.Machine | IsolatedStorageScope.Application, null);
		}

		// Token: 0x06001B60 RID: 7008 RVA: 0x0005CA90 File Offset: 0x0005AC90
		[SecuritySafeCritical]
		public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Type domainEvidenceType, Type assemblyEvidenceType)
		{
			if (domainEvidenceType != null)
			{
				IsolatedStorageFile.DemandAdminPermission();
			}
			IsolatedStorageFile isolatedStorageFile = new IsolatedStorageFile();
			isolatedStorageFile.InitStore(scope, domainEvidenceType, assemblyEvidenceType);
			isolatedStorageFile.Init(scope);
			return isolatedStorageFile;
		}

		// Token: 0x06001B61 RID: 7009 RVA: 0x0005CAC2 File Offset: 0x0005ACC2
		internal void EnsureStoreIsValid()
		{
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
		}

		// Token: 0x06001B62 RID: 7010 RVA: 0x0005CAF8 File Offset: 0x0005ACF8
		[SecuritySafeCritical]
		public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, object domainIdentity, object assemblyIdentity)
		{
			if (assemblyIdentity == null)
			{
				throw new ArgumentNullException("assemblyIdentity");
			}
			if (IsolatedStorage.IsDomain(scope) && domainIdentity == null)
			{
				throw new ArgumentNullException("domainIdentity");
			}
			IsolatedStorageFile.DemandAdminPermission();
			IsolatedStorageFile isolatedStorageFile = new IsolatedStorageFile();
			isolatedStorageFile.InitStore(scope, domainIdentity, assemblyIdentity, null);
			isolatedStorageFile.Init(scope);
			return isolatedStorageFile;
		}

		// Token: 0x06001B63 RID: 7011 RVA: 0x0005CB48 File Offset: 0x0005AD48
		[SecuritySafeCritical]
		public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Evidence domainEvidence, Type domainEvidenceType, Evidence assemblyEvidence, Type assemblyEvidenceType)
		{
			if (assemblyEvidence == null)
			{
				throw new ArgumentNullException("assemblyEvidence");
			}
			if (IsolatedStorage.IsDomain(scope) && domainEvidence == null)
			{
				throw new ArgumentNullException("domainEvidence");
			}
			IsolatedStorageFile.DemandAdminPermission();
			IsolatedStorageFile isolatedStorageFile = new IsolatedStorageFile();
			isolatedStorageFile.InitStore(scope, domainEvidence, domainEvidenceType, assemblyEvidence, assemblyEvidenceType, null, null);
			isolatedStorageFile.Init(scope);
			return isolatedStorageFile;
		}

		// Token: 0x06001B64 RID: 7012 RVA: 0x0005CB9C File Offset: 0x0005AD9C
		[SecuritySafeCritical]
		public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Type applicationEvidenceType)
		{
			if (applicationEvidenceType != null)
			{
				IsolatedStorageFile.DemandAdminPermission();
			}
			IsolatedStorageFile isolatedStorageFile = new IsolatedStorageFile();
			isolatedStorageFile.InitStore(scope, applicationEvidenceType);
			isolatedStorageFile.Init(scope);
			return isolatedStorageFile;
		}

		// Token: 0x06001B65 RID: 7013 RVA: 0x0005CBD0 File Offset: 0x0005ADD0
		[SecuritySafeCritical]
		public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, object applicationIdentity)
		{
			if (applicationIdentity == null)
			{
				throw new ArgumentNullException("applicationIdentity");
			}
			IsolatedStorageFile.DemandAdminPermission();
			IsolatedStorageFile isolatedStorageFile = new IsolatedStorageFile();
			isolatedStorageFile.InitStore(scope, null, null, applicationIdentity);
			isolatedStorageFile.Init(scope);
			return isolatedStorageFile;
		}

		// Token: 0x1700031A RID: 794
		// (get) Token: 0x06001B66 RID: 7014 RVA: 0x0005CC08 File Offset: 0x0005AE08
		public override long UsedSize
		{
			[SecuritySafeCritical]
			get
			{
				if (base.IsRoaming())
				{
					throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_CurrentSizeUndefined"));
				}
				object internalLock = this.m_internalLock;
				long usage;
				lock (internalLock)
				{
					if (this.m_bDisposed)
					{
						throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
					}
					if (this.m_closed)
					{
						throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
					}
					if (this.InvalidFileHandle)
					{
						this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
					}
					usage = (long)IsolatedStorageFile.GetUsage(this.m_handle);
				}
				return usage;
			}
		}

		// Token: 0x1700031B RID: 795
		// (get) Token: 0x06001B67 RID: 7015 RVA: 0x0005CCB8 File Offset: 0x0005AEB8
		[CLSCompliant(false)]
		[Obsolete("IsolatedStorageFile.CurrentSize has been deprecated because it is not CLS Compliant.  To get the current size use IsolatedStorageFile.UsedSize")]
		public override ulong CurrentSize
		{
			[SecuritySafeCritical]
			get
			{
				if (base.IsRoaming())
				{
					throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_CurrentSizeUndefined"));
				}
				object internalLock = this.m_internalLock;
				ulong usage;
				lock (internalLock)
				{
					if (this.m_bDisposed)
					{
						throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
					}
					if (this.m_closed)
					{
						throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
					}
					if (this.InvalidFileHandle)
					{
						this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
					}
					usage = IsolatedStorageFile.GetUsage(this.m_handle);
				}
				return usage;
			}
		}

		// Token: 0x1700031C RID: 796
		// (get) Token: 0x06001B68 RID: 7016 RVA: 0x0005CD68 File Offset: 0x0005AF68
		[ComVisible(false)]
		public override long AvailableFreeSpace
		{
			[SecuritySafeCritical]
			get
			{
				if (base.IsRoaming())
				{
					return long.MaxValue;
				}
				object internalLock = this.m_internalLock;
				long usage;
				lock (internalLock)
				{
					if (this.m_bDisposed)
					{
						throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
					}
					if (this.m_closed)
					{
						throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
					}
					if (this.InvalidFileHandle)
					{
						this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
					}
					usage = (long)IsolatedStorageFile.GetUsage(this.m_handle);
				}
				return this.Quota - usage;
			}
		}

		// Token: 0x1700031D RID: 797
		// (get) Token: 0x06001B69 RID: 7017 RVA: 0x0005CE18 File Offset: 0x0005B018
		// (set) Token: 0x06001B6A RID: 7018 RVA: 0x0005CE34 File Offset: 0x0005B034
		[ComVisible(false)]
		public override long Quota
		{
			get
			{
				if (base.IsRoaming())
				{
					return long.MaxValue;
				}
				return base.Quota;
			}
			[SecuritySafeCritical]
			internal set
			{
				bool flag = false;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this.Lock(ref flag);
					object internalLock = this.m_internalLock;
					lock (internalLock)
					{
						if (this.InvalidFileHandle)
						{
							this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
						}
						IsolatedStorageFile.SetQuota(this.m_handle, value);
					}
				}
				finally
				{
					if (flag)
					{
						this.Unlock();
					}
				}
				base.Quota = value;
			}
		}

		// Token: 0x1700031E RID: 798
		// (get) Token: 0x06001B6B RID: 7019 RVA: 0x0005CEC8 File Offset: 0x0005B0C8
		[ComVisible(false)]
		public static bool IsEnabled
		{
			get
			{
				return true;
			}
		}

		// Token: 0x1700031F RID: 799
		// (get) Token: 0x06001B6C RID: 7020 RVA: 0x0005CECB File Offset: 0x0005B0CB
		[CLSCompliant(false)]
		[Obsolete("IsolatedStorageFile.MaximumSize has been deprecated because it is not CLS Compliant.  To get the maximum size use IsolatedStorageFile.Quota")]
		public override ulong MaximumSize
		{
			get
			{
				if (base.IsRoaming())
				{
					return 9223372036854775807UL;
				}
				return base.MaximumSize;
			}
		}

		// Token: 0x06001B6D RID: 7021 RVA: 0x0005CEE8 File Offset: 0x0005B0E8
		[SecuritySafeCritical]
		[ComVisible(false)]
		public override bool IncreaseQuotaTo(long newQuotaSize)
		{
			if (newQuotaSize <= this.Quota)
			{
				throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_OldQuotaLarger"));
			}
			if (this.m_StoreScope != (IsolatedStorageScope.User | IsolatedStorageScope.Application))
			{
				throw new NotSupportedException(Environment.GetResourceString("IsolatedStorage_OnlyIncreaseUserApplicationStore"));
			}
			IsolatedStorageSecurityState isolatedStorageSecurityState = IsolatedStorageSecurityState.CreateStateToIncreaseQuotaForApplication(newQuotaSize, this.Quota - this.AvailableFreeSpace);
			try
			{
				isolatedStorageSecurityState.EnsureState();
			}
			catch (IsolatedStorageException)
			{
				return false;
			}
			this.Quota = newQuotaSize;
			return true;
		}

		// Token: 0x06001B6E RID: 7022 RVA: 0x0005CF64 File Offset: 0x0005B164
		[SecuritySafeCritical]
		internal void Reserve(ulong lReserve)
		{
			if (base.IsRoaming())
			{
				return;
			}
			ulong quota = (ulong)this.Quota;
			object internalLock = this.m_internalLock;
			lock (internalLock)
			{
				if (this.m_bDisposed)
				{
					throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
				}
				if (this.m_closed)
				{
					throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
				}
				if (this.InvalidFileHandle)
				{
					this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
				}
				IsolatedStorageFile.Reserve(this.m_handle, quota, lReserve, false);
			}
		}

		// Token: 0x06001B6F RID: 7023 RVA: 0x0005D010 File Offset: 0x0005B210
		internal void Unreserve(ulong lFree)
		{
			if (base.IsRoaming())
			{
				return;
			}
			ulong quota = (ulong)this.Quota;
			this.Unreserve(lFree, quota);
		}

		// Token: 0x06001B70 RID: 7024 RVA: 0x0005D038 File Offset: 0x0005B238
		[SecuritySafeCritical]
		internal void Unreserve(ulong lFree, ulong quota)
		{
			if (base.IsRoaming())
			{
				return;
			}
			object internalLock = this.m_internalLock;
			lock (internalLock)
			{
				if (this.m_bDisposed)
				{
					throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
				}
				if (this.m_closed)
				{
					throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
				}
				if (this.InvalidFileHandle)
				{
					this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
				}
				IsolatedStorageFile.Reserve(this.m_handle, quota, lFree, true);
			}
		}

		// Token: 0x06001B71 RID: 7025 RVA: 0x0005D0DC File Offset: 0x0005B2DC
		[SecuritySafeCritical]
		public void DeleteFile(string file)
		{
			if (file == null)
			{
				throw new ArgumentNullException("file");
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			long num = 0L;
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this.Lock(ref flag);
				try
				{
					string fullPath = this.GetFullPath(file);
					num = LongPathFile.GetLength(fullPath);
					LongPathFile.Delete(fullPath);
				}
				catch
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DeleteFile"));
				}
				this.Unreserve(IsolatedStorageFile.RoundToBlockSize((ulong)num));
			}
			finally
			{
				if (flag)
				{
					this.Unlock();
				}
			}
			CodeAccessPermission.RevertAll();
		}

		// Token: 0x06001B72 RID: 7026 RVA: 0x0005D180 File Offset: 0x0005B380
		[SecuritySafeCritical]
		[ComVisible(false)]
		public bool FileExists(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string fullPath = this.GetFullPath(path);
			string text = LongPath.NormalizePath(fullPath);
			if (path.EndsWith(Path.DirectorySeparatorChar.ToString() + ".", StringComparison.Ordinal))
			{
				if (text.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				{
					text += ".";
				}
				else
				{
					text = text + Path.DirectorySeparatorChar.ToString() + ".";
				}
			}
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read, new string[] { text }, false, false));
			}
			catch
			{
				return false;
			}
			bool result = LongPathFile.Exists(text);
			CodeAccessPermission.RevertAll();
			return result;
		}

		// Token: 0x06001B73 RID: 7027 RVA: 0x0005D290 File Offset: 0x0005B490
		[SecuritySafeCritical]
		[ComVisible(false)]
		public bool DirectoryExists(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string fullPath = this.GetFullPath(path);
			string text = LongPath.NormalizePath(fullPath);
			if (fullPath.EndsWith(Path.DirectorySeparatorChar.ToString() + ".", StringComparison.Ordinal))
			{
				if (text.EndsWith(Path.DirectorySeparatorChar))
				{
					text += ".";
				}
				else
				{
					text = text + Path.DirectorySeparatorChar.ToString() + ".";
				}
			}
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read, new string[] { text }, false, false));
			}
			catch
			{
				return false;
			}
			bool result = LongPathDirectory.Exists(text);
			CodeAccessPermission.RevertAll();
			return result;
		}

		// Token: 0x06001B74 RID: 7028 RVA: 0x0005D398 File Offset: 0x0005B598
		[SecuritySafeCritical]
		public void CreateDirectory(string dir)
		{
			if (dir == null)
			{
				throw new ArgumentNullException("dir");
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string fullPath = this.GetFullPath(dir);
			string text = LongPath.NormalizePath(fullPath);
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read, new string[] { text }, false, false));
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_CreateDirectory"));
			}
			string[] array = this.DirectoriesToCreate(text);
			if (array != null && array.Length != 0)
			{
				this.Reserve((ulong)(1024L * (long)array.Length));
				try
				{
					LongPathDirectory.CreateDirectory(array[array.Length - 1]);
				}
				catch
				{
					this.Unreserve((ulong)(1024L * (long)array.Length));
					try
					{
						LongPathDirectory.Delete(array[0], true);
					}
					catch
					{
					}
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_CreateDirectory"));
				}
				CodeAccessPermission.RevertAll();
				return;
			}
			if (LongPathDirectory.Exists(fullPath))
			{
				return;
			}
			throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_CreateDirectory"));
		}

		// Token: 0x06001B75 RID: 7029 RVA: 0x0005D4A8 File Offset: 0x0005B6A8
		[SecuritySafeCritical]
		[ComVisible(false)]
		public DateTimeOffset GetCreationTime(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (path.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string fullPath = this.GetFullPath(path);
			string text = LongPath.NormalizePath(fullPath);
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read, new string[] { text }, false, false));
			}
			catch
			{
				return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
			}
			DateTimeOffset creationTime = LongPathFile.GetCreationTime(text);
			CodeAccessPermission.RevertAll();
			return creationTime;
		}

		// Token: 0x06001B76 RID: 7030 RVA: 0x0005D594 File Offset: 0x0005B794
		[SecuritySafeCritical]
		[ComVisible(false)]
		public DateTimeOffset GetLastAccessTime(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (path.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string fullPath = this.GetFullPath(path);
			string text = LongPath.NormalizePath(fullPath);
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read, new string[] { text }, false, false));
			}
			catch
			{
				return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
			}
			DateTimeOffset lastAccessTime = LongPathFile.GetLastAccessTime(text);
			CodeAccessPermission.RevertAll();
			return lastAccessTime;
		}

		// Token: 0x06001B77 RID: 7031 RVA: 0x0005D680 File Offset: 0x0005B880
		[SecuritySafeCritical]
		[ComVisible(false)]
		public DateTimeOffset GetLastWriteTime(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (path.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string fullPath = this.GetFullPath(path);
			string text = LongPath.NormalizePath(fullPath);
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read, new string[] { text }, false, false));
			}
			catch
			{
				return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
			}
			DateTimeOffset lastWriteTime = LongPathFile.GetLastWriteTime(text);
			CodeAccessPermission.RevertAll();
			return lastWriteTime;
		}

		// Token: 0x06001B78 RID: 7032 RVA: 0x0005D76C File Offset: 0x0005B96C
		[ComVisible(false)]
		public void CopyFile(string sourceFileName, string destinationFileName)
		{
			if (sourceFileName == null)
			{
				throw new ArgumentNullException("sourceFileName");
			}
			if (destinationFileName == null)
			{
				throw new ArgumentNullException("destinationFileName");
			}
			if (sourceFileName.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
			}
			if (destinationFileName.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
			}
			this.CopyFile(sourceFileName, destinationFileName, false);
		}

		// Token: 0x06001B79 RID: 7033 RVA: 0x0005D7E4 File Offset: 0x0005B9E4
		[SecuritySafeCritical]
		[ComVisible(false)]
		public void CopyFile(string sourceFileName, string destinationFileName, bool overwrite)
		{
			if (sourceFileName == null)
			{
				throw new ArgumentNullException("sourceFileName");
			}
			if (destinationFileName == null)
			{
				throw new ArgumentNullException("destinationFileName");
			}
			if (sourceFileName.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
			}
			if (destinationFileName.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string text = LongPath.NormalizePath(this.GetFullPath(sourceFileName));
			string text2 = LongPath.NormalizePath(this.GetFullPath(destinationFileName));
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, new string[] { text }, false, false));
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Write, new string[] { text2 }, false, false));
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
			}
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this.Lock(ref flag);
				long num = 0L;
				try
				{
					num = LongPathFile.GetLength(text);
				}
				catch (FileNotFoundException)
				{
					throw new FileNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", new object[] { sourceFileName }));
				}
				catch (DirectoryNotFoundException)
				{
					throw new DirectoryNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", new object[] { sourceFileName }));
				}
				catch
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
				}
				long num2 = 0L;
				if (LongPathFile.Exists(text2))
				{
					try
					{
						num2 = LongPathFile.GetLength(text2);
					}
					catch
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
					}
				}
				if (num2 < num)
				{
					this.Reserve(IsolatedStorageFile.RoundToBlockSize((ulong)(num - num2)));
				}
				try
				{
					LongPathFile.Copy(text, text2, overwrite);
				}
				catch (FileNotFoundException)
				{
					if (num2 < num)
					{
						this.Unreserve(IsolatedStorageFile.RoundToBlockSize((ulong)(num - num2)));
					}
					throw new FileNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", new object[] { sourceFileName }));
				}
				catch
				{
					if (num2 < num)
					{
						this.Unreserve(IsolatedStorageFile.RoundToBlockSize((ulong)(num - num2)));
					}
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
				}
				if (num2 > num && overwrite)
				{
					this.Unreserve(IsolatedStorageFile.RoundToBlockSizeFloor((ulong)(num2 - num)));
				}
			}
			finally
			{
				if (flag)
				{
					this.Unlock();
				}
			}
		}

		// Token: 0x06001B7A RID: 7034 RVA: 0x0005DAD8 File Offset: 0x0005BCD8
		[SecuritySafeCritical]
		[ComVisible(false)]
		public void MoveFile(string sourceFileName, string destinationFileName)
		{
			if (sourceFileName == null)
			{
				throw new ArgumentNullException("sourceFileName");
			}
			if (destinationFileName == null)
			{
				throw new ArgumentNullException("destinationFileName");
			}
			if (sourceFileName.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
			}
			if (destinationFileName.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string text = LongPath.NormalizePath(this.GetFullPath(sourceFileName));
			string text2 = LongPath.NormalizePath(this.GetFullPath(destinationFileName));
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, new string[] { text }, false, false));
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Write, new string[] { text2 }, false, false));
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
			}
			try
			{
				LongPathFile.Move(text, text2);
			}
			catch (FileNotFoundException)
			{
				throw new FileNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", new object[] { sourceFileName }));
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
			}
			CodeAccessPermission.RevertAll();
		}

		// Token: 0x06001B7B RID: 7035 RVA: 0x0005DC4C File Offset: 0x0005BE4C
		[SecuritySafeCritical]
		[ComVisible(false)]
		public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName)
		{
			if (sourceDirectoryName == null)
			{
				throw new ArgumentNullException("sourceDirectoryName");
			}
			if (destinationDirectoryName == null)
			{
				throw new ArgumentNullException("destinationDirectoryName");
			}
			if (sourceDirectoryName.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceDirectoryName");
			}
			if (destinationDirectoryName.Trim().Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationDirectoryName");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string text = LongPath.NormalizePath(this.GetFullPath(sourceDirectoryName));
			string text2 = LongPath.NormalizePath(this.GetFullPath(destinationDirectoryName));
			try
			{
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, new string[] { text }, false, false));
				IsolatedStorageFile.Demand(new FileIOPermission(FileIOPermissionAccess.Write, new string[] { text2 }, false, false));
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
			}
			try
			{
				LongPathDirectory.Move(text, text2);
			}
			catch (DirectoryNotFoundException)
			{
				throw new DirectoryNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", new object[] { sourceDirectoryName }));
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
			}
			CodeAccessPermission.RevertAll();
		}

		// Token: 0x06001B7C RID: 7036 RVA: 0x0005DDC0 File Offset: 0x0005BFC0
		[SecurityCritical]
		private string[] DirectoriesToCreate(string fullPath)
		{
			List<string> list = new List<string>();
			int num = fullPath.Length;
			if (num >= 2 && fullPath[num - 1] == this.SeparatorExternal)
			{
				num--;
			}
			int i = LongPath.GetRootLength(fullPath);
			while (i < num)
			{
				i++;
				while (i < num && fullPath[i] != this.SeparatorExternal)
				{
					i++;
				}
				string text = fullPath.Substring(0, i);
				if (!LongPathDirectory.InternalExists(text))
				{
					list.Add(text);
				}
			}
			if (list.Count != 0)
			{
				return list.ToArray();
			}
			return null;
		}

		// Token: 0x06001B7D RID: 7037 RVA: 0x0005DE48 File Offset: 0x0005C048
		[SecuritySafeCritical]
		public void DeleteDirectory(string dir)
		{
			if (dir == null)
			{
				throw new ArgumentNullException("dir");
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this.Lock(ref flag);
				try
				{
					string text = LongPath.NormalizePath(this.GetFullPath(dir));
					if (text.Equals(LongPath.NormalizePath(this.GetFullPath(".")), StringComparison.Ordinal))
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DeleteDirectory"));
					}
					LongPathDirectory.Delete(text, false);
				}
				catch
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DeleteDirectory"));
				}
				this.Unreserve(1024UL);
			}
			finally
			{
				if (flag)
				{
					this.Unlock();
				}
			}
			CodeAccessPermission.RevertAll();
		}

		// Token: 0x06001B7E RID: 7038 RVA: 0x0005DF14 File Offset: 0x0005C114
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void Demand(CodeAccessPermission permission)
		{
			permission.Demand();
		}

		// Token: 0x06001B7F RID: 7039 RVA: 0x0005DF1C File Offset: 0x0005C11C
		[ComVisible(false)]
		public string[] GetFileNames()
		{
			return this.GetFileNames("*");
		}

		// Token: 0x06001B80 RID: 7040 RVA: 0x0005DF2C File Offset: 0x0005C12C
		[SecuritySafeCritical]
		public string[] GetFileNames(string searchPattern)
		{
			if (searchPattern == null)
			{
				throw new ArgumentNullException("searchPattern");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string[] fileDirectoryNames = IsolatedStorageFile.GetFileDirectoryNames(this.GetFullPath(searchPattern), searchPattern, true);
			CodeAccessPermission.RevertAll();
			return fileDirectoryNames;
		}

		// Token: 0x06001B81 RID: 7041 RVA: 0x0005DFA3 File Offset: 0x0005C1A3
		[ComVisible(false)]
		public string[] GetDirectoryNames()
		{
			return this.GetDirectoryNames("*");
		}

		// Token: 0x06001B82 RID: 7042 RVA: 0x0005DFB0 File Offset: 0x0005C1B0
		[SecuritySafeCritical]
		public string[] GetDirectoryNames(string searchPattern)
		{
			if (searchPattern == null)
			{
				throw new ArgumentNullException("searchPattern");
			}
			if (this.m_bDisposed)
			{
				throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			if (this.m_closed)
			{
				throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
			}
			this.m_fiop.Assert();
			this.m_fiop.PermitOnly();
			string[] fileDirectoryNames = IsolatedStorageFile.GetFileDirectoryNames(this.GetFullPath(searchPattern), searchPattern, false);
			CodeAccessPermission.RevertAll();
			return fileDirectoryNames;
		}

		// Token: 0x06001B83 RID: 7043 RVA: 0x0005E028 File Offset: 0x0005C228
		private static string NormalizeSearchPattern(string searchPattern)
		{
			string text = searchPattern.TrimEnd(Path.TrimEndChars);
			Path.CheckSearchPattern(text);
			return text;
		}

		// Token: 0x06001B84 RID: 7044 RVA: 0x0005E048 File Offset: 0x0005C248
		[ComVisible(false)]
		public IsolatedStorageFileStream OpenFile(string path, FileMode mode)
		{
			return new IsolatedStorageFileStream(path, mode, this);
		}

		// Token: 0x06001B85 RID: 7045 RVA: 0x0005E052 File Offset: 0x0005C252
		[ComVisible(false)]
		public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access)
		{
			return new IsolatedStorageFileStream(path, mode, access, this);
		}

		// Token: 0x06001B86 RID: 7046 RVA: 0x0005E05D File Offset: 0x0005C25D
		[ComVisible(false)]
		public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
		{
			return new IsolatedStorageFileStream(path, mode, access, share, this);
		}

		// Token: 0x06001B87 RID: 7047 RVA: 0x0005E06A File Offset: 0x0005C26A
		[ComVisible(false)]
		public IsolatedStorageFileStream CreateFile(string path)
		{
			return new IsolatedStorageFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, this);
		}

		// Token: 0x06001B88 RID: 7048 RVA: 0x0005E078 File Offset: 0x0005C278
		[SecuritySafeCritical]
		public override void Remove()
		{
			string text = null;
			this.RemoveLogicalDir();
			this.Close();
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(IsolatedStorageFile.GetRootDir(base.Scope));
			if (base.IsApp())
			{
				stringBuilder.Append(base.AppName);
				stringBuilder.Append(this.SeparatorExternal);
			}
			else
			{
				if (base.IsDomain())
				{
					stringBuilder.Append(base.DomainName);
					stringBuilder.Append(this.SeparatorExternal);
					text = stringBuilder.ToString();
				}
				stringBuilder.Append(base.AssemName);
				stringBuilder.Append(this.SeparatorExternal);
			}
			string text2 = stringBuilder.ToString();
			new FileIOPermission(FileIOPermissionAccess.AllAccess, text2).Assert();
			if (this.ContainsUnknownFiles(text2))
			{
				return;
			}
			try
			{
				LongPathDirectory.Delete(text2, true);
			}
			catch
			{
				return;
			}
			if (base.IsDomain())
			{
				CodeAccessPermission.RevertAssert();
				new FileIOPermission(FileIOPermissionAccess.AllAccess, text).Assert();
				if (!this.ContainsUnknownFiles(text))
				{
					try
					{
						LongPathDirectory.Delete(text, true);
					}
					catch
					{
					}
				}
			}
		}

		// Token: 0x06001B89 RID: 7049 RVA: 0x0005E188 File Offset: 0x0005C388
		[SecuritySafeCritical]
		private void RemoveLogicalDir()
		{
			this.m_fiop.Assert();
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this.Lock(ref flag);
				if (Directory.Exists(this.RootDirectory))
				{
					ulong lFree = (ulong)(base.IsRoaming() ? 0L : (this.Quota - this.AvailableFreeSpace));
					ulong quota = (ulong)this.Quota;
					try
					{
						LongPathDirectory.Delete(this.RootDirectory, true);
					}
					catch
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DeleteDirectories"));
					}
					this.Unreserve(lFree, quota);
				}
			}
			finally
			{
				if (flag)
				{
					this.Unlock();
				}
			}
		}

		// Token: 0x06001B8A RID: 7050 RVA: 0x0005E230 File Offset: 0x0005C430
		private bool ContainsUnknownFiles(string rootDir)
		{
			string[] fileDirectoryNames;
			string[] fileDirectoryNames2;
			try
			{
				fileDirectoryNames = IsolatedStorageFile.GetFileDirectoryNames(rootDir + "*", "*", true);
				fileDirectoryNames2 = IsolatedStorageFile.GetFileDirectoryNames(rootDir + "*", "*", false);
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DeleteDirectories"));
			}
			if (fileDirectoryNames2 != null && fileDirectoryNames2.Length != 0)
			{
				if (fileDirectoryNames2.Length > 1)
				{
					return true;
				}
				if (base.IsApp())
				{
					if (IsolatedStorageFile.NotAppFilesDir(fileDirectoryNames2[0]))
					{
						return true;
					}
				}
				else if (base.IsDomain())
				{
					if (IsolatedStorageFile.NotFilesDir(fileDirectoryNames2[0]))
					{
						return true;
					}
				}
				else if (IsolatedStorageFile.NotAssemFilesDir(fileDirectoryNames2[0]))
				{
					return true;
				}
			}
			if (fileDirectoryNames == null || fileDirectoryNames.Length == 0)
			{
				return false;
			}
			if (base.IsRoaming())
			{
				return fileDirectoryNames.Length > 1 || IsolatedStorageFile.NotIDFile(fileDirectoryNames[0]);
			}
			return fileDirectoryNames.Length > 2 || (IsolatedStorageFile.NotIDFile(fileDirectoryNames[0]) && IsolatedStorageFile.NotInfoFile(fileDirectoryNames[0])) || (fileDirectoryNames.Length == 2 && IsolatedStorageFile.NotIDFile(fileDirectoryNames[1]) && IsolatedStorageFile.NotInfoFile(fileDirectoryNames[1]));
		}

		// Token: 0x06001B8B RID: 7051 RVA: 0x0005E330 File Offset: 0x0005C530
		[SecuritySafeCritical]
		public void Close()
		{
			if (base.IsRoaming())
			{
				return;
			}
			object internalLock = this.m_internalLock;
			lock (internalLock)
			{
				if (!this.m_closed)
				{
					this.m_closed = true;
					if (this.m_handle != null)
					{
						this.m_handle.Dispose();
					}
					GC.SuppressFinalize(this);
				}
			}
		}

		// Token: 0x06001B8C RID: 7052 RVA: 0x0005E39C File Offset: 0x0005C59C
		public void Dispose()
		{
			this.Close();
			this.m_bDisposed = true;
		}

		// Token: 0x06001B8D RID: 7053 RVA: 0x0005E3AC File Offset: 0x0005C5AC
		~IsolatedStorageFile()
		{
			this.Dispose();
		}

		// Token: 0x06001B8E RID: 7054 RVA: 0x0005E3D8 File Offset: 0x0005C5D8
		private static bool NotIDFile(string file)
		{
			return string.Compare(file, "identity.dat", StringComparison.Ordinal) != 0;
		}

		// Token: 0x06001B8F RID: 7055 RVA: 0x0005E3E9 File Offset: 0x0005C5E9
		private static bool NotInfoFile(string file)
		{
			return string.Compare(file, "info.dat", StringComparison.Ordinal) != 0 && string.Compare(file, "appinfo.dat", StringComparison.Ordinal) != 0;
		}

		// Token: 0x06001B90 RID: 7056 RVA: 0x0005E40A File Offset: 0x0005C60A
		private static bool NotFilesDir(string dir)
		{
			return string.Compare(dir, "Files", StringComparison.Ordinal) != 0;
		}

		// Token: 0x06001B91 RID: 7057 RVA: 0x0005E41B File Offset: 0x0005C61B
		internal static bool NotAssemFilesDir(string dir)
		{
			return string.Compare(dir, "AssemFiles", StringComparison.Ordinal) != 0;
		}

		// Token: 0x06001B92 RID: 7058 RVA: 0x0005E42C File Offset: 0x0005C62C
		internal static bool NotAppFilesDir(string dir)
		{
			return string.Compare(dir, "AppFiles", StringComparison.Ordinal) != 0;
		}

		// Token: 0x06001B93 RID: 7059 RVA: 0x0005E440 File Offset: 0x0005C640
		[SecuritySafeCritical]
		public static void Remove(IsolatedStorageScope scope)
		{
			IsolatedStorageFile.VerifyGlobalScope(scope);
			IsolatedStorageFile.DemandAdminPermission();
			string rootDir = IsolatedStorageFile.GetRootDir(scope);
			new FileIOPermission(FileIOPermissionAccess.Write, rootDir).Assert();
			try
			{
				LongPathDirectory.Delete(rootDir, true);
				LongPathDirectory.CreateDirectory(rootDir);
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_DeleteDirectories"));
			}
		}

		// Token: 0x06001B94 RID: 7060 RVA: 0x0005E49C File Offset: 0x0005C69C
		[SecuritySafeCritical]
		public static IEnumerator GetEnumerator(IsolatedStorageScope scope)
		{
			IsolatedStorageFile.VerifyGlobalScope(scope);
			IsolatedStorageFile.DemandAdminPermission();
			return new IsolatedStorageFileEnumerator(scope);
		}

		// Token: 0x17000320 RID: 800
		// (get) Token: 0x06001B95 RID: 7061 RVA: 0x0005E4AF File Offset: 0x0005C6AF
		internal string RootDirectory
		{
			get
			{
				return this.m_RootDir;
			}
		}

		// Token: 0x06001B96 RID: 7062 RVA: 0x0005E4B8 File Offset: 0x0005C6B8
		internal string GetFullPath(string path)
		{
			if (path == string.Empty)
			{
				return this.RootDirectory;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.RootDirectory);
			if (path[0] == this.SeparatorExternal)
			{
				stringBuilder.Append(path.Substring(1));
			}
			else
			{
				stringBuilder.Append(path);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06001B97 RID: 7063 RVA: 0x0005E51C File Offset: 0x0005C71C
		[SecurityCritical]
		[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
		private static string GetDataDirectoryFromActivationContext()
		{
			if (IsolatedStorageFile.s_appDataDir == null)
			{
				ActivationContext activationContext = AppDomain.CurrentDomain.ActivationContext;
				if (activationContext == null)
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_ApplicationMissingIdentity"));
				}
				string text = activationContext.DataDirectory;
				if (text != null && text[text.Length - 1] != '\\')
				{
					text += "\\";
				}
				IsolatedStorageFile.s_appDataDir = text;
			}
			return IsolatedStorageFile.s_appDataDir;
		}

		// Token: 0x06001B98 RID: 7064 RVA: 0x0005E588 File Offset: 0x0005C788
		[SecuritySafeCritical]
		internal void Init(IsolatedStorageScope scope)
		{
			IsolatedStorageFile.GetGlobalFileIOPerm(scope).Assert();
			this.m_StoreScope = scope;
			StringBuilder stringBuilder = new StringBuilder();
			if (IsolatedStorage.IsApp(scope))
			{
				stringBuilder.Append(IsolatedStorageFile.GetRootDir(scope));
				if (IsolatedStorageFile.s_appDataDir == null)
				{
					stringBuilder.Append(base.AppName);
					stringBuilder.Append(this.SeparatorExternal);
				}
				try
				{
					LongPathDirectory.CreateDirectory(stringBuilder.ToString());
				}
				catch
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
				}
				this.CreateIDFile(stringBuilder.ToString(), IsolatedStorageScope.Application);
				this.m_InfoFile = stringBuilder.ToString() + "appinfo.dat";
				stringBuilder.Append("AppFiles");
			}
			else
			{
				stringBuilder.Append(IsolatedStorageFile.GetRootDir(scope));
				if (IsolatedStorage.IsDomain(scope))
				{
					stringBuilder.Append(base.DomainName);
					stringBuilder.Append(this.SeparatorExternal);
					try
					{
						LongPathDirectory.CreateDirectory(stringBuilder.ToString());
						this.CreateIDFile(stringBuilder.ToString(), IsolatedStorageScope.Domain);
					}
					catch
					{
						throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
					}
					this.m_InfoFile = stringBuilder.ToString() + "info.dat";
				}
				stringBuilder.Append(base.AssemName);
				stringBuilder.Append(this.SeparatorExternal);
				try
				{
					LongPathDirectory.CreateDirectory(stringBuilder.ToString());
					this.CreateIDFile(stringBuilder.ToString(), IsolatedStorageScope.Assembly);
				}
				catch
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
				}
				if (IsolatedStorage.IsDomain(scope))
				{
					stringBuilder.Append("Files");
				}
				else
				{
					this.m_InfoFile = stringBuilder.ToString() + "info.dat";
					stringBuilder.Append("AssemFiles");
				}
			}
			stringBuilder.Append(this.SeparatorExternal);
			string text = stringBuilder.ToString();
			try
			{
				LongPathDirectory.CreateDirectory(text);
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
			}
			this.m_RootDir = text;
			this.m_fiop = new FileIOPermission(FileIOPermissionAccess.AllAccess, text);
			if (scope == (IsolatedStorageScope.User | IsolatedStorageScope.Application))
			{
				this.UpdateQuotaFromInfoFile();
			}
		}

		// Token: 0x06001B99 RID: 7065 RVA: 0x0005E7AC File Offset: 0x0005C9AC
		[SecurityCritical]
		private void UpdateQuotaFromInfoFile()
		{
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				this.Lock(ref flag);
				object internalLock = this.m_internalLock;
				lock (internalLock)
				{
					if (this.InvalidFileHandle)
					{
						this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
					}
					long quota = 0L;
					if (IsolatedStorageFile.GetQuota(this.m_handle, out quota))
					{
						base.Quota = quota;
					}
				}
			}
			finally
			{
				if (flag)
				{
					this.Unlock();
				}
			}
		}

		// Token: 0x06001B9A RID: 7066 RVA: 0x0005E844 File Offset: 0x0005CA44
		[SecuritySafeCritical]
		internal bool InitExistingStore(IsolatedStorageScope scope)
		{
			StringBuilder stringBuilder = new StringBuilder();
			this.m_StoreScope = scope;
			stringBuilder.Append(IsolatedStorageFile.GetRootDir(scope));
			if (IsolatedStorage.IsApp(scope))
			{
				stringBuilder.Append(base.AppName);
				stringBuilder.Append(this.SeparatorExternal);
				this.m_InfoFile = stringBuilder.ToString() + "appinfo.dat";
				stringBuilder.Append("AppFiles");
			}
			else
			{
				if (IsolatedStorage.IsDomain(scope))
				{
					stringBuilder.Append(base.DomainName);
					stringBuilder.Append(this.SeparatorExternal);
					this.m_InfoFile = stringBuilder.ToString() + "info.dat";
				}
				stringBuilder.Append(base.AssemName);
				stringBuilder.Append(this.SeparatorExternal);
				if (IsolatedStorage.IsDomain(scope))
				{
					stringBuilder.Append("Files");
				}
				else
				{
					this.m_InfoFile = stringBuilder.ToString() + "info.dat";
					stringBuilder.Append("AssemFiles");
				}
			}
			stringBuilder.Append(this.SeparatorExternal);
			FileIOPermission fileIOPermission = new FileIOPermission(FileIOPermissionAccess.AllAccess, stringBuilder.ToString());
			fileIOPermission.Assert();
			if (!LongPathDirectory.Exists(stringBuilder.ToString()))
			{
				return false;
			}
			this.m_RootDir = stringBuilder.ToString();
			this.m_fiop = fileIOPermission;
			if (scope == (IsolatedStorageScope.User | IsolatedStorageScope.Application))
			{
				this.UpdateQuotaFromInfoFile();
			}
			return true;
		}

		// Token: 0x06001B9B RID: 7067 RVA: 0x0005E98D File Offset: 0x0005CB8D
		protected override IsolatedStoragePermission GetPermission(PermissionSet ps)
		{
			if (ps == null)
			{
				return null;
			}
			if (ps.IsUnrestricted())
			{
				return new IsolatedStorageFilePermission(PermissionState.Unrestricted);
			}
			return (IsolatedStoragePermission)ps.GetPermission(typeof(IsolatedStorageFilePermission));
		}

		// Token: 0x06001B9C RID: 7068 RVA: 0x0005E9B8 File Offset: 0x0005CBB8
		internal void UndoReserveOperation(ulong oldLen, ulong newLen)
		{
			oldLen = IsolatedStorageFile.RoundToBlockSize(oldLen);
			if (newLen > oldLen)
			{
				this.Unreserve(IsolatedStorageFile.RoundToBlockSize(newLen - oldLen));
			}
		}

		// Token: 0x06001B9D RID: 7069 RVA: 0x0005E9D4 File Offset: 0x0005CBD4
		internal void Reserve(ulong oldLen, ulong newLen)
		{
			oldLen = IsolatedStorageFile.RoundToBlockSize(oldLen);
			if (newLen > oldLen)
			{
				this.Reserve(IsolatedStorageFile.RoundToBlockSize(newLen - oldLen));
			}
		}

		// Token: 0x06001B9E RID: 7070 RVA: 0x0005E9F0 File Offset: 0x0005CBF0
		internal void ReserveOneBlock()
		{
			this.Reserve(1024UL);
		}

		// Token: 0x06001B9F RID: 7071 RVA: 0x0005E9FE File Offset: 0x0005CBFE
		internal void UnreserveOneBlock()
		{
			this.Unreserve(1024UL);
		}

		// Token: 0x06001BA0 RID: 7072 RVA: 0x0005EA0C File Offset: 0x0005CC0C
		internal static ulong RoundToBlockSize(ulong num)
		{
			if (num < 1024UL)
			{
				return 1024UL;
			}
			ulong num2 = num % 1024UL;
			if (num2 != 0UL)
			{
				num += 1024UL - num2;
			}
			return num;
		}

		// Token: 0x06001BA1 RID: 7073 RVA: 0x0005EA44 File Offset: 0x0005CC44
		internal static ulong RoundToBlockSizeFloor(ulong num)
		{
			if (num < 1024UL)
			{
				return 0UL;
			}
			ulong num2 = num % 1024UL;
			num -= num2;
			return num;
		}

		// Token: 0x06001BA2 RID: 7074 RVA: 0x0005EA6C File Offset: 0x0005CC6C
		[SecurityCritical]
		internal static string GetRootDir(IsolatedStorageScope scope)
		{
			if (IsolatedStorage.IsRoaming(scope))
			{
				if (IsolatedStorageFile.s_RootDirRoaming == null)
				{
					string text = null;
					IsolatedStorageFile.GetRootDir(scope, JitHelpers.GetStringHandleOnStack(ref text));
					IsolatedStorageFile.s_RootDirRoaming = text;
				}
				return IsolatedStorageFile.s_RootDirRoaming;
			}
			if (IsolatedStorage.IsMachine(scope))
			{
				if (IsolatedStorageFile.s_RootDirMachine == null)
				{
					IsolatedStorageFile.InitGlobalsMachine(scope);
				}
				return IsolatedStorageFile.s_RootDirMachine;
			}
			if (IsolatedStorageFile.s_RootDirUser == null)
			{
				IsolatedStorageFile.InitGlobalsNonRoamingUser(scope);
			}
			return IsolatedStorageFile.s_RootDirUser;
		}

		// Token: 0x06001BA3 RID: 7075 RVA: 0x0005EAE0 File Offset: 0x0005CCE0
		[SecuritySafeCritical]
		[HandleProcessCorruptedStateExceptions]
		private static void InitGlobalsMachine(IsolatedStorageScope scope)
		{
			string text = null;
			IsolatedStorageFile.GetRootDir(scope, JitHelpers.GetStringHandleOnStack(ref text));
			new FileIOPermission(FileIOPermissionAccess.AllAccess, text).Assert();
			string text2 = IsolatedStorageFile.GetMachineRandomDirectory(text);
			if (text2 == null)
			{
				Mutex mutex = IsolatedStorageFile.CreateMutexNotOwned(text);
				if (!mutex.WaitOne())
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
				}
				try
				{
					text2 = IsolatedStorageFile.GetMachineRandomDirectory(text);
					if (text2 == null)
					{
						string randomFileName = Path.GetRandomFileName();
						string randomFileName2 = Path.GetRandomFileName();
						try
						{
							IsolatedStorageFile.CreateDirectoryWithDacl(text + randomFileName);
							IsolatedStorageFile.CreateDirectoryWithDacl(text + randomFileName + "\\" + randomFileName2);
						}
						catch
						{
							throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
						}
						text2 = randomFileName + "\\" + randomFileName2;
					}
				}
				finally
				{
					mutex.ReleaseMutex();
				}
			}
			IsolatedStorageFile.s_RootDirMachine = text + text2 + "\\";
		}

		// Token: 0x06001BA4 RID: 7076 RVA: 0x0005EBC8 File Offset: 0x0005CDC8
		[SecuritySafeCritical]
		private static void InitGlobalsNonRoamingUser(IsolatedStorageScope scope)
		{
			string text = null;
			if (scope == (IsolatedStorageScope.User | IsolatedStorageScope.Application))
			{
				text = IsolatedStorageFile.GetDataDirectoryFromActivationContext();
				if (text != null)
				{
					IsolatedStorageFile.s_RootDirUser = text;
					return;
				}
			}
			IsolatedStorageFile.GetRootDir(scope, JitHelpers.GetStringHandleOnStack(ref text));
			new FileIOPermission(FileIOPermissionAccess.AllAccess, text).Assert();
			bool flag = false;
			string oldRandomDirectory = null;
			string text2 = IsolatedStorageFile.GetRandomDirectory(text, out flag, out oldRandomDirectory);
			if (text2 == null)
			{
				Mutex mutex = IsolatedStorageFile.CreateMutexNotOwned(text);
				if (!mutex.WaitOne())
				{
					throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
				}
				try
				{
					text2 = IsolatedStorageFile.GetRandomDirectory(text, out flag, out oldRandomDirectory);
					if (text2 == null)
					{
						if (flag)
						{
							text2 = IsolatedStorageFile.MigrateOldIsoStoreDirectory(text, oldRandomDirectory);
						}
						else
						{
							text2 = IsolatedStorageFile.CreateRandomDirectory(text);
						}
					}
				}
				finally
				{
					mutex.ReleaseMutex();
				}
			}
			IsolatedStorageFile.s_RootDirUser = text + text2 + "\\";
		}

		// Token: 0x17000321 RID: 801
		// (get) Token: 0x06001BA5 RID: 7077 RVA: 0x0005EC8C File Offset: 0x0005CE8C
		internal bool Disposed
		{
			get
			{
				return this.m_bDisposed;
			}
		}

		// Token: 0x17000322 RID: 802
		// (get) Token: 0x06001BA6 RID: 7078 RVA: 0x0005EC94 File Offset: 0x0005CE94
		private bool InvalidFileHandle
		{
			[SecuritySafeCritical]
			get
			{
				return this.m_handle == null || this.m_handle.IsClosed || this.m_handle.IsInvalid;
			}
		}

		// Token: 0x06001BA7 RID: 7079 RVA: 0x0005ECB8 File Offset: 0x0005CEB8
		[SecuritySafeCritical]
		[HandleProcessCorruptedStateExceptions]
		internal static string MigrateOldIsoStoreDirectory(string rootDir, string oldRandomDirectory)
		{
			string randomFileName = Path.GetRandomFileName();
			string randomFileName2 = Path.GetRandomFileName();
			string text = rootDir + randomFileName;
			string destDirName = text + "\\" + randomFileName2;
			try
			{
				LongPathDirectory.CreateDirectory(text);
				LongPathDirectory.Move(rootDir + oldRandomDirectory, destDirName);
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
			}
			return randomFileName + "\\" + randomFileName2;
		}

		// Token: 0x06001BA8 RID: 7080 RVA: 0x0005ED28 File Offset: 0x0005CF28
		[SecuritySafeCritical]
		[HandleProcessCorruptedStateExceptions]
		internal static string CreateRandomDirectory(string rootDir)
		{
			string text;
			string path;
			do
			{
				text = Path.GetRandomFileName() + "\\" + Path.GetRandomFileName();
				path = rootDir + text;
			}
			while (LongPathDirectory.Exists(path));
			try
			{
				LongPathDirectory.CreateDirectory(path);
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
			}
			return text;
		}

		// Token: 0x06001BA9 RID: 7081 RVA: 0x0005ED88 File Offset: 0x0005CF88
		internal static string GetRandomDirectory(string rootDir, out bool bMigrateNeeded, out string sOldStoreLocation)
		{
			bMigrateNeeded = false;
			sOldStoreLocation = null;
			string[] fileDirectoryNames = IsolatedStorageFile.GetFileDirectoryNames(rootDir + "*", "*", false);
			for (int i = 0; i < fileDirectoryNames.Length; i++)
			{
				if (fileDirectoryNames[i].Length == 12)
				{
					string[] fileDirectoryNames2 = IsolatedStorageFile.GetFileDirectoryNames(rootDir + fileDirectoryNames[i] + "\\*", "*", false);
					for (int j = 0; j < fileDirectoryNames2.Length; j++)
					{
						if (fileDirectoryNames2[j].Length == 12)
						{
							return fileDirectoryNames[i] + "\\" + fileDirectoryNames2[j];
						}
					}
				}
			}
			for (int k = 0; k < fileDirectoryNames.Length; k++)
			{
				if (fileDirectoryNames[k].Length == 24)
				{
					bMigrateNeeded = true;
					sOldStoreLocation = fileDirectoryNames[k];
					return null;
				}
			}
			return null;
		}

		// Token: 0x06001BAA RID: 7082 RVA: 0x0005EE3C File Offset: 0x0005D03C
		internal static string GetMachineRandomDirectory(string rootDir)
		{
			string[] fileDirectoryNames = IsolatedStorageFile.GetFileDirectoryNames(rootDir + "*", "*", false);
			for (int i = 0; i < fileDirectoryNames.Length; i++)
			{
				if (fileDirectoryNames[i].Length == 12)
				{
					string[] fileDirectoryNames2 = IsolatedStorageFile.GetFileDirectoryNames(rootDir + fileDirectoryNames[i] + "\\*", "*", false);
					for (int j = 0; j < fileDirectoryNames2.Length; j++)
					{
						if (fileDirectoryNames2[j].Length == 12)
						{
							return fileDirectoryNames[i] + "\\" + fileDirectoryNames2[j];
						}
					}
				}
			}
			return null;
		}

		// Token: 0x06001BAB RID: 7083 RVA: 0x0005EEC0 File Offset: 0x0005D0C0
		[SecurityCritical]
		internal static Mutex CreateMutexNotOwned(string pathName)
		{
			return new Mutex(false, "Global\\" + IsolatedStorageFile.GetStrongHashSuitableForObjectName(pathName));
		}

		// Token: 0x06001BAC RID: 7084 RVA: 0x0005EED8 File Offset: 0x0005D0D8
		internal static string GetStrongHashSuitableForObjectName(string name)
		{
			MemoryStream memoryStream = new MemoryStream();
			new BinaryWriter(memoryStream).Write(name.ToUpper(CultureInfo.InvariantCulture));
			memoryStream.Position = 0L;
			return Path.ToBase32StringSuitableForDirName(new SHA1CryptoServiceProvider().ComputeHash(memoryStream));
		}

		// Token: 0x06001BAD RID: 7085 RVA: 0x0005EF19 File Offset: 0x0005D119
		private string GetSyncObjectName()
		{
			if (this.m_SyncObjectName == null)
			{
				this.m_SyncObjectName = IsolatedStorageFile.GetStrongHashSuitableForObjectName(this.m_InfoFile);
			}
			return this.m_SyncObjectName;
		}

		// Token: 0x06001BAE RID: 7086 RVA: 0x0005EF3C File Offset: 0x0005D13C
		[SecuritySafeCritical]
		internal void Lock(ref bool locked)
		{
			locked = false;
			if (base.IsRoaming())
			{
				return;
			}
			object internalLock = this.m_internalLock;
			lock (internalLock)
			{
				if (this.m_bDisposed)
				{
					throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
				}
				if (this.m_closed)
				{
					throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
				}
				if (this.InvalidFileHandle)
				{
					this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
				}
				locked = IsolatedStorageFile.Lock(this.m_handle, true);
			}
		}

		// Token: 0x06001BAF RID: 7087 RVA: 0x0005EFE0 File Offset: 0x0005D1E0
		[SecuritySafeCritical]
		internal void Unlock()
		{
			if (base.IsRoaming())
			{
				return;
			}
			object internalLock = this.m_internalLock;
			lock (internalLock)
			{
				if (this.m_bDisposed)
				{
					throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
				}
				if (this.m_closed)
				{
					throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
				}
				if (this.InvalidFileHandle)
				{
					this.m_handle = IsolatedStorageFile.Open(this.m_InfoFile, this.GetSyncObjectName());
				}
				IsolatedStorageFile.Lock(this.m_handle, false);
			}
		}

		// Token: 0x06001BB0 RID: 7088 RVA: 0x0005F080 File Offset: 0x0005D280
		[SecurityCritical]
		internal static FileIOPermission GetGlobalFileIOPerm(IsolatedStorageScope scope)
		{
			if (IsolatedStorage.IsRoaming(scope))
			{
				if (IsolatedStorageFile.s_PermRoaming == null)
				{
					IsolatedStorageFile.s_PermRoaming = new FileIOPermission(FileIOPermissionAccess.AllAccess, IsolatedStorageFile.GetRootDir(scope));
				}
				return IsolatedStorageFile.s_PermRoaming;
			}
			if (IsolatedStorage.IsMachine(scope))
			{
				if (IsolatedStorageFile.s_PermMachine == null)
				{
					IsolatedStorageFile.s_PermMachine = new FileIOPermission(FileIOPermissionAccess.AllAccess, IsolatedStorageFile.GetRootDir(scope));
				}
				return IsolatedStorageFile.s_PermMachine;
			}
			if (IsolatedStorageFile.s_PermUser == null)
			{
				IsolatedStorageFile.s_PermUser = new FileIOPermission(FileIOPermissionAccess.AllAccess, IsolatedStorageFile.GetRootDir(scope));
			}
			return IsolatedStorageFile.s_PermUser;
		}

		// Token: 0x06001BB1 RID: 7089 RVA: 0x0005F10B File Offset: 0x0005D30B
		[SecurityCritical]
		private static void DemandAdminPermission()
		{
			if (IsolatedStorageFile.s_PermAdminUser == null)
			{
				IsolatedStorageFile.s_PermAdminUser = new IsolatedStorageFilePermission(IsolatedStorageContainment.AdministerIsolatedStorageByUser, 0L, false);
			}
			IsolatedStorageFile.s_PermAdminUser.Demand();
		}

		// Token: 0x06001BB2 RID: 7090 RVA: 0x0005F133 File Offset: 0x0005D333
		internal static void VerifyGlobalScope(IsolatedStorageScope scope)
		{
			if (scope != IsolatedStorageScope.User && scope != (IsolatedStorageScope.User | IsolatedStorageScope.Roaming) && scope != IsolatedStorageScope.Machine)
			{
				throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_Scope_U_R_M"));
			}
		}

		// Token: 0x06001BB3 RID: 7091 RVA: 0x0005F154 File Offset: 0x0005D354
		[SecuritySafeCritical]
		internal void CreateIDFile(string path, IsolatedStorageScope scope)
		{
			try
			{
				using (FileStream fileStream = new FileStream(path + "identity.dat", FileMode.OpenOrCreate))
				{
					MemoryStream identityStream = base.GetIdentityStream(scope);
					byte[] buffer = identityStream.GetBuffer();
					fileStream.Write(buffer, 0, (int)identityStream.Length);
					identityStream.Close();
				}
			}
			catch
			{
			}
		}

		// Token: 0x06001BB4 RID: 7092 RVA: 0x0005F1C4 File Offset: 0x0005D3C4
		[SecuritySafeCritical]
		internal static string[] GetFileDirectoryNames(string path, string userSearchPattern, bool file)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path", Environment.GetResourceString("ArgumentNull_Path"));
			}
			userSearchPattern = IsolatedStorageFile.NormalizeSearchPattern(userSearchPattern);
			if (userSearchPattern.Length == 0)
			{
				return new string[0];
			}
			bool flag = false;
			char c = path[path.Length - 1];
			if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar || c == '.')
			{
				flag = true;
			}
			string text = LongPath.NormalizePath(path);
			if (flag && text[text.Length - 1] != c)
			{
				text += "\\*";
			}
			string text2 = LongPath.GetDirectoryName(text);
			if (text2 != null)
			{
				text2 += "\\";
			}
			try
			{
				new FileIOPermission(FileIOPermissionAccess.Read, new string[] { (text2 == null) ? text : text2 }, false, false).Demand();
			}
			catch
			{
				throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
			}
			string[] array = new string[10];
			int num = 0;
			Win32Native.WIN32_FIND_DATA win32_FIND_DATA = default(Win32Native.WIN32_FIND_DATA);
			SafeFindHandle safeFindHandle = Win32Native.FindFirstFile(Path.AddLongPathPrefix(text), ref win32_FIND_DATA);
			int lastWin32Error;
			if (safeFindHandle.IsInvalid)
			{
				lastWin32Error = Marshal.GetLastWin32Error();
				if (lastWin32Error == 2)
				{
					return new string[0];
				}
				__Error.WinIOError(lastWin32Error, userSearchPattern);
			}
			int num2 = 0;
			do
			{
				bool flag2;
				if (file)
				{
					flag2 = win32_FIND_DATA.IsFile;
				}
				else
				{
					flag2 = win32_FIND_DATA.IsNormalDirectory;
				}
				if (flag2)
				{
					num2++;
					if (num == array.Length)
					{
						Array.Resize<string>(ref array, 2 * array.Length);
					}
					array[num++] = win32_FIND_DATA.cFileName;
				}
			}
			while (Win32Native.FindNextFile(safeFindHandle, ref win32_FIND_DATA));
			lastWin32Error = Marshal.GetLastWin32Error();
			safeFindHandle.Close();
			if (lastWin32Error != 0 && lastWin32Error != 18)
			{
				__Error.WinIOError(lastWin32Error, userSearchPattern);
			}
			if (!file && num2 == 1 && (win32_FIND_DATA.dwFileAttributes & 16) != 0)
			{
				return new string[] { win32_FIND_DATA.cFileName };
			}
			if (num == array.Length)
			{
				return array;
			}
			Array.Resize<string>(ref array, num);
			return array;
		}

		// Token: 0x06001BB5 RID: 7093
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern ulong GetUsage(SafeIsolatedStorageFileHandle handle);

		// Token: 0x06001BB6 RID: 7094
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern SafeIsolatedStorageFileHandle Open(string infoFile, string syncName);

		// Token: 0x06001BB7 RID: 7095
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void Reserve(SafeIsolatedStorageFileHandle handle, ulong plQuota, ulong plReserve, [MarshalAs(UnmanagedType.Bool)] bool fFree);

		// Token: 0x06001BB8 RID: 7096
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void GetRootDir(IsolatedStorageScope scope, StringHandleOnStack retRootDir);

		// Token: 0x06001BB9 RID: 7097
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool Lock(SafeIsolatedStorageFileHandle handle, [MarshalAs(UnmanagedType.Bool)] bool fLock);

		// Token: 0x06001BBA RID: 7098
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void CreateDirectoryWithDacl(string path);

		// Token: 0x06001BBB RID: 7099
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetQuota(SafeIsolatedStorageFileHandle scope, out long quota);

		// Token: 0x06001BBC RID: 7100
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern void SetQuota(SafeIsolatedStorageFileHandle scope, long quota);

		// Token: 0x0400097A RID: 2426
		private const int s_BlockSize = 1024;

		// Token: 0x0400097B RID: 2427
		private const int s_DirSize = 1024;

		// Token: 0x0400097C RID: 2428
		private const string s_name = "file.store";

		// Token: 0x0400097D RID: 2429
		internal const string s_Files = "Files";

		// Token: 0x0400097E RID: 2430
		internal const string s_AssemFiles = "AssemFiles";

		// Token: 0x0400097F RID: 2431
		internal const string s_AppFiles = "AppFiles";

		// Token: 0x04000980 RID: 2432
		internal const string s_IDFile = "identity.dat";

		// Token: 0x04000981 RID: 2433
		internal const string s_InfoFile = "info.dat";

		// Token: 0x04000982 RID: 2434
		internal const string s_AppInfoFile = "appinfo.dat";

		// Token: 0x04000983 RID: 2435
		private static volatile string s_RootDirUser;

		// Token: 0x04000984 RID: 2436
		private static volatile string s_RootDirMachine;

		// Token: 0x04000985 RID: 2437
		private static volatile string s_RootDirRoaming;

		// Token: 0x04000986 RID: 2438
		private static volatile string s_appDataDir;

		// Token: 0x04000987 RID: 2439
		private static volatile FileIOPermission s_PermUser;

		// Token: 0x04000988 RID: 2440
		private static volatile FileIOPermission s_PermMachine;

		// Token: 0x04000989 RID: 2441
		private static volatile FileIOPermission s_PermRoaming;

		// Token: 0x0400098A RID: 2442
		private static volatile IsolatedStorageFilePermission s_PermAdminUser;

		// Token: 0x0400098B RID: 2443
		private FileIOPermission m_fiop;

		// Token: 0x0400098C RID: 2444
		private string m_RootDir;

		// Token: 0x0400098D RID: 2445
		private string m_InfoFile;

		// Token: 0x0400098E RID: 2446
		private string m_SyncObjectName;

		// Token: 0x0400098F RID: 2447
		[SecurityCritical]
		private SafeIsolatedStorageFileHandle m_handle;

		// Token: 0x04000990 RID: 2448
		private bool m_closed;

		// Token: 0x04000991 RID: 2449
		private bool m_bDisposed;

		// Token: 0x04000992 RID: 2450
		private object m_internalLock = new object();

		// Token: 0x04000993 RID: 2451
		private IsolatedStorageScope m_StoreScope;
	}
}
