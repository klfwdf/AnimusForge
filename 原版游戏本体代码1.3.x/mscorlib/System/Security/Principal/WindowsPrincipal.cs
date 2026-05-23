using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Permissions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Principal
{
	// Token: 0x02000333 RID: 819
	[ComVisible(true)]
	[HostProtection(SecurityAction.LinkDemand, SecurityInfrastructure = true)]
	[Serializable]
	public class WindowsPrincipal : ClaimsPrincipal
	{
		// Token: 0x060028EE RID: 10478 RVA: 0x00096EF4 File Offset: 0x000950F4
		private WindowsPrincipal()
		{
		}

		// Token: 0x060028EF RID: 10479 RVA: 0x00096EFC File Offset: 0x000950FC
		public WindowsPrincipal(WindowsIdentity ntIdentity)
			: base(ntIdentity)
		{
			if (ntIdentity == null)
			{
				throw new ArgumentNullException("ntIdentity");
			}
			this.m_identity = ntIdentity;
		}

		// Token: 0x060028F0 RID: 10480 RVA: 0x00096F1C File Offset: 0x0009511C
		[OnDeserialized]
		[SecuritySafeCritical]
		private void OnDeserializedMethod(StreamingContext context)
		{
			ClaimsIdentity claimsIdentity = null;
			foreach (ClaimsIdentity claimsIdentity2 in base.Identities)
			{
				if (claimsIdentity2 != null)
				{
					claimsIdentity = claimsIdentity2;
					break;
				}
			}
			if (claimsIdentity == null)
			{
				base.AddIdentity(this.m_identity);
			}
		}

		// Token: 0x17000552 RID: 1362
		// (get) Token: 0x060028F1 RID: 10481 RVA: 0x00096F7C File Offset: 0x0009517C
		public override IIdentity Identity
		{
			get
			{
				return this.m_identity;
			}
		}

		// Token: 0x060028F2 RID: 10482 RVA: 0x00096F84 File Offset: 0x00095184
		[SecuritySafeCritical]
		[SecurityPermission(SecurityAction.Demand, ControlPrincipal = true)]
		public override bool IsInRole(string role)
		{
			if (role == null || role.Length == 0)
			{
				return false;
			}
			NTAccount identity = new NTAccount(role);
			IdentityReferenceCollection identityReferenceCollection = NTAccount.Translate(new IdentityReferenceCollection(1) { identity }, typeof(SecurityIdentifier), false);
			SecurityIdentifier securityIdentifier = identityReferenceCollection[0] as SecurityIdentifier;
			return (securityIdentifier != null && this.IsInRole(securityIdentifier)) || base.IsInRole(role);
		}

		// Token: 0x17000553 RID: 1363
		// (get) Token: 0x060028F3 RID: 10483 RVA: 0x00096FF0 File Offset: 0x000951F0
		public virtual IEnumerable<Claim> UserClaims
		{
			get
			{
				foreach (ClaimsIdentity claimsIdentity in this.Identities)
				{
					WindowsIdentity windowsIdentity = claimsIdentity as WindowsIdentity;
					if (windowsIdentity != null)
					{
						foreach (Claim claim in windowsIdentity.UserClaims)
						{
							yield return claim;
						}
						IEnumerator<Claim> enumerator2 = null;
					}
				}
				IEnumerator<ClaimsIdentity> enumerator = null;
				yield break;
				yield break;
			}
		}

		// Token: 0x17000554 RID: 1364
		// (get) Token: 0x060028F4 RID: 10484 RVA: 0x00097010 File Offset: 0x00095210
		public virtual IEnumerable<Claim> DeviceClaims
		{
			get
			{
				foreach (ClaimsIdentity claimsIdentity in this.Identities)
				{
					WindowsIdentity windowsIdentity = claimsIdentity as WindowsIdentity;
					if (windowsIdentity != null)
					{
						foreach (Claim claim in windowsIdentity.DeviceClaims)
						{
							yield return claim;
						}
						IEnumerator<Claim> enumerator2 = null;
					}
				}
				IEnumerator<ClaimsIdentity> enumerator = null;
				yield break;
				yield break;
			}
		}

		// Token: 0x060028F5 RID: 10485 RVA: 0x0009702D File Offset: 0x0009522D
		public virtual bool IsInRole(WindowsBuiltInRole role)
		{
			if (role < WindowsBuiltInRole.Administrator || role > WindowsBuiltInRole.Replicator)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)role }), "role");
			}
			return this.IsInRole((int)role);
		}

		// Token: 0x060028F6 RID: 10486 RVA: 0x0009706C File Offset: 0x0009526C
		public virtual bool IsInRole(int rid)
		{
			SecurityIdentifier sid = new SecurityIdentifier(IdentifierAuthority.NTAuthority, new int[] { 32, rid });
			return this.IsInRole(sid);
		}

		// Token: 0x060028F7 RID: 10487 RVA: 0x00097098 File Offset: 0x00095298
		[SecuritySafeCritical]
		[ComVisible(false)]
		public virtual bool IsInRole(SecurityIdentifier sid)
		{
			if (sid == null)
			{
				throw new ArgumentNullException("sid");
			}
			if (this.m_identity.AccessToken.IsInvalid)
			{
				return false;
			}
			SafeAccessTokenHandle invalidHandle = SafeAccessTokenHandle.InvalidHandle;
			if (this.m_identity.ImpersonationLevel == TokenImpersonationLevel.None && !Win32Native.DuplicateTokenEx(this.m_identity.AccessToken, 8U, IntPtr.Zero, 2U, 2U, ref invalidHandle))
			{
				throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
			}
			bool result = false;
			if (!Win32Native.CheckTokenMembership((this.m_identity.ImpersonationLevel != TokenImpersonationLevel.None) ? this.m_identity.AccessToken : invalidHandle, sid.BinaryForm, ref result))
			{
				throw new SecurityException(Win32Native.GetMessage(Marshal.GetLastWin32Error()));
			}
			invalidHandle.Dispose();
			return result;
		}

		// Token: 0x0400108D RID: 4237
		private WindowsIdentity m_identity;

		// Token: 0x0400108E RID: 4238
		private string[] m_roles;

		// Token: 0x0400108F RID: 4239
		private Hashtable m_rolesTable;

		// Token: 0x04001090 RID: 4240
		private bool m_rolesLoaded;
	}
}
