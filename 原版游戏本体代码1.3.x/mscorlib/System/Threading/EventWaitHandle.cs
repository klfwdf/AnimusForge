using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
	// Token: 0x020004F4 RID: 1268
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public class EventWaitHandle : WaitHandle
	{
		// Token: 0x06003BD9 RID: 15321 RVA: 0x000E2EBE File Offset: 0x000E10BE
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public EventWaitHandle(bool initialState, EventResetMode mode)
			: this(initialState, mode, null)
		{
		}

		// Token: 0x06003BDA RID: 15322 RVA: 0x000E2ECC File Offset: 0x000E10CC
		[SecurityCritical]
		[__DynamicallyInvokable]
		public EventWaitHandle(bool initialState, EventResetMode mode, string name)
		{
			if (name != null && 260 < name.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", new object[] { name }));
			}
			SafeWaitHandle safeWaitHandle;
			if (mode != EventResetMode.AutoReset)
			{
				if (mode != EventResetMode.ManualReset)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag", new object[] { name }));
				}
				safeWaitHandle = Win32Native.CreateEvent(null, true, initialState, name);
			}
			else
			{
				safeWaitHandle = Win32Native.CreateEvent(null, false, initialState, name);
			}
			if (safeWaitHandle.IsInvalid)
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				safeWaitHandle.SetHandleAsInvalid();
				if (name != null && name.Length != 0 && 6 == lastWin32Error)
				{
					throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", new object[] { name }));
				}
				__Error.WinIOError(lastWin32Error, name);
			}
			base.SetHandleInternal(safeWaitHandle);
		}

		// Token: 0x06003BDB RID: 15323 RVA: 0x000E2F8F File Offset: 0x000E118F
		[SecurityCritical]
		[__DynamicallyInvokable]
		public EventWaitHandle(bool initialState, EventResetMode mode, string name, out bool createdNew)
			: this(initialState, mode, name, out createdNew, null)
		{
		}

		// Token: 0x06003BDC RID: 15324 RVA: 0x000E2FA0 File Offset: 0x000E11A0
		[SecurityCritical]
		public unsafe EventWaitHandle(bool initialState, EventResetMode mode, string name, out bool createdNew, EventWaitHandleSecurity eventSecurity)
		{
			if (name != null && 260 < name.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", new object[] { name }));
			}
			Win32Native.SECURITY_ATTRIBUTES security_ATTRIBUTES = null;
			if (eventSecurity != null)
			{
				security_ATTRIBUTES = new Win32Native.SECURITY_ATTRIBUTES();
				security_ATTRIBUTES.nLength = Marshal.SizeOf<Win32Native.SECURITY_ATTRIBUTES>(security_ATTRIBUTES);
				byte[] securityDescriptorBinaryForm = eventSecurity.GetSecurityDescriptorBinaryForm();
				byte* ptr = stackalloc byte[(UIntPtr)securityDescriptorBinaryForm.Length];
				Buffer.Memcpy(ptr, 0, securityDescriptorBinaryForm, 0, securityDescriptorBinaryForm.Length);
				security_ATTRIBUTES.pSecurityDescriptor = ptr;
			}
			bool isManualReset;
			if (mode != EventResetMode.AutoReset)
			{
				if (mode != EventResetMode.ManualReset)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag", new object[] { name }));
				}
				isManualReset = true;
			}
			else
			{
				isManualReset = false;
			}
			SafeWaitHandle safeWaitHandle = Win32Native.CreateEvent(security_ATTRIBUTES, isManualReset, initialState, name);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (safeWaitHandle.IsInvalid)
			{
				safeWaitHandle.SetHandleAsInvalid();
				if (name != null && name.Length != 0 && 6 == lastWin32Error)
				{
					throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", new object[] { name }));
				}
				__Error.WinIOError(lastWin32Error, name);
			}
			createdNew = lastWin32Error != 183;
			base.SetHandleInternal(safeWaitHandle);
		}

		// Token: 0x06003BDD RID: 15325 RVA: 0x000E30AC File Offset: 0x000E12AC
		[SecurityCritical]
		private EventWaitHandle(SafeWaitHandle handle)
		{
			base.SetHandleInternal(handle);
		}

		// Token: 0x06003BDE RID: 15326 RVA: 0x000E30BB File Offset: 0x000E12BB
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static EventWaitHandle OpenExisting(string name)
		{
			return EventWaitHandle.OpenExisting(name, EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize);
		}

		// Token: 0x06003BDF RID: 15327 RVA: 0x000E30C8 File Offset: 0x000E12C8
		[SecurityCritical]
		public static EventWaitHandle OpenExisting(string name, EventWaitHandleRights rights)
		{
			EventWaitHandle result;
			switch (EventWaitHandle.OpenExistingWorker(name, rights, out result))
			{
			case WaitHandle.OpenExistingResult.NameNotFound:
				throw new WaitHandleCannotBeOpenedException();
			case WaitHandle.OpenExistingResult.PathNotFound:
				__Error.WinIOError(3, "");
				return result;
			case WaitHandle.OpenExistingResult.NameInvalid:
				throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", new object[] { name }));
			default:
				return result;
			}
		}

		// Token: 0x06003BE0 RID: 15328 RVA: 0x000E3123 File Offset: 0x000E1323
		[SecurityCritical]
		[__DynamicallyInvokable]
		public static bool TryOpenExisting(string name, out EventWaitHandle result)
		{
			return EventWaitHandle.OpenExistingWorker(name, EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize, out result) == WaitHandle.OpenExistingResult.Success;
		}

		// Token: 0x06003BE1 RID: 15329 RVA: 0x000E3134 File Offset: 0x000E1334
		[SecurityCritical]
		public static bool TryOpenExisting(string name, EventWaitHandleRights rights, out EventWaitHandle result)
		{
			return EventWaitHandle.OpenExistingWorker(name, rights, out result) == WaitHandle.OpenExistingResult.Success;
		}

		// Token: 0x06003BE2 RID: 15330 RVA: 0x000E3144 File Offset: 0x000E1344
		[SecurityCritical]
		private static WaitHandle.OpenExistingResult OpenExistingWorker(string name, EventWaitHandleRights rights, out EventWaitHandle result)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
			}
			if (name.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
			}
			if (name != null && 260 < name.Length)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", new object[] { name }));
			}
			result = null;
			SafeWaitHandle safeWaitHandle = Win32Native.OpenEvent((int)rights, false, name);
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
				__Error.WinIOError(lastWin32Error, "");
			}
			result = new EventWaitHandle(safeWaitHandle);
			return WaitHandle.OpenExistingResult.Success;
		}

		// Token: 0x06003BE3 RID: 15331 RVA: 0x000E3200 File Offset: 0x000E1400
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public bool Reset()
		{
			bool flag = Win32Native.ResetEvent(this.safeWaitHandle);
			if (!flag)
			{
				__Error.WinIOError();
			}
			return flag;
		}

		// Token: 0x06003BE4 RID: 15332 RVA: 0x000E3224 File Offset: 0x000E1424
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public bool Set()
		{
			bool flag = Win32Native.SetEvent(this.safeWaitHandle);
			if (!flag)
			{
				__Error.WinIOError();
			}
			return flag;
		}

		// Token: 0x06003BE5 RID: 15333 RVA: 0x000E3248 File Offset: 0x000E1448
		[SecuritySafeCritical]
		public EventWaitHandleSecurity GetAccessControl()
		{
			return new EventWaitHandleSecurity(this.safeWaitHandle, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
		}

		// Token: 0x06003BE6 RID: 15334 RVA: 0x000E3259 File Offset: 0x000E1459
		[SecuritySafeCritical]
		public void SetAccessControl(EventWaitHandleSecurity eventSecurity)
		{
			if (eventSecurity == null)
			{
				throw new ArgumentNullException("eventSecurity");
			}
			eventSecurity.Persist(this.safeWaitHandle);
		}
	}
}
