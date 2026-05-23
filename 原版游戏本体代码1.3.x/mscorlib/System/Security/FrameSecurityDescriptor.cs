using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security
{
	// Token: 0x020001D6 RID: 470
	[Serializable]
	internal class FrameSecurityDescriptor
	{
		// Token: 0x06001C7C RID: 7292
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void IncrementOverridesCount();

		// Token: 0x06001C7D RID: 7293
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void DecrementOverridesCount();

		// Token: 0x06001C7E RID: 7294
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void IncrementAssertCount();

		// Token: 0x06001C7F RID: 7295
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void DecrementAssertCount();

		// Token: 0x06001C80 RID: 7296 RVA: 0x00061ADF File Offset: 0x0005FCDF
		internal FrameSecurityDescriptor()
		{
		}

		// Token: 0x06001C81 RID: 7297 RVA: 0x00061AE8 File Offset: 0x0005FCE8
		private PermissionSet CreateSingletonSet(IPermission perm)
		{
			PermissionSet permissionSet = new PermissionSet(false);
			permissionSet.AddPermission(perm.Copy());
			return permissionSet;
		}

		// Token: 0x06001C82 RID: 7298 RVA: 0x00061B0A File Offset: 0x0005FD0A
		internal bool HasImperativeAsserts()
		{
			return this.m_assertions != null;
		}

		// Token: 0x06001C83 RID: 7299 RVA: 0x00061B15 File Offset: 0x0005FD15
		internal bool HasImperativeDenials()
		{
			return this.m_denials != null;
		}

		// Token: 0x06001C84 RID: 7300 RVA: 0x00061B20 File Offset: 0x0005FD20
		internal bool HasImperativeRestrictions()
		{
			return this.m_restriction != null;
		}

		// Token: 0x06001C85 RID: 7301 RVA: 0x00061B2B File Offset: 0x0005FD2B
		[SecurityCritical]
		internal void SetAssert(IPermission perm)
		{
			this.m_assertions = this.CreateSingletonSet(perm);
			FrameSecurityDescriptor.IncrementAssertCount();
		}

		// Token: 0x06001C86 RID: 7302 RVA: 0x00061B3F File Offset: 0x0005FD3F
		[SecurityCritical]
		internal void SetAssert(PermissionSet permSet)
		{
			this.m_assertions = permSet.Copy();
			this.m_AssertFT = this.m_AssertFT || this.m_assertions.IsUnrestricted();
			FrameSecurityDescriptor.IncrementAssertCount();
		}

		// Token: 0x06001C87 RID: 7303 RVA: 0x00061B6E File Offset: 0x0005FD6E
		internal PermissionSet GetAssertions(bool fDeclarative)
		{
			if (!fDeclarative)
			{
				return this.m_assertions;
			}
			return this.m_DeclarativeAssertions;
		}

		// Token: 0x06001C88 RID: 7304 RVA: 0x00061B80 File Offset: 0x0005FD80
		[SecurityCritical]
		internal void SetAssertAllPossible()
		{
			this.m_assertAllPossible = true;
			FrameSecurityDescriptor.IncrementAssertCount();
		}

		// Token: 0x06001C89 RID: 7305 RVA: 0x00061B8E File Offset: 0x0005FD8E
		internal bool GetAssertAllPossible()
		{
			return this.m_assertAllPossible;
		}

		// Token: 0x06001C8A RID: 7306 RVA: 0x00061B96 File Offset: 0x0005FD96
		[SecurityCritical]
		internal void SetDeny(IPermission perm)
		{
			this.m_denials = this.CreateSingletonSet(perm);
			FrameSecurityDescriptor.IncrementOverridesCount();
		}

		// Token: 0x06001C8B RID: 7307 RVA: 0x00061BAA File Offset: 0x0005FDAA
		[SecurityCritical]
		internal void SetDeny(PermissionSet permSet)
		{
			this.m_denials = permSet.Copy();
			FrameSecurityDescriptor.IncrementOverridesCount();
		}

		// Token: 0x06001C8C RID: 7308 RVA: 0x00061BBD File Offset: 0x0005FDBD
		internal PermissionSet GetDenials(bool fDeclarative)
		{
			if (!fDeclarative)
			{
				return this.m_denials;
			}
			return this.m_DeclarativeDenials;
		}

		// Token: 0x06001C8D RID: 7309 RVA: 0x00061BCF File Offset: 0x0005FDCF
		[SecurityCritical]
		internal void SetPermitOnly(IPermission perm)
		{
			this.m_restriction = this.CreateSingletonSet(perm);
			FrameSecurityDescriptor.IncrementOverridesCount();
		}

		// Token: 0x06001C8E RID: 7310 RVA: 0x00061BE3 File Offset: 0x0005FDE3
		[SecurityCritical]
		internal void SetPermitOnly(PermissionSet permSet)
		{
			this.m_restriction = permSet.Copy();
			FrameSecurityDescriptor.IncrementOverridesCount();
		}

		// Token: 0x06001C8F RID: 7311 RVA: 0x00061BF6 File Offset: 0x0005FDF6
		internal PermissionSet GetPermitOnly(bool fDeclarative)
		{
			if (!fDeclarative)
			{
				return this.m_restriction;
			}
			return this.m_DeclarativeRestrictions;
		}

		// Token: 0x06001C90 RID: 7312 RVA: 0x00061C08 File Offset: 0x0005FE08
		[SecurityCritical]
		internal void SetTokenHandles(SafeAccessTokenHandle callerToken, SafeAccessTokenHandle impToken)
		{
			if (this.m_callerToken != null && !this.m_callerToken.IsInvalid)
			{
				this.m_callerToken.Dispose();
			}
			this.m_callerToken = callerToken;
			this.m_impToken = impToken;
		}

		// Token: 0x06001C91 RID: 7313 RVA: 0x00061C38 File Offset: 0x0005FE38
		[SecurityCritical]
		internal void RevertAssert()
		{
			if (this.m_assertions != null)
			{
				this.m_assertions = null;
				FrameSecurityDescriptor.DecrementAssertCount();
			}
			if (this.m_DeclarativeAssertions != null)
			{
				this.m_AssertFT = this.m_DeclarativeAssertions.IsUnrestricted();
				return;
			}
			this.m_AssertFT = false;
		}

		// Token: 0x06001C92 RID: 7314 RVA: 0x00061C6F File Offset: 0x0005FE6F
		[SecurityCritical]
		internal void RevertAssertAllPossible()
		{
			if (this.m_assertAllPossible)
			{
				this.m_assertAllPossible = false;
				FrameSecurityDescriptor.DecrementAssertCount();
			}
		}

		// Token: 0x06001C93 RID: 7315 RVA: 0x00061C85 File Offset: 0x0005FE85
		[SecurityCritical]
		internal void RevertDeny()
		{
			if (this.HasImperativeDenials())
			{
				FrameSecurityDescriptor.DecrementOverridesCount();
				this.m_denials = null;
			}
		}

		// Token: 0x06001C94 RID: 7316 RVA: 0x00061C9B File Offset: 0x0005FE9B
		[SecurityCritical]
		internal void RevertPermitOnly()
		{
			if (this.HasImperativeRestrictions())
			{
				FrameSecurityDescriptor.DecrementOverridesCount();
				this.m_restriction = null;
			}
		}

		// Token: 0x06001C95 RID: 7317 RVA: 0x00061CB1 File Offset: 0x0005FEB1
		[SecurityCritical]
		internal void RevertAll()
		{
			this.RevertAssert();
			this.RevertAssertAllPossible();
			this.RevertDeny();
			this.RevertPermitOnly();
		}

		// Token: 0x06001C96 RID: 7318 RVA: 0x00061CCC File Offset: 0x0005FECC
		[SecurityCritical]
		internal bool CheckDemand(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
		{
			bool flag = this.CheckDemand2(demand, permToken, rmh, false);
			if (flag)
			{
				flag = this.CheckDemand2(demand, permToken, rmh, true);
			}
			return flag;
		}

		// Token: 0x06001C97 RID: 7319 RVA: 0x00061CF4 File Offset: 0x0005FEF4
		[SecurityCritical]
		internal bool CheckDemand2(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh, bool fDeclarative)
		{
			if (this.GetPermitOnly(fDeclarative) != null)
			{
				this.GetPermitOnly(fDeclarative).CheckDecoded(demand, permToken);
			}
			if (this.GetDenials(fDeclarative) != null)
			{
				this.GetDenials(fDeclarative).CheckDecoded(demand, permToken);
			}
			if (this.GetAssertions(fDeclarative) != null)
			{
				this.GetAssertions(fDeclarative).CheckDecoded(demand, permToken);
			}
			bool flag = SecurityManager._SetThreadSecurity(false);
			try
			{
				PermissionSet permissionSet = this.GetPermitOnly(fDeclarative);
				if (permissionSet != null)
				{
					CodeAccessPermission codeAccessPermission = (CodeAccessPermission)permissionSet.GetPermission(demand);
					if (codeAccessPermission == null)
					{
						if (!permissionSet.IsUnrestricted())
						{
							throw new SecurityException(string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), null, permissionSet, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
						}
					}
					else
					{
						bool flag2 = true;
						try
						{
							flag2 = !demand.CheckPermitOnly(codeAccessPermission);
						}
						catch (ArgumentException)
						{
						}
						if (flag2)
						{
							throw new SecurityException(string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), null, permissionSet, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
						}
					}
				}
				permissionSet = this.GetDenials(fDeclarative);
				if (permissionSet != null)
				{
					CodeAccessPermission denied = (CodeAccessPermission)permissionSet.GetPermission(demand);
					if (permissionSet.IsUnrestricted())
					{
						throw new SecurityException(string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), permissionSet, null, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
					}
					bool flag3 = true;
					try
					{
						flag3 = !demand.CheckDeny(denied);
					}
					catch (ArgumentException)
					{
					}
					if (flag3)
					{
						throw new SecurityException(string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), permissionSet, null, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
					}
				}
				if (this.GetAssertAllPossible())
				{
					return false;
				}
				permissionSet = this.GetAssertions(fDeclarative);
				if (permissionSet != null)
				{
					CodeAccessPermission asserted = (CodeAccessPermission)permissionSet.GetPermission(demand);
					try
					{
						if (permissionSet.IsUnrestricted() || demand.CheckAssert(asserted))
						{
							return false;
						}
					}
					catch (ArgumentException)
					{
					}
				}
			}
			finally
			{
				if (flag)
				{
					SecurityManager._SetThreadSecurity(true);
				}
			}
			return true;
		}

		// Token: 0x06001C98 RID: 7320 RVA: 0x00061F44 File Offset: 0x00060144
		[SecurityCritical]
		internal bool CheckSetDemand(PermissionSet demandSet, out PermissionSet alteredDemandSet, RuntimeMethodHandleInternal rmh)
		{
			PermissionSet permissionSet = null;
			PermissionSet permissionSet2 = null;
			bool flag = this.CheckSetDemand2(demandSet, out permissionSet, rmh, false);
			if (permissionSet != null)
			{
				demandSet = permissionSet;
			}
			if (flag)
			{
				flag = this.CheckSetDemand2(demandSet, out permissionSet2, rmh, true);
			}
			if (permissionSet2 != null)
			{
				alteredDemandSet = permissionSet2;
			}
			else if (permissionSet != null)
			{
				alteredDemandSet = permissionSet;
			}
			else
			{
				alteredDemandSet = null;
			}
			return flag;
		}

		// Token: 0x06001C99 RID: 7321 RVA: 0x00061F8C File Offset: 0x0006018C
		[SecurityCritical]
		internal bool CheckSetDemand2(PermissionSet demandSet, out PermissionSet alteredDemandSet, RuntimeMethodHandleInternal rmh, bool fDeclarative)
		{
			alteredDemandSet = null;
			if (demandSet == null || demandSet.IsEmpty())
			{
				return false;
			}
			if (this.GetPermitOnly(fDeclarative) != null)
			{
				this.GetPermitOnly(fDeclarative).CheckDecoded(demandSet);
			}
			if (this.GetDenials(fDeclarative) != null)
			{
				this.GetDenials(fDeclarative).CheckDecoded(demandSet);
			}
			if (this.GetAssertions(fDeclarative) != null)
			{
				this.GetAssertions(fDeclarative).CheckDecoded(demandSet);
			}
			bool flag = SecurityManager._SetThreadSecurity(false);
			try
			{
				PermissionSet permissionSet = this.GetPermitOnly(fDeclarative);
				if (permissionSet != null)
				{
					IPermission permThatFailed = null;
					bool flag2 = true;
					try
					{
						flag2 = !demandSet.CheckPermitOnly(permissionSet, out permThatFailed);
					}
					catch (ArgumentException)
					{
					}
					if (flag2)
					{
						throw new SecurityException(Environment.GetResourceString("Security_GenericNoType"), null, permissionSet, SecurityRuntime.GetMethodInfo(rmh), demandSet, permThatFailed);
					}
				}
				permissionSet = this.GetDenials(fDeclarative);
				if (permissionSet != null)
				{
					IPermission permThatFailed2 = null;
					bool flag3 = true;
					try
					{
						flag3 = !demandSet.CheckDeny(permissionSet, out permThatFailed2);
					}
					catch (ArgumentException)
					{
					}
					if (flag3)
					{
						throw new SecurityException(Environment.GetResourceString("Security_GenericNoType"), permissionSet, null, SecurityRuntime.GetMethodInfo(rmh), demandSet, permThatFailed2);
					}
				}
				if (this.GetAssertAllPossible())
				{
					return false;
				}
				permissionSet = this.GetAssertions(fDeclarative);
				if (permissionSet != null)
				{
					if (demandSet.CheckAssertion(permissionSet))
					{
						return false;
					}
					if (!permissionSet.IsUnrestricted())
					{
						PermissionSet.RemoveAssertedPermissionSet(demandSet, permissionSet, out alteredDemandSet);
					}
				}
			}
			finally
			{
				if (flag)
				{
					SecurityManager._SetThreadSecurity(true);
				}
			}
			return true;
		}

		// Token: 0x040009F1 RID: 2545
		private PermissionSet m_assertions;

		// Token: 0x040009F2 RID: 2546
		private PermissionSet m_denials;

		// Token: 0x040009F3 RID: 2547
		private PermissionSet m_restriction;

		// Token: 0x040009F4 RID: 2548
		private PermissionSet m_DeclarativeAssertions;

		// Token: 0x040009F5 RID: 2549
		private PermissionSet m_DeclarativeDenials;

		// Token: 0x040009F6 RID: 2550
		private PermissionSet m_DeclarativeRestrictions;

		// Token: 0x040009F7 RID: 2551
		[SecurityCritical]
		[NonSerialized]
		private SafeAccessTokenHandle m_callerToken;

		// Token: 0x040009F8 RID: 2552
		[SecurityCritical]
		[NonSerialized]
		private SafeAccessTokenHandle m_impToken;

		// Token: 0x040009F9 RID: 2553
		private bool m_AssertFT;

		// Token: 0x040009FA RID: 2554
		private bool m_assertAllPossible;

		// Token: 0x040009FB RID: 2555
		private bool m_declSecComputed;
	}
}
