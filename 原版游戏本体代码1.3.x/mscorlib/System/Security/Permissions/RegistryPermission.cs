using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Util;

namespace System.Security.Permissions
{
	// Token: 0x0200031B RID: 795
	[ComVisible(true)]
	[Serializable]
	public sealed class RegistryPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
	{
		// Token: 0x06002802 RID: 10242 RVA: 0x00091EC9 File Offset: 0x000900C9
		public RegistryPermission(PermissionState state)
		{
			if (state == PermissionState.Unrestricted)
			{
				this.m_unrestricted = true;
				return;
			}
			if (state == PermissionState.None)
			{
				this.m_unrestricted = false;
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
		}

		// Token: 0x06002803 RID: 10243 RVA: 0x00091EF7 File Offset: 0x000900F7
		public RegistryPermission(RegistryPermissionAccess access, string pathList)
		{
			this.SetPathList(access, pathList);
		}

		// Token: 0x06002804 RID: 10244 RVA: 0x00091F07 File Offset: 0x00090107
		public RegistryPermission(RegistryPermissionAccess access, AccessControlActions control, string pathList)
		{
			this.m_unrestricted = false;
			this.AddPathList(access, control, pathList);
		}

		// Token: 0x06002805 RID: 10245 RVA: 0x00091F1F File Offset: 0x0009011F
		public void SetPathList(RegistryPermissionAccess access, string pathList)
		{
			this.VerifyAccess(access);
			this.m_unrestricted = false;
			if ((access & RegistryPermissionAccess.Read) != RegistryPermissionAccess.NoAccess)
			{
				this.m_read = null;
			}
			if ((access & RegistryPermissionAccess.Write) != RegistryPermissionAccess.NoAccess)
			{
				this.m_write = null;
			}
			if ((access & RegistryPermissionAccess.Create) != RegistryPermissionAccess.NoAccess)
			{
				this.m_create = null;
			}
			this.AddPathList(access, pathList);
		}

		// Token: 0x06002806 RID: 10246 RVA: 0x00091F5B File Offset: 0x0009015B
		internal void SetPathList(AccessControlActions control, string pathList)
		{
			this.m_unrestricted = false;
			if ((control & AccessControlActions.View) != AccessControlActions.None)
			{
				this.m_viewAcl = null;
			}
			if ((control & AccessControlActions.Change) != AccessControlActions.None)
			{
				this.m_changeAcl = null;
			}
			this.AddPathList(RegistryPermissionAccess.NoAccess, control, pathList);
		}

		// Token: 0x06002807 RID: 10247 RVA: 0x00091F85 File Offset: 0x00090185
		public void AddPathList(RegistryPermissionAccess access, string pathList)
		{
			this.AddPathList(access, AccessControlActions.None, pathList);
		}

		// Token: 0x06002808 RID: 10248 RVA: 0x00091F90 File Offset: 0x00090190
		[SecuritySafeCritical]
		public void AddPathList(RegistryPermissionAccess access, AccessControlActions control, string pathList)
		{
			this.VerifyAccess(access);
			if ((access & RegistryPermissionAccess.Read) != RegistryPermissionAccess.NoAccess)
			{
				if (this.m_read == null)
				{
					this.m_read = new StringExpressionSet();
				}
				this.m_read.AddExpressions(pathList);
			}
			if ((access & RegistryPermissionAccess.Write) != RegistryPermissionAccess.NoAccess)
			{
				if (this.m_write == null)
				{
					this.m_write = new StringExpressionSet();
				}
				this.m_write.AddExpressions(pathList);
			}
			if ((access & RegistryPermissionAccess.Create) != RegistryPermissionAccess.NoAccess)
			{
				if (this.m_create == null)
				{
					this.m_create = new StringExpressionSet();
				}
				this.m_create.AddExpressions(pathList);
			}
			if ((control & AccessControlActions.View) != AccessControlActions.None)
			{
				if (this.m_viewAcl == null)
				{
					this.m_viewAcl = new StringExpressionSet();
				}
				this.m_viewAcl.AddExpressions(pathList);
			}
			if ((control & AccessControlActions.Change) != AccessControlActions.None)
			{
				if (this.m_changeAcl == null)
				{
					this.m_changeAcl = new StringExpressionSet();
				}
				this.m_changeAcl.AddExpressions(pathList);
			}
		}

		// Token: 0x06002809 RID: 10249 RVA: 0x00092058 File Offset: 0x00090258
		[SecuritySafeCritical]
		public string GetPathList(RegistryPermissionAccess access)
		{
			this.VerifyAccess(access);
			this.ExclusiveAccess(access);
			if ((access & RegistryPermissionAccess.Read) != RegistryPermissionAccess.NoAccess)
			{
				if (this.m_read == null)
				{
					return "";
				}
				return this.m_read.UnsafeToString();
			}
			else if ((access & RegistryPermissionAccess.Write) != RegistryPermissionAccess.NoAccess)
			{
				if (this.m_write == null)
				{
					return "";
				}
				return this.m_write.UnsafeToString();
			}
			else
			{
				if ((access & RegistryPermissionAccess.Create) == RegistryPermissionAccess.NoAccess)
				{
					return "";
				}
				if (this.m_create == null)
				{
					return "";
				}
				return this.m_create.UnsafeToString();
			}
		}

		// Token: 0x0600280A RID: 10250 RVA: 0x000920D5 File Offset: 0x000902D5
		private void VerifyAccess(RegistryPermissionAccess access)
		{
			if ((access & ~(RegistryPermissionAccess.Read | RegistryPermissionAccess.Write | RegistryPermissionAccess.Create)) != RegistryPermissionAccess.NoAccess)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)access }));
			}
		}

		// Token: 0x0600280B RID: 10251 RVA: 0x000920FC File Offset: 0x000902FC
		private void ExclusiveAccess(RegistryPermissionAccess access)
		{
			if (access == RegistryPermissionAccess.NoAccess)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumNotSingleFlag"));
			}
			if ((access & (access - 1)) != RegistryPermissionAccess.NoAccess)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumNotSingleFlag"));
			}
		}

		// Token: 0x0600280C RID: 10252 RVA: 0x00092128 File Offset: 0x00090328
		private bool IsEmpty()
		{
			return !this.m_unrestricted && (this.m_read == null || this.m_read.IsEmpty()) && (this.m_write == null || this.m_write.IsEmpty()) && (this.m_create == null || this.m_create.IsEmpty()) && (this.m_viewAcl == null || this.m_viewAcl.IsEmpty()) && (this.m_changeAcl == null || this.m_changeAcl.IsEmpty());
		}

		// Token: 0x0600280D RID: 10253 RVA: 0x000921A8 File Offset: 0x000903A8
		public bool IsUnrestricted()
		{
			return this.m_unrestricted;
		}

		// Token: 0x0600280E RID: 10254 RVA: 0x000921B0 File Offset: 0x000903B0
		[SecuritySafeCritical]
		public override bool IsSubsetOf(IPermission target)
		{
			if (target == null)
			{
				return this.IsEmpty();
			}
			RegistryPermission registryPermission = target as RegistryPermission;
			if (registryPermission == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return registryPermission.IsUnrestricted() || (!this.IsUnrestricted() && ((this.m_read == null || this.m_read.IsSubsetOf(registryPermission.m_read)) && (this.m_write == null || this.m_write.IsSubsetOf(registryPermission.m_write)) && (this.m_create == null || this.m_create.IsSubsetOf(registryPermission.m_create)) && (this.m_viewAcl == null || this.m_viewAcl.IsSubsetOf(registryPermission.m_viewAcl))) && (this.m_changeAcl == null || this.m_changeAcl.IsSubsetOf(registryPermission.m_changeAcl)));
		}

		// Token: 0x0600280F RID: 10255 RVA: 0x00092294 File Offset: 0x00090494
		[SecuritySafeCritical]
		public override IPermission Intersect(IPermission target)
		{
			if (target == null)
			{
				return null;
			}
			if (!base.VerifyType(target))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			if (this.IsUnrestricted())
			{
				return target.Copy();
			}
			RegistryPermission registryPermission = (RegistryPermission)target;
			if (registryPermission.IsUnrestricted())
			{
				return this.Copy();
			}
			StringExpressionSet stringExpressionSet = ((this.m_read == null) ? null : this.m_read.Intersect(registryPermission.m_read));
			StringExpressionSet stringExpressionSet2 = ((this.m_write == null) ? null : this.m_write.Intersect(registryPermission.m_write));
			StringExpressionSet stringExpressionSet3 = ((this.m_create == null) ? null : this.m_create.Intersect(registryPermission.m_create));
			StringExpressionSet stringExpressionSet4 = ((this.m_viewAcl == null) ? null : this.m_viewAcl.Intersect(registryPermission.m_viewAcl));
			StringExpressionSet stringExpressionSet5 = ((this.m_changeAcl == null) ? null : this.m_changeAcl.Intersect(registryPermission.m_changeAcl));
			if ((stringExpressionSet == null || stringExpressionSet.IsEmpty()) && (stringExpressionSet2 == null || stringExpressionSet2.IsEmpty()) && (stringExpressionSet3 == null || stringExpressionSet3.IsEmpty()) && (stringExpressionSet4 == null || stringExpressionSet4.IsEmpty()) && (stringExpressionSet5 == null || stringExpressionSet5.IsEmpty()))
			{
				return null;
			}
			return new RegistryPermission(PermissionState.None)
			{
				m_unrestricted = false,
				m_read = stringExpressionSet,
				m_write = stringExpressionSet2,
				m_create = stringExpressionSet3,
				m_viewAcl = stringExpressionSet4,
				m_changeAcl = stringExpressionSet5
			};
		}

		// Token: 0x06002810 RID: 10256 RVA: 0x00092404 File Offset: 0x00090604
		[SecuritySafeCritical]
		public override IPermission Union(IPermission other)
		{
			if (other == null)
			{
				return this.Copy();
			}
			if (!base.VerifyType(other))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			RegistryPermission registryPermission = (RegistryPermission)other;
			if (this.IsUnrestricted() || registryPermission.IsUnrestricted())
			{
				return new RegistryPermission(PermissionState.Unrestricted);
			}
			StringExpressionSet stringExpressionSet = ((this.m_read == null) ? registryPermission.m_read : this.m_read.Union(registryPermission.m_read));
			StringExpressionSet stringExpressionSet2 = ((this.m_write == null) ? registryPermission.m_write : this.m_write.Union(registryPermission.m_write));
			StringExpressionSet stringExpressionSet3 = ((this.m_create == null) ? registryPermission.m_create : this.m_create.Union(registryPermission.m_create));
			StringExpressionSet stringExpressionSet4 = ((this.m_viewAcl == null) ? registryPermission.m_viewAcl : this.m_viewAcl.Union(registryPermission.m_viewAcl));
			StringExpressionSet stringExpressionSet5 = ((this.m_changeAcl == null) ? registryPermission.m_changeAcl : this.m_changeAcl.Union(registryPermission.m_changeAcl));
			if ((stringExpressionSet == null || stringExpressionSet.IsEmpty()) && (stringExpressionSet2 == null || stringExpressionSet2.IsEmpty()) && (stringExpressionSet3 == null || stringExpressionSet3.IsEmpty()) && (stringExpressionSet4 == null || stringExpressionSet4.IsEmpty()) && (stringExpressionSet5 == null || stringExpressionSet5.IsEmpty()))
			{
				return null;
			}
			return new RegistryPermission(PermissionState.None)
			{
				m_unrestricted = false,
				m_read = stringExpressionSet,
				m_write = stringExpressionSet2,
				m_create = stringExpressionSet3,
				m_viewAcl = stringExpressionSet4,
				m_changeAcl = stringExpressionSet5
			};
		}

		// Token: 0x06002811 RID: 10257 RVA: 0x0009258C File Offset: 0x0009078C
		public override IPermission Copy()
		{
			RegistryPermission registryPermission = new RegistryPermission(PermissionState.None);
			if (this.m_unrestricted)
			{
				registryPermission.m_unrestricted = true;
			}
			else
			{
				registryPermission.m_unrestricted = false;
				if (this.m_read != null)
				{
					registryPermission.m_read = this.m_read.Copy();
				}
				if (this.m_write != null)
				{
					registryPermission.m_write = this.m_write.Copy();
				}
				if (this.m_create != null)
				{
					registryPermission.m_create = this.m_create.Copy();
				}
				if (this.m_viewAcl != null)
				{
					registryPermission.m_viewAcl = this.m_viewAcl.Copy();
				}
				if (this.m_changeAcl != null)
				{
					registryPermission.m_changeAcl = this.m_changeAcl.Copy();
				}
			}
			return registryPermission;
		}

		// Token: 0x06002812 RID: 10258 RVA: 0x0009263C File Offset: 0x0009083C
		[SecuritySafeCritical]
		public override SecurityElement ToXml()
		{
			SecurityElement securityElement = CodeAccessPermission.CreatePermissionElement(this, "System.Security.Permissions.RegistryPermission");
			if (!this.IsUnrestricted())
			{
				if (this.m_read != null && !this.m_read.IsEmpty())
				{
					securityElement.AddAttribute("Read", SecurityElement.Escape(this.m_read.UnsafeToString()));
				}
				if (this.m_write != null && !this.m_write.IsEmpty())
				{
					securityElement.AddAttribute("Write", SecurityElement.Escape(this.m_write.UnsafeToString()));
				}
				if (this.m_create != null && !this.m_create.IsEmpty())
				{
					securityElement.AddAttribute("Create", SecurityElement.Escape(this.m_create.UnsafeToString()));
				}
				if (this.m_viewAcl != null && !this.m_viewAcl.IsEmpty())
				{
					securityElement.AddAttribute("ViewAccessControl", SecurityElement.Escape(this.m_viewAcl.UnsafeToString()));
				}
				if (this.m_changeAcl != null && !this.m_changeAcl.IsEmpty())
				{
					securityElement.AddAttribute("ChangeAccessControl", SecurityElement.Escape(this.m_changeAcl.UnsafeToString()));
				}
			}
			else
			{
				securityElement.AddAttribute("Unrestricted", "true");
			}
			return securityElement;
		}

		// Token: 0x06002813 RID: 10259 RVA: 0x00092764 File Offset: 0x00090964
		public override void FromXml(SecurityElement esd)
		{
			CodeAccessPermission.ValidateElement(esd, this);
			if (XMLUtil.IsUnrestricted(esd))
			{
				this.m_unrestricted = true;
				return;
			}
			this.m_unrestricted = false;
			this.m_read = null;
			this.m_write = null;
			this.m_create = null;
			this.m_viewAcl = null;
			this.m_changeAcl = null;
			string text = esd.Attribute("Read");
			if (text != null)
			{
				this.m_read = new StringExpressionSet(text);
			}
			text = esd.Attribute("Write");
			if (text != null)
			{
				this.m_write = new StringExpressionSet(text);
			}
			text = esd.Attribute("Create");
			if (text != null)
			{
				this.m_create = new StringExpressionSet(text);
			}
			text = esd.Attribute("ViewAccessControl");
			if (text != null)
			{
				this.m_viewAcl = new StringExpressionSet(text);
			}
			text = esd.Attribute("ChangeAccessControl");
			if (text != null)
			{
				this.m_changeAcl = new StringExpressionSet(text);
			}
		}

		// Token: 0x06002814 RID: 10260 RVA: 0x00092839 File Offset: 0x00090A39
		int IBuiltInPermission.GetTokenIndex()
		{
			return RegistryPermission.GetTokenIndex();
		}

		// Token: 0x06002815 RID: 10261 RVA: 0x00092840 File Offset: 0x00090A40
		internal static int GetTokenIndex()
		{
			return 5;
		}

		// Token: 0x04000F7E RID: 3966
		private StringExpressionSet m_read;

		// Token: 0x04000F7F RID: 3967
		private StringExpressionSet m_write;

		// Token: 0x04000F80 RID: 3968
		private StringExpressionSet m_create;

		// Token: 0x04000F81 RID: 3969
		[OptionalField(VersionAdded = 2)]
		private StringExpressionSet m_viewAcl;

		// Token: 0x04000F82 RID: 3970
		[OptionalField(VersionAdded = 2)]
		private StringExpressionSet m_changeAcl;

		// Token: 0x04000F83 RID: 3971
		private bool m_unrestricted;
	}
}
