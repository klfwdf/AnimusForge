using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
	// Token: 0x02000502 RID: 1282
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public sealed class Mutex : WaitHandle
	{
		// Token: 0x06003C7A RID: 15482 RVA: 0x000E4504 File Offset: 0x000E2704
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public Mutex(bool initiallyOwned, string name, out bool createdNew)
			: this(initiallyOwned, name, out createdNew, null)
		{
		}

		// Token: 0x06003C7B RID: 15483 RVA: 0x000E4510 File Offset: 0x000E2710
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public unsafe Mutex(bool initiallyOwned, string name, out bool createdNew, MutexSecurity mutexSecurity)
		{
			if (name != null && 260 < name.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", new object[] { name }));
			}
			Win32Native.SECURITY_ATTRIBUTES security_ATTRIBUTES = null;
			if (mutexSecurity != null)
			{
				security_ATTRIBUTES = new Win32Native.SECURITY_ATTRIBUTES();
				security_ATTRIBUTES.nLength = Marshal.SizeOf<Win32Native.SECURITY_ATTRIBUTES>(security_ATTRIBUTES);
				byte[] securityDescriptorBinaryForm = mutexSecurity.GetSecurityDescriptorBinaryForm();
				byte* ptr = stackalloc byte[(UIntPtr)securityDescriptorBinaryForm.Length];
				Buffer.Memcpy(ptr, 0, securityDescriptorBinaryForm, 0, securityDescriptorBinaryForm.Length);
				security_ATTRIBUTES.pSecurityDescriptor = ptr;
			}
			this.CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, security_ATTRIBUTES);
		}

		// Token: 0x06003C7C RID: 15484 RVA: 0x000E4591 File Offset: 0x000E2791
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal Mutex(bool initiallyOwned, string name, out bool createdNew, Win32Native.SECURITY_ATTRIBUTES secAttrs)
		{
			if (name != null && 260 < name.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", new object[] { name }));
			}
			this.CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, secAttrs);
		}

		// Token: 0x06003C7D RID: 15485 RVA: 0x000E45D0 File Offset: 0x000E27D0
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal void CreateMutexWithGuaranteedCleanup(bool initiallyOwned, string name, out bool createdNew, Win32Native.SECURITY_ATTRIBUTES secAttrs)
		{
			RuntimeHelpers.CleanupCode backoutCode = new RuntimeHelpers.CleanupCode(this.MutexCleanupCode);
			Mutex.MutexCleanupInfo mutexCleanupInfo = new Mutex.MutexCleanupInfo(null, false);
			Mutex.MutexTryCodeHelper mutexTryCodeHelper = new Mutex.MutexTryCodeHelper(initiallyOwned, mutexCleanupInfo, name, secAttrs, this);
			RuntimeHelpers.TryCode code = new RuntimeHelpers.TryCode(mutexTryCodeHelper.MutexTryCode);
			RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(code, backoutCode, mutexCleanupInfo);
			createdNew = mutexTryCodeHelper.m_newMutex;
		}

		// Token: 0x06003C7E RID: 15486 RVA: 0x000E461C File Offset: 0x000E281C
		[SecurityCritical]
		[PrePrepareMethod]
		private void MutexCleanupCode(object userData, bool exceptionThrown)
		{
			Mutex.MutexCleanupInfo mutexCleanupInfo = (Mutex.MutexCleanupInfo)userData;
			if (!this.hasThreadAffinity)
			{
				if (mutexCleanupInfo.mutexHandle != null && !mutexCleanupInfo.mutexHandle.IsInvalid)
				{
					if (mutexCleanupInfo.inCriticalRegion)
					{
						Win32Native.ReleaseMutex(mutexCleanupInfo.mutexHandle);
					}
					mutexCleanupInfo.mutexHandle.Dispose();
				}
				if (mutexCleanupInfo.inCriticalRegion)
				{
					Thread.EndCriticalRegion();
					Thread.EndThreadAffinity();
				}
			}
		}

		// Token: 0x06003C7F RID: 15487 RVA: 0x000E467E File Offset: 0x000E287E
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public Mutex(bool initiallyOwned, string name)
			: this(initiallyOwned, name, out Mutex.dummyBool)
		{
		}

		// Token: 0x06003C80 RID: 15488 RVA: 0x000E468D File Offset: 0x000E288D
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public Mutex(bool initiallyOwned)
			: this(initiallyOwned, null, out Mutex.dummyBool)
		{
		}

		// Token: 0x06003C81 RID: 15489 RVA: 0x000E469C File Offset: 0x000E289C
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public Mutex()
			: this(false, null, out Mutex.dummyBool)
		{
		}

		// Token: 0x06003C82 RID: 15490 RVA: 0x000E46AB File Offset: 0x000E28AB
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		private Mutex(SafeWaitHandle handle)
		{
			base.SetHandleInternal(handle);
			this.hasThreadAffinity = true;
		}

		// Token: 0x06003C83 RID: 15491 RVA: 0x000E46C1 File Offset: 0x000E28C1
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static Mutex OpenExisting(string name)
		{
			return Mutex.OpenExisting(name, MutexRights.Modify | MutexRights.Synchronize);
		}

		// Token: 0x06003C84 RID: 15492 RVA: 0x000E46D0 File Offset: 0x000E28D0
		[SecurityCritical]
		public static Mutex OpenExisting(string name, MutexRights rights)
		{
			Mutex result;
			switch (Mutex.OpenExistingWorker(name, rights, out result))
			{
			case WaitHandle.OpenExistingResult.NameNotFound:
				throw new WaitHandleCannotBeOpenedException();
			case WaitHandle.OpenExistingResult.PathNotFound:
				__Error.WinIOError(3, name);
				return result;
			case WaitHandle.OpenExistingResult.NameInvalid:
				throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", new object[] { name }));
			default:
				return result;
			}
		}

		// Token: 0x06003C85 RID: 15493 RVA: 0x000E4727 File Offset: 0x000E2927
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static bool TryOpenExisting(string name, out Mutex result)
		{
			return Mutex.OpenExistingWorker(name, MutexRights.Modify | MutexRights.Synchronize, out result) == WaitHandle.OpenExistingResult.Success;
		}

		// Token: 0x06003C86 RID: 15494 RVA: 0x000E4738 File Offset: 0x000E2938
		[SecurityCritical]
		public static bool TryOpenExisting(string name, MutexRights rights, out Mutex result)
		{
			return Mutex.OpenExistingWorker(name, rights, out result) == WaitHandle.OpenExistingResult.Success;
		}

		// Token: 0x06003C87 RID: 15495 RVA: 0x000E4748 File Offset: 0x000E2948
		[SecurityCritical]
		private static WaitHandle.OpenExistingResult OpenExistingWorker(string name, MutexRights rights, out Mutex result)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
			}
			if (260 < name.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", new object[] { name }));
			}
			result = null;
			SafeWaitHandle safeWaitHandle = Win32Native.OpenMutex((int)rights, false, name);
			if (safeWaitHandle.IsInvalid)
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (2 == lastWin32Error || 123 == lastWin32Error)
				{
					return WaitHandle.OpenExistingResult.NameNotFound;
				}
				if (3 == lastWin32Error)
				{
					return WaitHandle.OpenExistingResult.PathNotFound;
				}
				if (name != null && name.Length != 0 && 6 == lastWin32Error)
				{
					return WaitHandle.OpenExistingResult.NameInvalid;
				}
				__Error.WinIOError(lastWin32Error, name);
			}
			result = new Mutex(safeWaitHandle);
			return WaitHandle.OpenExistingResult.Success;
		}

		// Token: 0x06003C88 RID: 15496 RVA: 0x000E47FF File Offset: 0x000E29FF
		[SecuritySafeCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[__DynamicallyInvokable]
		public void ReleaseMutex()
		{
			if (Win32Native.ReleaseMutex(this.safeWaitHandle))
			{
				Thread.EndCriticalRegion();
				Thread.EndThreadAffinity();
				return;
			}
			throw new ApplicationException(Environment.GetResourceString("Arg_SynchronizationLockException"));
		}

		// Token: 0x06003C89 RID: 15497 RVA: 0x000E482C File Offset: 0x000E2A2C
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		private static int CreateMutexHandle(bool initiallyOwned, string name, Win32Native.SECURITY_ATTRIBUTES securityAttribute, out SafeWaitHandle mutexHandle)
		{
			bool flag = false;
			int num;
			do
			{
				mutexHandle = Win32Native.CreateMutex(securityAttribute, initiallyOwned, name);
				num = Marshal.GetLastWin32Error();
				if (!mutexHandle.IsInvalid || num != 5)
				{
					return num;
				}
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					try
					{
					}
					finally
					{
						Thread.BeginThreadAffinity();
						flag = true;
					}
					mutexHandle = Win32Native.OpenMutex(1048577, false, name);
					if (!mutexHandle.IsInvalid)
					{
						num = 183;
					}
					else
					{
						num = Marshal.GetLastWin32Error();
					}
				}
				finally
				{
					if (flag)
					{
						Thread.EndThreadAffinity();
					}
				}
			}
			while (num == 2);
			if (num == 0)
			{
				num = 183;
			}
			return num;
		}

		// Token: 0x06003C8A RID: 15498 RVA: 0x000E48C4 File Offset: 0x000E2AC4
		[SecuritySafeCritical]
		public MutexSecurity GetAccessControl()
		{
			return new MutexSecurity(this.safeWaitHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
		}

		// Token: 0x06003C8B RID: 15499 RVA: 0x000E48D5 File Offset: 0x000E2AD5
		[SecuritySafeCritical]
		public void SetAccessControl(MutexSecurity mutexSecurity)
		{
			if (mutexSecurity == null)
			{
				throw new ArgumentNullException("mutexSecurity");
			}
			mutexSecurity.Persist(this.safeWaitHandle);
		}

		// Token: 0x040019A4 RID: 6564
		private static bool dummyBool;

		// Token: 0x02000BF1 RID: 3057
		internal class MutexTryCodeHelper
		{
			// Token: 0x06006F64 RID: 28516 RVA: 0x0017FC5B File Offset: 0x0017DE5B
			[SecurityCritical]
			[PrePrepareMethod]
			internal MutexTryCodeHelper(bool initiallyOwned, Mutex.MutexCleanupInfo cleanupInfo, string name, Win32Native.SECURITY_ATTRIBUTES secAttrs, Mutex mutex)
			{
				this.m_initiallyOwned = initiallyOwned;
				this.m_cleanupInfo = cleanupInfo;
				this.m_name = name;
				this.m_secAttrs = secAttrs;
				this.m_mutex = mutex;
			}

			// Token: 0x06006F65 RID: 28517 RVA: 0x0017FC88 File Offset: 0x0017DE88
			[SecurityCritical]
			[PrePrepareMethod]
			internal void MutexTryCode(object userData)
			{
				SafeWaitHandle safeWaitHandle = null;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
				}
				finally
				{
					if (this.m_initiallyOwned)
					{
						this.m_cleanupInfo.inCriticalRegion = true;
						Thread.BeginThreadAffinity();
						Thread.BeginCriticalRegion();
					}
				}
				int num = 0;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
				}
				finally
				{
					num = Mutex.CreateMutexHandle(this.m_initiallyOwned, this.m_name, this.m_secAttrs, out safeWaitHandle);
				}
				if (safeWaitHandle.IsInvalid)
				{
					safeWaitHandle.SetHandleAsInvalid();
					if (this.m_name != null && this.m_name.Length != 0 && 6 == num)
					{
						throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", new object[] { this.m_name }));
					}
					__Error.WinIOError(num, this.m_name);
				}
				this.m_newMutex = num != 183;
				this.m_mutex.SetHandleInternal(safeWaitHandle);
				this.m_mutex.hasThreadAffinity = true;
			}

			// Token: 0x0400361C RID: 13852
			private bool m_initiallyOwned;

			// Token: 0x0400361D RID: 13853
			private Mutex.MutexCleanupInfo m_cleanupInfo;

			// Token: 0x0400361E RID: 13854
			internal bool m_newMutex;

			// Token: 0x0400361F RID: 13855
			private string m_name;

			// Token: 0x04003620 RID: 13856
			[SecurityCritical]
			private Win32Native.SECURITY_ATTRIBUTES m_secAttrs;

			// Token: 0x04003621 RID: 13857
			private Mutex m_mutex;
		}

		// Token: 0x02000BF2 RID: 3058
		internal class MutexCleanupInfo
		{
			// Token: 0x06006F66 RID: 28518 RVA: 0x0017FD78 File Offset: 0x0017DF78
			[SecurityCritical]
			internal MutexCleanupInfo(SafeWaitHandle mutexHandle, bool inCriticalRegion)
			{
				this.mutexHandle = mutexHandle;
				this.inCriticalRegion = inCriticalRegion;
			}

			// Token: 0x04003622 RID: 13858
			[SecurityCritical]
			internal SafeWaitHandle mutexHandle;

			// Token: 0x04003623 RID: 13859
			internal bool inCriticalRegion;
		}
	}
}
