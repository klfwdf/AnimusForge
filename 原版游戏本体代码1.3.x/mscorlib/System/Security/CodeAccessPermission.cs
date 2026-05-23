using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;
using System.Threading;

namespace System.Security
{
	// Token: 0x020001CE RID: 462
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, ControlEvidence = true, ControlPolicy = true)]
	[Serializable]
	public abstract class CodeAccessPermission : IPermission, ISecurityEncodable, IStackWalk
	{
		// Token: 0x06001C34 RID: 7220 RVA: 0x00061104 File Offset: 0x0005F304
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void RevertAssert()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.RevertAssert(ref stackCrawlMark);
		}

		// Token: 0x06001C35 RID: 7221 RVA: 0x0006111C File Offset: 0x0005F31C
		[SecuritySafeCritical]
		[Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void RevertDeny()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.RevertDeny(ref stackCrawlMark);
		}

		// Token: 0x06001C36 RID: 7222 RVA: 0x00061134 File Offset: 0x0005F334
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void RevertPermitOnly()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.RevertPermitOnly(ref stackCrawlMark);
		}

		// Token: 0x06001C37 RID: 7223 RVA: 0x0006114C File Offset: 0x0005F34C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void RevertAll()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.RevertAll(ref stackCrawlMark);
		}

		// Token: 0x06001C38 RID: 7224 RVA: 0x00061164 File Offset: 0x0005F364
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Demand()
		{
			if (!this.CheckDemand(null))
			{
				StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCallersCaller;
				CodeAccessSecurityEngine.Check(this, ref stackCrawlMark);
			}
		}

		// Token: 0x06001C39 RID: 7225 RVA: 0x00061184 File Offset: 0x0005F384
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void Demand(PermissionType permissionType)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCallersCaller;
			CodeAccessSecurityEngine.SpecialDemand(permissionType, ref stackCrawlMark);
		}

		// Token: 0x06001C3A RID: 7226 RVA: 0x0006119C File Offset: 0x0005F39C
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Assert()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			CodeAccessSecurityEngine.Assert(this, ref stackCrawlMark);
		}

		// Token: 0x06001C3B RID: 7227 RVA: 0x000611B4 File Offset: 0x0005F3B4
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void Assert(bool allPossible)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			SecurityRuntime.AssertAllPossible(ref stackCrawlMark);
		}

		// Token: 0x06001C3C RID: 7228 RVA: 0x000611CC File Offset: 0x0005F3CC
		[SecuritySafeCritical]
		[Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void Deny()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			CodeAccessSecurityEngine.Deny(this, ref stackCrawlMark);
		}

		// Token: 0x06001C3D RID: 7229 RVA: 0x000611E4 File Offset: 0x0005F3E4
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public void PermitOnly()
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			CodeAccessSecurityEngine.PermitOnly(this, ref stackCrawlMark);
		}

		// Token: 0x06001C3E RID: 7230 RVA: 0x000611FB File Offset: 0x0005F3FB
		public virtual IPermission Union(IPermission other)
		{
			if (other == null)
			{
				return this.Copy();
			}
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SecurityPermissionUnion"));
		}

		// Token: 0x06001C3F RID: 7231 RVA: 0x00061218 File Offset: 0x0005F418
		internal static SecurityElement CreatePermissionElement(IPermission perm, string permname)
		{
			SecurityElement securityElement = new SecurityElement("IPermission");
			XMLUtil.AddClassAttribute(securityElement, perm.GetType(), permname);
			securityElement.AddAttribute("version", "1");
			return securityElement;
		}

		// Token: 0x06001C40 RID: 7232 RVA: 0x00061250 File Offset: 0x0005F450
		internal static void ValidateElement(SecurityElement elem, IPermission perm)
		{
			if (elem == null)
			{
				throw new ArgumentNullException("elem");
			}
			if (!XMLUtil.IsPermissionElement(perm, elem))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_NotAPermissionElement"));
			}
			string text = elem.Attribute("version");
			if (text != null && !text.Equals("1"))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidXMLBadVersion"));
			}
		}

		// Token: 0x06001C41 RID: 7233
		public abstract SecurityElement ToXml();

		// Token: 0x06001C42 RID: 7234
		public abstract void FromXml(SecurityElement elem);

		// Token: 0x06001C43 RID: 7235 RVA: 0x000612B0 File Offset: 0x0005F4B0
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x06001C44 RID: 7236 RVA: 0x000612BD File Offset: 0x0005F4BD
		internal bool VerifyType(IPermission perm)
		{
			return perm != null && !(perm.GetType() != base.GetType());
		}

		// Token: 0x06001C45 RID: 7237
		public abstract IPermission Copy();

		// Token: 0x06001C46 RID: 7238
		public abstract IPermission Intersect(IPermission target);

		// Token: 0x06001C47 RID: 7239
		public abstract bool IsSubsetOf(IPermission target);

		// Token: 0x06001C48 RID: 7240 RVA: 0x000612D8 File Offset: 0x0005F4D8
		[ComVisible(false)]
		public override bool Equals(object obj)
		{
			IPermission permission = obj as IPermission;
			if (obj != null && permission == null)
			{
				return false;
			}
			try
			{
				if (!this.IsSubsetOf(permission))
				{
					return false;
				}
				if (permission != null && !permission.IsSubsetOf(this))
				{
					return false;
				}
			}
			catch (ArgumentException)
			{
				return false;
			}
			return true;
		}

		// Token: 0x06001C49 RID: 7241 RVA: 0x0006132C File Offset: 0x0005F52C
		[ComVisible(false)]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06001C4A RID: 7242 RVA: 0x00061334 File Offset: 0x0005F534
		internal bool CheckDemand(CodeAccessPermission grant)
		{
			return this.IsSubsetOf(grant);
		}

		// Token: 0x06001C4B RID: 7243 RVA: 0x0006133D File Offset: 0x0005F53D
		internal bool CheckPermitOnly(CodeAccessPermission permitted)
		{
			return this.IsSubsetOf(permitted);
		}

		// Token: 0x06001C4C RID: 7244 RVA: 0x00061348 File Offset: 0x0005F548
		internal bool CheckDeny(CodeAccessPermission denied)
		{
			IPermission permission = this.Intersect(denied);
			return permission == null || permission.IsSubsetOf(null);
		}

		// Token: 0x06001C4D RID: 7245 RVA: 0x00061369 File Offset: 0x0005F569
		internal bool CheckAssert(CodeAccessPermission asserted)
		{
			return this.IsSubsetOf(asserted);
		}
	}
}
