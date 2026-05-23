using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Principal
{
	// Token: 0x0200032C RID: 812
	[ComVisible(true)]
	[Serializable]
	public class WindowsIdentity : ClaimsIdentity, ISerializable, IDeserializationCallback, IDisposable
	{
		// Token: 0x060028A3 RID: 10403 RVA: 0x00095029 File Offset: 0x00093229
		[SecurityCritical]
		private WindowsIdentity()
			: base(null, null, null, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
		{
		}

		// Token: 0x060028A4 RID: 10404 RVA: 0x00095066 File Offset: 0x00093266
		[SecurityCritical]
		internal WindowsIdentity(SafeAccessTokenHandle safeTokenHandle)
			: this(safeTokenHandle.DangerousGetHandle(), null, -1)
		{
			GC.KeepAlive(safeTokenHandle);
		}

		// Token: 0x060028A5 RID: 10405 RVA: 0x0009507C File Offset: 0x0009327C
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public WindowsIdentity(IntPtr userToken)
			: this(userToken, null, -1)
		{
		}

		// Token: 0x060028A6 RID: 10406 RVA: 0x00095087 File Offset: 0x00093287
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public WindowsIdentity(IntPtr userToken, string type)
			: this(userToken, type, -1)
		{
		}

		// Token: 0x060028A7 RID: 10407 RVA: 0x00095092 File Offset: 0x00093292
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public WindowsIdentity(IntPtr userToken, string type, WindowsAccountType acctType)
			: this(userToken, type, -1)
		{
		}

		// Token: 0x060028A8 RID: 10408 RVA: 0x0009509D File Offset: 0x0009329D
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public WindowsIdentity(IntPtr userToken, string type, WindowsAccountType acctType, bool isAuthenticated)
			: this(userToken, type, isAuthenticated ? 1 : 0)
		{
		}

		// Token: 0x060028A9 RID: 10409 RVA: 0x000950B0 File Offset: 0x000932B0
		[SecurityCritical]
		private WindowsIdentity(IntPtr userToken, string authType, int isAuthenticated)
			: base(null, null, null, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
		{
			this.CreateFromToken(userToken);
			this.m_authType = authType;
			this.m_isAuthenticated = isAuthenticated;
		}

		// Token: 0x060028AA RID: 10410 RVA: 0x00095110 File Offset: 0x00093310
		[SecurityCritical]
		private void CreateFromToken(IntPtr userToken)
		{
			if (userToken == IntPtr.Zero)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_TokenZero"));
			}
			uint num = (uint)Marshal.SizeOf(typeof(uint));
			bool tokenInformation = Win32Native.GetTokenInformation(userToken, 8U, SafeLocalAllocHandle.InvalidHandle, 0U, out num);
			if (Marshal.GetLastWin32Error() == 6)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidImpersonationToken"));
			}
			if (!Win32Native.DuplicateHandle(Win32Native.GetCurrentProcess(), userToken, Win32Native.GetCurrentProcess(), ref this.m_safeTokenHandle, 0U, true, 2U))
			{
				throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
			}
		}

		// Token: 0x060028AB RID: 10411 RVA: 0x0009519D File Offset: 0x0009339D
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		public WindowsIdentity(string sUserPrincipalName)
			: this(sUserPrincipalName, null)
		{
		}

		// Token: 0x060028AC RID: 10412 RVA: 0x000951A8 File Offset: 0x000933A8
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		public WindowsIdentity(string sUserPrincipalName, string type)
			: base(null, null, null, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
		{
			WindowsIdentity.KerbS4ULogon(sUserPrincipalName, ref this.m_safeTokenHandle);
		}

		// Token: 0x060028AD RID: 10413 RVA: 0x000951FD File Offset: 0x000933FD
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public WindowsIdentity(SerializationInfo info, StreamingContext context)
			: this(info)
		{
		}

		// Token: 0x060028AE RID: 10414 RVA: 0x00095208 File Offset: 0x00093408
		[SecurityCritical]
		private WindowsIdentity(SerializationInfo info)
			: base(info)
		{
			this.m_claimsInitialized = false;
			IntPtr intPtr = (IntPtr)info.GetValue("m_userToken", typeof(IntPtr));
			if (intPtr != IntPtr.Zero)
			{
				this.CreateFromToken(intPtr);
			}
		}

		// Token: 0x060028AF RID: 10415 RVA: 0x0009527C File Offset: 0x0009347C
		[SecurityCritical]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("m_userToken", this.m_safeTokenHandle.DangerousGetHandle());
		}

		// Token: 0x060028B0 RID: 10416 RVA: 0x000952A1 File Offset: 0x000934A1
		void IDeserializationCallback.OnDeserialization(object sender)
		{
		}

		// Token: 0x060028B1 RID: 10417 RVA: 0x000952A3 File Offset: 0x000934A3
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		public static WindowsIdentity GetCurrent()
		{
			return WindowsIdentity.GetCurrentInternal(TokenAccessLevels.MaximumAllowed, false);
		}

		// Token: 0x060028B2 RID: 10418 RVA: 0x000952B0 File Offset: 0x000934B0
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		public static WindowsIdentity GetCurrent(bool ifImpersonating)
		{
			return WindowsIdentity.GetCurrentInternal(TokenAccessLevels.MaximumAllowed, ifImpersonating);
		}

		// Token: 0x060028B3 RID: 10419 RVA: 0x000952BD File Offset: 0x000934BD
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
		public static WindowsIdentity GetCurrent(TokenAccessLevels desiredAccess)
		{
			return WindowsIdentity.GetCurrentInternal(desiredAccess, false);
		}

		// Token: 0x060028B4 RID: 10420 RVA: 0x000952C6 File Offset: 0x000934C6
		[SecuritySafeCritical]
		public static WindowsIdentity GetAnonymous()
		{
			return new WindowsIdentity();
		}

		// Token: 0x17000543 RID: 1347
		// (get) Token: 0x060028B5 RID: 10421 RVA: 0x000952D0 File Offset: 0x000934D0
		public sealed override string AuthenticationType
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_safeTokenHandle.IsInvalid)
				{
					return string.Empty;
				}
				if (this.m_authType == null)
				{
					Win32Native.LUID logonAuthId = WindowsIdentity.GetLogonAuthId(this.m_safeTokenHandle);
					if (logonAuthId.LowPart == 998U)
					{
						return string.Empty;
					}
					SafeLsaReturnBufferHandle invalidHandle = SafeLsaReturnBufferHandle.InvalidHandle;
					try
					{
						int num = Win32Native.LsaGetLogonSessionData(ref logonAuthId, ref invalidHandle);
						if (num < 0)
						{
							throw WindowsIdentity.GetExceptionFromNtStatus(num);
						}
						invalidHandle.Initialize((ulong)Marshal.SizeOf(typeof(Win32Native.SECURITY_LOGON_SESSION_DATA)));
						Win32Native.SECURITY_LOGON_SESSION_DATA security_LOGON_SESSION_DATA = invalidHandle.Read<Win32Native.SECURITY_LOGON_SESSION_DATA>(0UL);
						return Marshal.PtrToStringUni(security_LOGON_SESSION_DATA.AuthenticationPackage.Buffer);
					}
					finally
					{
						if (!invalidHandle.IsInvalid)
						{
							invalidHandle.Dispose();
						}
					}
				}
				return this.m_authType;
			}
		}

		// Token: 0x17000544 RID: 1348
		// (get) Token: 0x060028B6 RID: 10422 RVA: 0x00095390 File Offset: 0x00093590
		[ComVisible(false)]
		public TokenImpersonationLevel ImpersonationLevel
		{
			[SecuritySafeCritical]
			get
			{
				if (!this.m_impersonationLevelInitialized)
				{
					TokenImpersonationLevel impersonationLevel;
					if (this.m_safeTokenHandle.IsInvalid)
					{
						impersonationLevel = TokenImpersonationLevel.Anonymous;
					}
					else
					{
						TokenType tokenInformation = (TokenType)this.GetTokenInformation<int>(TokenInformationClass.TokenType);
						if (tokenInformation == TokenType.TokenPrimary)
						{
							impersonationLevel = TokenImpersonationLevel.None;
						}
						else
						{
							int tokenInformation2 = this.GetTokenInformation<int>(TokenInformationClass.TokenImpersonationLevel);
							impersonationLevel = tokenInformation2 + TokenImpersonationLevel.Anonymous;
						}
					}
					this.m_impersonationLevel = impersonationLevel;
					this.m_impersonationLevelInitialized = true;
				}
				return this.m_impersonationLevel;
			}
		}

		// Token: 0x17000545 RID: 1349
		// (get) Token: 0x060028B7 RID: 10423 RVA: 0x000953F1 File Offset: 0x000935F1
		public override bool IsAuthenticated
		{
			get
			{
				if (this.m_isAuthenticated == -1)
				{
					this.m_isAuthenticated = (this.CheckNtTokenForSid(new SecurityIdentifier(IdentifierAuthority.NTAuthority, new int[] { 11 })) ? 1 : 0);
				}
				return this.m_isAuthenticated == 1;
			}
		}

		// Token: 0x060028B8 RID: 10424 RVA: 0x0009542C File Offset: 0x0009362C
		[SecuritySafeCritical]
		[ComVisible(false)]
		private bool CheckNtTokenForSid(SecurityIdentifier sid)
		{
			if (this.m_safeTokenHandle.IsInvalid)
			{
				return false;
			}
			SafeAccessTokenHandle invalidHandle = SafeAccessTokenHandle.InvalidHandle;
			TokenImpersonationLevel impersonationLevel = this.ImpersonationLevel;
			bool result = false;
			try
			{
				if (impersonationLevel == TokenImpersonationLevel.None && !Win32Native.DuplicateTokenEx(this.m_safeTokenHandle, 8U, IntPtr.Zero, 2U, 2U, ref invalidHandle))
				{
					throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
				}
				if (!Win32Native.CheckTokenMembership((impersonationLevel != TokenImpersonationLevel.None) ? this.m_safeTokenHandle : invalidHandle, sid.BinaryForm, ref result))
				{
					throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
				}
			}
			finally
			{
				if (invalidHandle != SafeAccessTokenHandle.InvalidHandle)
				{
					invalidHandle.Dispose();
				}
			}
			return result;
		}

		// Token: 0x17000546 RID: 1350
		// (get) Token: 0x060028B9 RID: 10425 RVA: 0x000954D0 File Offset: 0x000936D0
		public virtual bool IsGuest
		{
			[SecuritySafeCritical]
			get
			{
				return !this.m_safeTokenHandle.IsInvalid && this.CheckNtTokenForSid(new SecurityIdentifier(IdentifierAuthority.NTAuthority, new int[] { 32, 546 }));
			}
		}

		// Token: 0x17000547 RID: 1351
		// (get) Token: 0x060028BA RID: 10426 RVA: 0x00095504 File Offset: 0x00093704
		public virtual bool IsSystem
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_safeTokenHandle.IsInvalid)
				{
					return false;
				}
				SecurityIdentifier right = new SecurityIdentifier(IdentifierAuthority.NTAuthority, new int[] { 18 });
				return this.User == right;
			}
		}

		// Token: 0x17000548 RID: 1352
		// (get) Token: 0x060028BB RID: 10427 RVA: 0x00095540 File Offset: 0x00093740
		public virtual bool IsAnonymous
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_safeTokenHandle.IsInvalid)
				{
					return true;
				}
				SecurityIdentifier right = new SecurityIdentifier(IdentifierAuthority.NTAuthority, new int[] { 7 });
				return this.User == right;
			}
		}

		// Token: 0x17000549 RID: 1353
		// (get) Token: 0x060028BC RID: 10428 RVA: 0x0009557A File Offset: 0x0009377A
		public override string Name
		{
			[SecuritySafeCritical]
			get
			{
				return this.GetName();
			}
		}

		// Token: 0x060028BD RID: 10429 RVA: 0x00095584 File Offset: 0x00093784
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal string GetName()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			if (this.m_safeTokenHandle.IsInvalid)
			{
				return string.Empty;
			}
			if (this.m_name == null)
			{
				using (WindowsIdentity.SafeRevertToSelf(ref stackCrawlMark))
				{
					NTAccount ntaccount = this.User.Translate(typeof(NTAccount)) as NTAccount;
					this.m_name = ntaccount.ToString();
				}
			}
			return this.m_name;
		}

		// Token: 0x1700054A RID: 1354
		// (get) Token: 0x060028BE RID: 10430 RVA: 0x00095600 File Offset: 0x00093800
		[ComVisible(false)]
		public SecurityIdentifier Owner
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_safeTokenHandle.IsInvalid)
				{
					return null;
				}
				if (this.m_owner == null)
				{
					using (SafeLocalAllocHandle tokenInformation = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, TokenInformationClass.TokenOwner))
					{
						this.m_owner = new SecurityIdentifier(tokenInformation.Read<IntPtr>(0UL), true);
					}
				}
				return this.m_owner;
			}
		}

		// Token: 0x1700054B RID: 1355
		// (get) Token: 0x060028BF RID: 10431 RVA: 0x00095670 File Offset: 0x00093870
		[ComVisible(false)]
		public SecurityIdentifier User
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_safeTokenHandle.IsInvalid)
				{
					return null;
				}
				if (this.m_user == null)
				{
					using (SafeLocalAllocHandle tokenInformation = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, TokenInformationClass.TokenUser))
					{
						this.m_user = new SecurityIdentifier(tokenInformation.Read<IntPtr>(0UL), true);
					}
				}
				return this.m_user;
			}
		}

		// Token: 0x1700054C RID: 1356
		// (get) Token: 0x060028C0 RID: 10432 RVA: 0x000956E0 File Offset: 0x000938E0
		public IdentityReferenceCollection Groups
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_safeTokenHandle.IsInvalid)
				{
					return null;
				}
				if (this.m_groups == null)
				{
					IdentityReferenceCollection identityReferenceCollection = new IdentityReferenceCollection();
					using (SafeLocalAllocHandle tokenInformation = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, TokenInformationClass.TokenGroups))
					{
						uint num = tokenInformation.Read<uint>(0UL);
						if (num != 0U)
						{
							Win32Native.TOKEN_GROUPS token_GROUPS = tokenInformation.Read<Win32Native.TOKEN_GROUPS>(0UL);
							Win32Native.SID_AND_ATTRIBUTES[] array = new Win32Native.SID_AND_ATTRIBUTES[token_GROUPS.GroupCount];
							tokenInformation.ReadArray<Win32Native.SID_AND_ATTRIBUTES>((ulong)Marshal.OffsetOf(typeof(Win32Native.TOKEN_GROUPS), "Groups").ToInt32(), array, 0, array.Length);
							foreach (Win32Native.SID_AND_ATTRIBUTES sid_AND_ATTRIBUTES in array)
							{
								uint num2 = 3221225492U;
								if ((sid_AND_ATTRIBUTES.Attributes & num2) == 4U)
								{
									identityReferenceCollection.Add(new SecurityIdentifier(sid_AND_ATTRIBUTES.Sid, true));
								}
							}
						}
					}
					Interlocked.CompareExchange(ref this.m_groups, identityReferenceCollection, null);
				}
				return this.m_groups as IdentityReferenceCollection;
			}
		}

		// Token: 0x1700054D RID: 1357
		// (get) Token: 0x060028C1 RID: 10433 RVA: 0x000957E8 File Offset: 0x000939E8
		public virtual IntPtr Token
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				return this.m_safeTokenHandle.DangerousGetHandle();
			}
		}

		// Token: 0x060028C2 RID: 10434 RVA: 0x000957F8 File Offset: 0x000939F8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void RunImpersonated(SafeAccessTokenHandle safeAccessTokenHandle, Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			WindowsIdentity wi = null;
			if (!safeAccessTokenHandle.IsInvalid)
			{
				wi = new WindowsIdentity(safeAccessTokenHandle);
			}
			using (WindowsIdentity.SafeImpersonate(safeAccessTokenHandle, wi, ref stackCrawlMark))
			{
				action();
			}
		}

		// Token: 0x060028C3 RID: 10435 RVA: 0x00095854 File Offset: 0x00093A54
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static T RunImpersonated<T>(SafeAccessTokenHandle safeAccessTokenHandle, Func<T> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException("func");
			}
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			WindowsIdentity wi = null;
			if (!safeAccessTokenHandle.IsInvalid)
			{
				wi = new WindowsIdentity(safeAccessTokenHandle);
			}
			T result = default(T);
			using (WindowsIdentity.SafeImpersonate(safeAccessTokenHandle, wi, ref stackCrawlMark))
			{
				result = func();
			}
			return result;
		}

		// Token: 0x060028C4 RID: 10436 RVA: 0x000958B8 File Offset: 0x00093AB8
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public virtual WindowsImpersonationContext Impersonate()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return this.Impersonate(ref stackCrawlMark);
		}

		// Token: 0x060028C5 RID: 10437 RVA: 0x000958D0 File Offset: 0x00093AD0
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode | SecurityPermissionFlag.ControlPrincipal)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static WindowsImpersonationContext Impersonate(IntPtr userToken)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			if (userToken == IntPtr.Zero)
			{
				return WindowsIdentity.SafeRevertToSelf(ref stackCrawlMark);
			}
			WindowsIdentity windowsIdentity = new WindowsIdentity(userToken, null, -1);
			return windowsIdentity.Impersonate(ref stackCrawlMark);
		}

		// Token: 0x060028C6 RID: 10438 RVA: 0x00095905 File Offset: 0x00093B05
		[SecurityCritical]
		internal WindowsImpersonationContext Impersonate(ref StackCrawlMark stackMark)
		{
			if (this.m_safeTokenHandle.IsInvalid)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AnonymousCannotImpersonate"));
			}
			return WindowsIdentity.SafeImpersonate(this.m_safeTokenHandle, this, ref stackMark);
		}

		// Token: 0x060028C7 RID: 10439 RVA: 0x00095931 File Offset: 0x00093B31
		[SecuritySafeCritical]
		[ComVisible(false)]
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && this.m_safeTokenHandle != null && !this.m_safeTokenHandle.IsClosed)
			{
				this.m_safeTokenHandle.Dispose();
			}
			this.m_name = null;
			this.m_owner = null;
			this.m_user = null;
		}

		// Token: 0x060028C8 RID: 10440 RVA: 0x0009596B File Offset: 0x00093B6B
		[ComVisible(false)]
		public void Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x1700054E RID: 1358
		// (get) Token: 0x060028C9 RID: 10441 RVA: 0x00095974 File Offset: 0x00093B74
		public SafeAccessTokenHandle AccessToken
		{
			[SecurityCritical]
			get
			{
				return this.m_safeTokenHandle;
			}
		}

		// Token: 0x060028CA RID: 10442 RVA: 0x0009597C File Offset: 0x00093B7C
		[SecurityCritical]
		internal static WindowsImpersonationContext SafeRevertToSelf(ref StackCrawlMark stackMark)
		{
			return WindowsIdentity.SafeImpersonate(WindowsIdentity.s_invalidTokenHandle, null, ref stackMark);
		}

		// Token: 0x060028CB RID: 10443 RVA: 0x0009598C File Offset: 0x00093B8C
		[SecurityCritical]
		internal static WindowsImpersonationContext SafeImpersonate(SafeAccessTokenHandle userToken, WindowsIdentity wi, ref StackCrawlMark stackMark)
		{
			int num = 0;
			bool isImpersonating;
			SafeAccessTokenHandle currentToken = WindowsIdentity.GetCurrentToken(TokenAccessLevels.MaximumAllowed, false, out isImpersonating, out num);
			if (currentToken == null || currentToken.IsInvalid)
			{
				throw new SecurityException(Win32Native.GetMessage(num));
			}
			FrameSecurityDescriptor securityObjectForFrame = SecurityRuntime.GetSecurityObjectForFrame(ref stackMark, true);
			if (securityObjectForFrame == null)
			{
				throw new SecurityException(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
			}
			WindowsImpersonationContext windowsImpersonationContext = new WindowsImpersonationContext(currentToken, WindowsIdentity.GetCurrentThreadWI(), isImpersonating, securityObjectForFrame);
			if (userToken.IsInvalid)
			{
				num = Win32.RevertToSelf();
				if (num < 0)
				{
					Environment.FailFast(Win32Native.GetMessage(num));
				}
				WindowsIdentity.UpdateThreadWI(wi);
				securityObjectForFrame.SetTokenHandles(currentToken, (wi == null) ? null : wi.AccessToken);
			}
			else
			{
				num = Win32.RevertToSelf();
				if (num < 0)
				{
					Environment.FailFast(Win32Native.GetMessage(num));
				}
				num = Win32.ImpersonateLoggedOnUser(userToken);
				if (num < 0)
				{
					windowsImpersonationContext.Undo();
					throw new SecurityException(Environment.GetResourceString("Argument_ImpersonateUser"));
				}
				WindowsIdentity.UpdateThreadWI(wi);
				securityObjectForFrame.SetTokenHandles(currentToken, (wi == null) ? null : wi.AccessToken);
			}
			return windowsImpersonationContext;
		}

		// Token: 0x060028CC RID: 10444 RVA: 0x00095A76 File Offset: 0x00093C76
		[SecurityCritical]
		internal static WindowsIdentity GetCurrentThreadWI()
		{
			return SecurityContext.GetCurrentWI(Thread.CurrentThread.GetExecutionContextReader());
		}

		// Token: 0x060028CD RID: 10445 RVA: 0x00095A88 File Offset: 0x00093C88
		[SecurityCritical]
		internal static void UpdateThreadWI(WindowsIdentity wi)
		{
			Thread currentThread = Thread.CurrentThread;
			if (currentThread.GetExecutionContextReader().SecurityContext.WindowsIdentity != wi)
			{
				ExecutionContext mutableExecutionContext = currentThread.GetMutableExecutionContext();
				SecurityContext securityContext = mutableExecutionContext.SecurityContext;
				if (wi != null && securityContext == null)
				{
					securityContext = new SecurityContext();
					mutableExecutionContext.SecurityContext = securityContext;
				}
				if (securityContext != null)
				{
					securityContext.WindowsIdentity = wi;
				}
			}
		}

		// Token: 0x060028CE RID: 10446 RVA: 0x00095AE8 File Offset: 0x00093CE8
		[SecurityCritical]
		internal static WindowsIdentity GetCurrentInternal(TokenAccessLevels desiredAccess, bool threadOnly)
		{
			int errorCode = 0;
			bool flag;
			SafeAccessTokenHandle currentToken = WindowsIdentity.GetCurrentToken(desiredAccess, threadOnly, out flag, out errorCode);
			if (currentToken != null && !currentToken.IsInvalid)
			{
				WindowsIdentity windowsIdentity = new WindowsIdentity();
				windowsIdentity.m_safeTokenHandle.Dispose();
				windowsIdentity.m_safeTokenHandle = currentToken;
				return windowsIdentity;
			}
			if (threadOnly && !flag)
			{
				return null;
			}
			throw new SecurityException(Win32Native.GetMessage(errorCode));
		}

		// Token: 0x060028CF RID: 10447 RVA: 0x00095B3B File Offset: 0x00093D3B
		internal static RuntimeConstructorInfo GetSpecialSerializationCtor()
		{
			return WindowsIdentity.s_specialSerializationCtor;
		}

		// Token: 0x060028D0 RID: 10448 RVA: 0x00095B42 File Offset: 0x00093D42
		private static int GetHRForWin32Error(int dwLastError)
		{
			if (((long)dwLastError & (long)((ulong)(-2147483648))) == (long)((ulong)(-2147483648)))
			{
				return dwLastError;
			}
			return (dwLastError & 65535) | -2147024896;
		}

		// Token: 0x060028D1 RID: 10449 RVA: 0x00095B64 File Offset: 0x00093D64
		[SecurityCritical]
		private static Exception GetExceptionFromNtStatus(int status)
		{
			if (status == -1073741790)
			{
				return new UnauthorizedAccessException();
			}
			if (status == -1073741670 || status == -1073741801)
			{
				return new OutOfMemoryException();
			}
			int errorCode = Win32Native.LsaNtStatusToWinError(status);
			return new SecurityException(Win32Native.GetMessage(errorCode));
		}

		// Token: 0x060028D2 RID: 10450 RVA: 0x00095BA8 File Offset: 0x00093DA8
		[SecurityCritical]
		private static SafeAccessTokenHandle GetCurrentToken(TokenAccessLevels desiredAccess, bool threadOnly, out bool isImpersonating, out int hr)
		{
			isImpersonating = true;
			SafeAccessTokenHandle safeAccessTokenHandle = WindowsIdentity.GetCurrentThreadToken(desiredAccess, out hr);
			if (safeAccessTokenHandle == null && hr == WindowsIdentity.GetHRForWin32Error(1008))
			{
				isImpersonating = false;
				if (!threadOnly)
				{
					safeAccessTokenHandle = WindowsIdentity.GetCurrentProcessToken(desiredAccess, out hr);
				}
			}
			return safeAccessTokenHandle;
		}

		// Token: 0x060028D3 RID: 10451 RVA: 0x00095BE0 File Offset: 0x00093DE0
		[SecurityCritical]
		private static SafeAccessTokenHandle GetCurrentProcessToken(TokenAccessLevels desiredAccess, out int hr)
		{
			hr = 0;
			SafeAccessTokenHandle result;
			if (!Win32Native.OpenProcessToken(Win32Native.GetCurrentProcess(), desiredAccess, out result))
			{
				hr = WindowsIdentity.GetHRForWin32Error(Marshal.GetLastWin32Error());
			}
			return result;
		}

		// Token: 0x060028D4 RID: 10452 RVA: 0x00095C0C File Offset: 0x00093E0C
		[SecurityCritical]
		internal static SafeAccessTokenHandle GetCurrentThreadToken(TokenAccessLevels desiredAccess, out int hr)
		{
			SafeAccessTokenHandle result;
			hr = Win32.OpenThreadToken(desiredAccess, WinSecurityContext.Both, out result);
			return result;
		}

		// Token: 0x060028D5 RID: 10453 RVA: 0x00095C28 File Offset: 0x00093E28
		[SecurityCritical]
		private T GetTokenInformation<T>(TokenInformationClass tokenInformationClass) where T : struct
		{
			T result;
			using (SafeLocalAllocHandle tokenInformation = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, tokenInformationClass))
			{
				result = tokenInformation.Read<T>(0UL);
			}
			return result;
		}

		// Token: 0x060028D6 RID: 10454 RVA: 0x00095C68 File Offset: 0x00093E68
		[SecurityCritical]
		internal static ImpersonationQueryResult QueryImpersonation()
		{
			SafeAccessTokenHandle safeAccessTokenHandle = null;
			int num = Win32.OpenThreadToken(TokenAccessLevels.Query, WinSecurityContext.Thread, out safeAccessTokenHandle);
			if (safeAccessTokenHandle != null)
			{
				safeAccessTokenHandle.Close();
				return ImpersonationQueryResult.Impersonated;
			}
			if (num == WindowsIdentity.GetHRForWin32Error(5))
			{
				return ImpersonationQueryResult.Impersonated;
			}
			if (num == WindowsIdentity.GetHRForWin32Error(1008))
			{
				return ImpersonationQueryResult.NotImpersonated;
			}
			return ImpersonationQueryResult.Failed;
		}

		// Token: 0x060028D7 RID: 10455 RVA: 0x00095CA8 File Offset: 0x00093EA8
		[SecurityCritical]
		private static Win32Native.LUID GetLogonAuthId(SafeAccessTokenHandle safeTokenHandle)
		{
			Win32Native.LUID authenticationId;
			using (SafeLocalAllocHandle tokenInformation = WindowsIdentity.GetTokenInformation(safeTokenHandle, TokenInformationClass.TokenStatistics))
			{
				Win32Native.TOKEN_STATISTICS token_STATISTICS = tokenInformation.Read<Win32Native.TOKEN_STATISTICS>(0UL);
				authenticationId = token_STATISTICS.AuthenticationId;
			}
			return authenticationId;
		}

		// Token: 0x060028D8 RID: 10456 RVA: 0x00095CEC File Offset: 0x00093EEC
		[SecurityCritical]
		private static SafeLocalAllocHandle GetTokenInformation(SafeAccessTokenHandle tokenHandle, TokenInformationClass tokenInformationClass)
		{
			SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
			uint num = (uint)Marshal.SizeOf(typeof(uint));
			bool tokenInformation = Win32Native.GetTokenInformation(tokenHandle, (uint)tokenInformationClass, safeLocalAllocHandle, 0U, out num);
			int lastWin32Error = Marshal.GetLastWin32Error();
			if (lastWin32Error == 6)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidImpersonationToken"));
			}
			if (lastWin32Error != 24 && lastWin32Error != 122)
			{
				throw new SecurityException(Win32Native.GetMessage(lastWin32Error));
			}
			UIntPtr sizetdwBytes = new UIntPtr(num);
			safeLocalAllocHandle.Dispose();
			safeLocalAllocHandle = Win32Native.LocalAlloc(0, sizetdwBytes);
			if (safeLocalAllocHandle == null || safeLocalAllocHandle.IsInvalid)
			{
				throw new OutOfMemoryException();
			}
			safeLocalAllocHandle.Initialize((ulong)num);
			if (!Win32Native.GetTokenInformation(tokenHandle, (uint)tokenInformationClass, safeLocalAllocHandle, num, out num))
			{
				throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
			}
			return safeLocalAllocHandle;
		}

		// Token: 0x060028D9 RID: 10457 RVA: 0x00095D9C File Offset: 0x00093F9C
		[SecurityCritical]
		[HandleProcessCorruptedStateExceptions]
		private unsafe static SafeAccessTokenHandle KerbS4ULogon(string upn, ref SafeAccessTokenHandle safeTokenHandle)
		{
			byte[] array = new byte[] { 67, 76, 82 };
			UIntPtr sizetdwBytes = new UIntPtr((uint)(array.Length + 1));
			SafeAccessTokenHandle result;
			using (SafeLocalAllocHandle safeLocalAllocHandle = Win32Native.LocalAlloc(64, sizetdwBytes))
			{
				if (safeLocalAllocHandle == null || safeLocalAllocHandle.IsInvalid)
				{
					throw new OutOfMemoryException();
				}
				safeLocalAllocHandle.Initialize((ulong)((long)array.Length + 1L));
				safeLocalAllocHandle.WriteArray<byte>(0UL, array, 0, array.Length);
				Win32Native.UNICODE_INTPTR_STRING unicode_INTPTR_STRING = new Win32Native.UNICODE_INTPTR_STRING(array.Length, safeLocalAllocHandle);
				SafeLsaLogonProcessHandle invalidHandle = SafeLsaLogonProcessHandle.InvalidHandle;
				SafeLsaReturnBufferHandle invalidHandle2 = SafeLsaReturnBufferHandle.InvalidHandle;
				try
				{
					Privilege privilege = null;
					RuntimeHelpers.PrepareConstrainedRegions();
					int num;
					try
					{
						try
						{
							privilege = new Privilege("SeTcbPrivilege");
							privilege.Enable();
						}
						catch (PrivilegeNotHeldException)
						{
						}
						IntPtr zero = IntPtr.Zero;
						num = Win32Native.LsaRegisterLogonProcess(ref unicode_INTPTR_STRING, ref invalidHandle, ref zero);
						if (5 == Win32Native.LsaNtStatusToWinError(num))
						{
							num = Win32Native.LsaConnectUntrusted(ref invalidHandle);
						}
					}
					catch
					{
						if (privilege != null)
						{
							privilege.Revert();
						}
						throw;
					}
					finally
					{
						if (privilege != null)
						{
							privilege.Revert();
						}
					}
					if (num < 0)
					{
						throw WindowsIdentity.GetExceptionFromNtStatus(num);
					}
					byte[] array2 = new byte["Kerberos".Length + 1];
					Encoding.ASCII.GetBytes("Kerberos", 0, "Kerberos".Length, array2, 0);
					sizetdwBytes = new UIntPtr((uint)array2.Length);
					using (SafeLocalAllocHandle safeLocalAllocHandle2 = Win32Native.LocalAlloc(0, sizetdwBytes))
					{
						if (safeLocalAllocHandle2 == null || safeLocalAllocHandle2.IsInvalid)
						{
							throw new OutOfMemoryException();
						}
						safeLocalAllocHandle2.Initialize((ulong)array2.Length);
						safeLocalAllocHandle2.WriteArray<byte>(0UL, array2, 0, array2.Length);
						Win32Native.UNICODE_INTPTR_STRING unicode_INTPTR_STRING2 = new Win32Native.UNICODE_INTPTR_STRING("Kerberos".Length, safeLocalAllocHandle2);
						uint authenticationPackage = 0U;
						num = Win32Native.LsaLookupAuthenticationPackage(invalidHandle, ref unicode_INTPTR_STRING2, ref authenticationPackage);
						if (num < 0)
						{
							throw WindowsIdentity.GetExceptionFromNtStatus(num);
						}
						Win32Native.TOKEN_SOURCE token_SOURCE = default(Win32Native.TOKEN_SOURCE);
						if (!Win32Native.AllocateLocallyUniqueId(ref token_SOURCE.SourceIdentifier))
						{
							throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
						}
						token_SOURCE.Name = new char[8];
						token_SOURCE.Name[0] = 'C';
						token_SOURCE.Name[1] = 'L';
						token_SOURCE.Name[2] = 'R';
						uint num2 = 0U;
						Win32Native.LUID luid = default(Win32Native.LUID);
						Win32Native.QUOTA_LIMITS quota_LIMITS = default(Win32Native.QUOTA_LIMITS);
						int num3 = 0;
						byte[] bytes = Encoding.Unicode.GetBytes(upn);
						uint num4 = (uint)(Marshal.SizeOf(typeof(Win32Native.KERB_S4U_LOGON)) + bytes.Length);
						using (SafeLocalAllocHandle safeLocalAllocHandle3 = Win32Native.LocalAlloc(64, new UIntPtr(num4)))
						{
							if (safeLocalAllocHandle3 == null || safeLocalAllocHandle3.IsInvalid)
							{
								throw new OutOfMemoryException();
							}
							safeLocalAllocHandle3.Initialize((ulong)num4);
							ulong num5 = (ulong)((long)Marshal.SizeOf(typeof(Win32Native.KERB_S4U_LOGON)));
							safeLocalAllocHandle3.WriteArray<byte>(num5, bytes, 0, bytes.Length);
							byte* ptr = null;
							RuntimeHelpers.PrepareConstrainedRegions();
							try
							{
								safeLocalAllocHandle3.AcquirePointer(ref ptr);
								safeLocalAllocHandle3.Write<Win32Native.KERB_S4U_LOGON>(0UL, new Win32Native.KERB_S4U_LOGON
								{
									MessageType = 12U,
									Flags = 0U,
									ClientUpn = new Win32Native.UNICODE_INTPTR_STRING(bytes.Length, new IntPtr((void*)(ptr + num5)))
								});
								num = Win32Native.LsaLogonUser(invalidHandle, ref unicode_INTPTR_STRING, 3U, authenticationPackage, new IntPtr((void*)ptr), (uint)safeLocalAllocHandle3.ByteLength, IntPtr.Zero, ref token_SOURCE, ref invalidHandle2, ref num2, ref luid, ref safeTokenHandle, ref quota_LIMITS, ref num3);
								if (num == -1073741714 && num3 < 0)
								{
									num = num3;
								}
								if (num < 0)
								{
									throw WindowsIdentity.GetExceptionFromNtStatus(num);
								}
								if (num3 < 0)
								{
									throw WindowsIdentity.GetExceptionFromNtStatus(num3);
								}
							}
							finally
							{
								if (ptr != null)
								{
									safeLocalAllocHandle3.ReleasePointer();
								}
							}
						}
						result = safeTokenHandle;
					}
				}
				finally
				{
					if (!invalidHandle.IsInvalid)
					{
						invalidHandle.Dispose();
					}
					if (!invalidHandle2.IsInvalid)
					{
						invalidHandle2.Dispose();
					}
				}
			}
			return result;
		}

		// Token: 0x060028DA RID: 10458 RVA: 0x000961D0 File Offset: 0x000943D0
		[SecuritySafeCritical]
		protected WindowsIdentity(WindowsIdentity identity)
			: base(identity, null, identity.m_authType, null, null, false)
		{
			if (identity == null)
			{
				throw new ArgumentNullException("identity");
			}
			bool flag = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				if (!identity.m_safeTokenHandle.IsInvalid && identity.m_safeTokenHandle != SafeAccessTokenHandle.InvalidHandle && identity.m_safeTokenHandle.DangerousGetHandle() != IntPtr.Zero)
				{
					identity.m_safeTokenHandle.DangerousAddRef(ref flag);
					if (!identity.m_safeTokenHandle.IsInvalid && identity.m_safeTokenHandle.DangerousGetHandle() != IntPtr.Zero)
					{
						this.CreateFromToken(identity.m_safeTokenHandle.DangerousGetHandle());
					}
					this.m_authType = identity.m_authType;
					this.m_isAuthenticated = identity.m_isAuthenticated;
				}
			}
			finally
			{
				if (flag)
				{
					identity.m_safeTokenHandle.DangerousRelease();
				}
			}
		}

		// Token: 0x060028DB RID: 10459 RVA: 0x000962D8 File Offset: 0x000944D8
		[SecurityCritical]
		internal IntPtr GetTokenInternal()
		{
			return this.m_safeTokenHandle.DangerousGetHandle();
		}

		// Token: 0x060028DC RID: 10460 RVA: 0x000962E8 File Offset: 0x000944E8
		[SecurityCritical]
		internal WindowsIdentity(ClaimsIdentity claimsIdentity, IntPtr userToken)
			: base(claimsIdentity)
		{
			if (userToken != IntPtr.Zero && userToken.ToInt64() > 0L)
			{
				this.CreateFromToken(userToken);
			}
		}

		// Token: 0x060028DD RID: 10461 RVA: 0x00096343 File Offset: 0x00094543
		internal ClaimsIdentity CloneAsBase()
		{
			return base.Clone();
		}

		// Token: 0x060028DE RID: 10462 RVA: 0x0009634B File Offset: 0x0009454B
		public override ClaimsIdentity Clone()
		{
			return new WindowsIdentity(this);
		}

		// Token: 0x1700054F RID: 1359
		// (get) Token: 0x060028DF RID: 10463 RVA: 0x00096353 File Offset: 0x00094553
		public virtual IEnumerable<Claim> UserClaims
		{
			get
			{
				this.InitializeClaims();
				return this.m_userClaims.AsReadOnly();
			}
		}

		// Token: 0x17000550 RID: 1360
		// (get) Token: 0x060028E0 RID: 10464 RVA: 0x00096366 File Offset: 0x00094566
		public virtual IEnumerable<Claim> DeviceClaims
		{
			get
			{
				this.InitializeClaims();
				return this.m_deviceClaims.AsReadOnly();
			}
		}

		// Token: 0x17000551 RID: 1361
		// (get) Token: 0x060028E1 RID: 10465 RVA: 0x0009637C File Offset: 0x0009457C
		public override IEnumerable<Claim> Claims
		{
			get
			{
				if (!this.m_claimsInitialized)
				{
					this.InitializeClaims();
				}
				foreach (Claim claim in base.Claims)
				{
					yield return claim;
				}
				IEnumerator<Claim> enumerator = null;
				foreach (Claim claim2 in this.m_userClaims)
				{
					yield return claim2;
				}
				List<Claim>.Enumerator enumerator2 = default(List<Claim>.Enumerator);
				foreach (Claim claim3 in this.m_deviceClaims)
				{
					yield return claim3;
				}
				enumerator2 = default(List<Claim>.Enumerator);
				yield break;
				yield break;
			}
		}

		// Token: 0x060028E2 RID: 10466 RVA: 0x0009639C File Offset: 0x0009459C
		[SecuritySafeCritical]
		private void InitializeClaims()
		{
			if (!this.m_claimsInitialized)
			{
				object claimsIntiailizedLock = this.m_claimsIntiailizedLock;
				lock (claimsIntiailizedLock)
				{
					if (!this.m_claimsInitialized)
					{
						this.m_userClaims = new List<Claim>();
						this.m_deviceClaims = new List<Claim>();
						if (!string.IsNullOrEmpty(this.Name))
						{
							this.m_userClaims.Add(new Claim(base.NameClaimType, this.Name, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this));
						}
						this.AddPrimarySidClaim(this.m_userClaims);
						this.AddGroupSidClaims(this.m_userClaims);
						if (Environment.IsWindows8OrAbove)
						{
							this.AddDeviceGroupSidClaims(this.m_deviceClaims, TokenInformationClass.TokenDeviceGroups);
							this.AddTokenClaims(this.m_userClaims, TokenInformationClass.TokenUserClaimAttributes, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsuserclaim");
							this.AddTokenClaims(this.m_deviceClaims, TokenInformationClass.TokenDeviceClaimAttributes, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdeviceclaim");
						}
						this.m_claimsInitialized = true;
					}
				}
			}
		}

		// Token: 0x060028E3 RID: 10467 RVA: 0x000964A0 File Offset: 0x000946A0
		[SecurityCritical]
		private void AddDeviceGroupSidClaims(List<Claim> instanceClaims, TokenInformationClass tokenInformationClass)
		{
			if (this.m_safeTokenHandle.IsInvalid)
			{
				return;
			}
			SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
			try
			{
				safeLocalAllocHandle = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, tokenInformationClass);
				int num = Marshal.ReadInt32(safeLocalAllocHandle.DangerousGetHandle());
				IntPtr intPtr = new IntPtr((long)safeLocalAllocHandle.DangerousGetHandle() + (long)Marshal.OffsetOf(typeof(Win32Native.TOKEN_GROUPS), "Groups"));
				for (int i = 0; i < num; i++)
				{
					Win32Native.SID_AND_ATTRIBUTES sid_AND_ATTRIBUTES = (Win32Native.SID_AND_ATTRIBUTES)Marshal.PtrToStructure(intPtr, typeof(Win32Native.SID_AND_ATTRIBUTES));
					uint num2 = 3221225492U;
					SecurityIdentifier securityIdentifier = new SecurityIdentifier(sid_AND_ATTRIBUTES.Sid, true);
					if ((sid_AND_ATTRIBUTES.Attributes & num2) == 4U)
					{
						string text = "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowsdevicegroup";
						instanceClaims.Add(new Claim(text, securityIdentifier.Value, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", Convert.ToString(securityIdentifier.IdentifierAuthority, CultureInfo.InvariantCulture))
						{
							Properties = { { text, "" } }
						});
					}
					else if ((sid_AND_ATTRIBUTES.Attributes & num2) == 16U)
					{
						string text = "http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlywindowsdevicegroup";
						instanceClaims.Add(new Claim(text, securityIdentifier.Value, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", Convert.ToString(securityIdentifier.IdentifierAuthority, CultureInfo.InvariantCulture))
						{
							Properties = { { text, "" } }
						});
					}
					intPtr = new IntPtr((long)intPtr + Win32Native.SID_AND_ATTRIBUTES.SizeOf);
				}
			}
			finally
			{
				safeLocalAllocHandle.Close();
			}
		}

		// Token: 0x060028E4 RID: 10468 RVA: 0x00096658 File Offset: 0x00094858
		[SecurityCritical]
		private void AddGroupSidClaims(List<Claim> instanceClaims)
		{
			if (this.m_safeTokenHandle.IsInvalid)
			{
				return;
			}
			SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
			SafeLocalAllocHandle safeLocalAllocHandle2 = SafeLocalAllocHandle.InvalidHandle;
			try
			{
				safeLocalAllocHandle2 = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, TokenInformationClass.TokenPrimaryGroup);
				Win32Native.TOKEN_PRIMARY_GROUP token_PRIMARY_GROUP = (Win32Native.TOKEN_PRIMARY_GROUP)Marshal.PtrToStructure(safeLocalAllocHandle2.DangerousGetHandle(), typeof(Win32Native.TOKEN_PRIMARY_GROUP));
				SecurityIdentifier securityIdentifier = new SecurityIdentifier(token_PRIMARY_GROUP.PrimaryGroup, true);
				bool flag = false;
				safeLocalAllocHandle = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, TokenInformationClass.TokenGroups);
				int num = Marshal.ReadInt32(safeLocalAllocHandle.DangerousGetHandle());
				IntPtr intPtr = new IntPtr((long)safeLocalAllocHandle.DangerousGetHandle() + (long)Marshal.OffsetOf(typeof(Win32Native.TOKEN_GROUPS), "Groups"));
				for (int i = 0; i < num; i++)
				{
					Win32Native.SID_AND_ATTRIBUTES sid_AND_ATTRIBUTES = (Win32Native.SID_AND_ATTRIBUTES)Marshal.PtrToStructure(intPtr, typeof(Win32Native.SID_AND_ATTRIBUTES));
					uint num2 = 3221225492U;
					SecurityIdentifier securityIdentifier2 = new SecurityIdentifier(sid_AND_ATTRIBUTES.Sid, true);
					if ((sid_AND_ATTRIBUTES.Attributes & num2) == 4U)
					{
						if (!flag && StringComparer.Ordinal.Equals(securityIdentifier2.Value, securityIdentifier.Value))
						{
							instanceClaims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/primarygroupsid", securityIdentifier2.Value, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", Convert.ToString(securityIdentifier2.IdentifierAuthority, CultureInfo.InvariantCulture)));
							flag = true;
						}
						instanceClaims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid", securityIdentifier2.Value, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", Convert.ToString(securityIdentifier2.IdentifierAuthority, CultureInfo.InvariantCulture)));
					}
					else if ((sid_AND_ATTRIBUTES.Attributes & num2) == 16U)
					{
						if (!flag && StringComparer.Ordinal.Equals(securityIdentifier2.Value, securityIdentifier.Value))
						{
							instanceClaims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarygroupsid", securityIdentifier2.Value, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", Convert.ToString(securityIdentifier2.IdentifierAuthority, CultureInfo.InvariantCulture)));
							flag = true;
						}
						instanceClaims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/denyonlysid", securityIdentifier2.Value, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", Convert.ToString(securityIdentifier2.IdentifierAuthority, CultureInfo.InvariantCulture)));
					}
					intPtr = new IntPtr((long)intPtr + Win32Native.SID_AND_ATTRIBUTES.SizeOf);
				}
			}
			finally
			{
				safeLocalAllocHandle.Close();
				safeLocalAllocHandle2.Close();
			}
		}

		// Token: 0x060028E5 RID: 10469 RVA: 0x000968F8 File Offset: 0x00094AF8
		[SecurityCritical]
		private void AddPrimarySidClaim(List<Claim> instanceClaims)
		{
			if (this.m_safeTokenHandle.IsInvalid)
			{
				return;
			}
			SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
			try
			{
				safeLocalAllocHandle = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, TokenInformationClass.TokenUser);
				Win32Native.SID_AND_ATTRIBUTES sid_AND_ATTRIBUTES = (Win32Native.SID_AND_ATTRIBUTES)Marshal.PtrToStructure(safeLocalAllocHandle.DangerousGetHandle(), typeof(Win32Native.SID_AND_ATTRIBUTES));
				uint num = 16U;
				SecurityIdentifier securityIdentifier = new SecurityIdentifier(sid_AND_ATTRIBUTES.Sid, true);
				if (sid_AND_ATTRIBUTES.Attributes == 0U)
				{
					instanceClaims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/primarysid", securityIdentifier.Value, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", Convert.ToString(securityIdentifier.IdentifierAuthority, CultureInfo.InvariantCulture)));
				}
				else if ((sid_AND_ATTRIBUTES.Attributes & num) == 16U)
				{
					instanceClaims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/denyonlyprimarysid", securityIdentifier.Value, "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, "http://schemas.microsoft.com/ws/2008/06/identity/claims/windowssubauthority", Convert.ToString(securityIdentifier.IdentifierAuthority, CultureInfo.InvariantCulture)));
				}
			}
			finally
			{
				safeLocalAllocHandle.Close();
			}
		}

		// Token: 0x060028E6 RID: 10470 RVA: 0x00096A04 File Offset: 0x00094C04
		[SecurityCritical]
		private void AddTokenClaims(List<Claim> instanceClaims, TokenInformationClass tokenInformationClass, string propertyValue)
		{
			if (this.m_safeTokenHandle.IsInvalid)
			{
				return;
			}
			SafeLocalAllocHandle safeLocalAllocHandle = SafeLocalAllocHandle.InvalidHandle;
			try
			{
				SafeLocalAllocHandle invalidHandle = SafeLocalAllocHandle.InvalidHandle;
				safeLocalAllocHandle = WindowsIdentity.GetTokenInformation(this.m_safeTokenHandle, tokenInformationClass);
				Win32Native.CLAIM_SECURITY_ATTRIBUTES_INFORMATION claim_SECURITY_ATTRIBUTES_INFORMATION = (Win32Native.CLAIM_SECURITY_ATTRIBUTES_INFORMATION)Marshal.PtrToStructure(safeLocalAllocHandle.DangerousGetHandle(), typeof(Win32Native.CLAIM_SECURITY_ATTRIBUTES_INFORMATION));
				long num = 0L;
				int num2 = 0;
				while ((long)num2 < (long)((ulong)claim_SECURITY_ATTRIBUTES_INFORMATION.AttributeCount))
				{
					IntPtr ptr = new IntPtr(claim_SECURITY_ATTRIBUTES_INFORMATION.Attribute.pAttributeV1.ToInt64() + num);
					Win32Native.CLAIM_SECURITY_ATTRIBUTE_V1 claim_SECURITY_ATTRIBUTE_V = (Win32Native.CLAIM_SECURITY_ATTRIBUTE_V1)Marshal.PtrToStructure(ptr, typeof(Win32Native.CLAIM_SECURITY_ATTRIBUTE_V1));
					switch (claim_SECURITY_ATTRIBUTE_V.ValueType)
					{
					case 1:
					{
						long[] array = new long[claim_SECURITY_ATTRIBUTE_V.ValueCount];
						Marshal.Copy(claim_SECURITY_ATTRIBUTE_V.Values.pInt64, array, 0, (int)claim_SECURITY_ATTRIBUTE_V.ValueCount);
						int num3 = 0;
						while ((long)num3 < (long)((ulong)claim_SECURITY_ATTRIBUTE_V.ValueCount))
						{
							instanceClaims.Add(new Claim(claim_SECURITY_ATTRIBUTE_V.Name, Convert.ToString(array[num3], CultureInfo.InvariantCulture), "http://www.w3.org/2001/XMLSchema#integer64", this.m_issuerName, this.m_issuerName, this, propertyValue, string.Empty));
							num3++;
						}
						break;
					}
					case 2:
					{
						long[] array2 = new long[claim_SECURITY_ATTRIBUTE_V.ValueCount];
						Marshal.Copy(claim_SECURITY_ATTRIBUTE_V.Values.pUint64, array2, 0, (int)claim_SECURITY_ATTRIBUTE_V.ValueCount);
						int num4 = 0;
						while ((long)num4 < (long)((ulong)claim_SECURITY_ATTRIBUTE_V.ValueCount))
						{
							instanceClaims.Add(new Claim(claim_SECURITY_ATTRIBUTE_V.Name, Convert.ToString((ulong)array2[num4], CultureInfo.InvariantCulture), "http://www.w3.org/2001/XMLSchema#uinteger64", this.m_issuerName, this.m_issuerName, this, propertyValue, string.Empty));
							num4++;
						}
						break;
					}
					case 3:
					{
						IntPtr[] array3 = new IntPtr[claim_SECURITY_ATTRIBUTE_V.ValueCount];
						Marshal.Copy(claim_SECURITY_ATTRIBUTE_V.Values.ppString, array3, 0, (int)claim_SECURITY_ATTRIBUTE_V.ValueCount);
						int num5 = 0;
						while ((long)num5 < (long)((ulong)claim_SECURITY_ATTRIBUTE_V.ValueCount))
						{
							instanceClaims.Add(new Claim(claim_SECURITY_ATTRIBUTE_V.Name, Marshal.PtrToStringAuto(array3[num5]), "http://www.w3.org/2001/XMLSchema#string", this.m_issuerName, this.m_issuerName, this, propertyValue, string.Empty));
							num5++;
						}
						break;
					}
					case 6:
					{
						long[] array4 = new long[claim_SECURITY_ATTRIBUTE_V.ValueCount];
						Marshal.Copy(claim_SECURITY_ATTRIBUTE_V.Values.pUint64, array4, 0, (int)claim_SECURITY_ATTRIBUTE_V.ValueCount);
						int num6 = 0;
						while ((long)num6 < (long)((ulong)claim_SECURITY_ATTRIBUTE_V.ValueCount))
						{
							instanceClaims.Add(new Claim(claim_SECURITY_ATTRIBUTE_V.Name, (array4[num6] == 0L) ? Convert.ToString(false, CultureInfo.InvariantCulture) : Convert.ToString(true, CultureInfo.InvariantCulture), "http://www.w3.org/2001/XMLSchema#boolean", this.m_issuerName, this.m_issuerName, this, propertyValue, string.Empty));
							num6++;
						}
						break;
					}
					}
					num += (long)Marshal.SizeOf<Win32Native.CLAIM_SECURITY_ATTRIBUTE_V1>(claim_SECURITY_ATTRIBUTE_V);
					num2++;
				}
			}
			finally
			{
				safeLocalAllocHandle.Close();
			}
		}

		// Token: 0x04001034 RID: 4148
		[SecurityCritical]
		private static SafeAccessTokenHandle s_invalidTokenHandle = SafeAccessTokenHandle.InvalidHandle;

		// Token: 0x04001035 RID: 4149
		private string m_name;

		// Token: 0x04001036 RID: 4150
		private SecurityIdentifier m_owner;

		// Token: 0x04001037 RID: 4151
		private SecurityIdentifier m_user;

		// Token: 0x04001038 RID: 4152
		private object m_groups;

		// Token: 0x04001039 RID: 4153
		[SecurityCritical]
		private SafeAccessTokenHandle m_safeTokenHandle = SafeAccessTokenHandle.InvalidHandle;

		// Token: 0x0400103A RID: 4154
		private string m_authType;

		// Token: 0x0400103B RID: 4155
		private int m_isAuthenticated = -1;

		// Token: 0x0400103C RID: 4156
		private volatile TokenImpersonationLevel m_impersonationLevel;

		// Token: 0x0400103D RID: 4157
		private volatile bool m_impersonationLevelInitialized;

		// Token: 0x0400103E RID: 4158
		private static RuntimeConstructorInfo s_specialSerializationCtor = typeof(WindowsIdentity).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(SerializationInfo) }, null) as RuntimeConstructorInfo;

		// Token: 0x0400103F RID: 4159
		[NonSerialized]
		public new const string DefaultIssuer = "AD AUTHORITY";

		// Token: 0x04001040 RID: 4160
		[NonSerialized]
		private string m_issuerName = "AD AUTHORITY";

		// Token: 0x04001041 RID: 4161
		[NonSerialized]
		private object m_claimsIntiailizedLock = new object();

		// Token: 0x04001042 RID: 4162
		[NonSerialized]
		private volatile bool m_claimsInitialized;

		// Token: 0x04001043 RID: 4163
		[NonSerialized]
		private List<Claim> m_deviceClaims;

		// Token: 0x04001044 RID: 4164
		[NonSerialized]
		private List<Claim> m_userClaims;
	}
}
