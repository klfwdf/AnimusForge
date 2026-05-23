using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.Principal;
using System.Security.Util;
using System.Threading;

namespace System.Security.Permissions
{
	// Token: 0x02000305 RID: 773
	[ComVisible(true)]
	[Serializable]
	public sealed class PrincipalPermission : IPermission, ISecurityEncodable, IUnrestrictedPermission, IBuiltInPermission
	{
		// Token: 0x06002723 RID: 10019 RVA: 0x0008D728 File Offset: 0x0008B928
		public PrincipalPermission(PermissionState state)
		{
			if (state == PermissionState.Unrestricted)
			{
				this.m_array = new IDRole[1];
				this.m_array[0] = new IDRole();
				this.m_array[0].m_authenticated = true;
				this.m_array[0].m_id = null;
				this.m_array[0].m_role = null;
				return;
			}
			if (state == PermissionState.None)
			{
				this.m_array = new IDRole[1];
				this.m_array[0] = new IDRole();
				this.m_array[0].m_authenticated = false;
				this.m_array[0].m_id = "";
				this.m_array[0].m_role = "";
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
		}

		// Token: 0x06002724 RID: 10020 RVA: 0x0008D7E4 File Offset: 0x0008B9E4
		public PrincipalPermission(string name, string role)
		{
			this.m_array = new IDRole[1];
			this.m_array[0] = new IDRole();
			this.m_array[0].m_authenticated = true;
			this.m_array[0].m_id = name;
			this.m_array[0].m_role = role;
		}

		// Token: 0x06002725 RID: 10021 RVA: 0x0008D83C File Offset: 0x0008BA3C
		public PrincipalPermission(string name, string role, bool isAuthenticated)
		{
			this.m_array = new IDRole[1];
			this.m_array[0] = new IDRole();
			this.m_array[0].m_authenticated = isAuthenticated;
			this.m_array[0].m_id = name;
			this.m_array[0].m_role = role;
		}

		// Token: 0x06002726 RID: 10022 RVA: 0x0008D892 File Offset: 0x0008BA92
		private PrincipalPermission(IDRole[] array)
		{
			this.m_array = array;
		}

		// Token: 0x06002727 RID: 10023 RVA: 0x0008D8A4 File Offset: 0x0008BAA4
		private bool IsEmpty()
		{
			for (int i = 0; i < this.m_array.Length; i++)
			{
				if (this.m_array[i].m_id == null || !this.m_array[i].m_id.Equals("") || this.m_array[i].m_role == null || !this.m_array[i].m_role.Equals("") || this.m_array[i].m_authenticated)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002728 RID: 10024 RVA: 0x0008D926 File Offset: 0x0008BB26
		private bool VerifyType(IPermission perm)
		{
			return perm != null && !(perm.GetType() != base.GetType());
		}

		// Token: 0x06002729 RID: 10025 RVA: 0x0008D944 File Offset: 0x0008BB44
		public bool IsUnrestricted()
		{
			for (int i = 0; i < this.m_array.Length; i++)
			{
				if (this.m_array[i].m_id != null || this.m_array[i].m_role != null || !this.m_array[i].m_authenticated)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x0600272A RID: 10026 RVA: 0x0008D994 File Offset: 0x0008BB94
		public bool IsSubsetOf(IPermission target)
		{
			if (target == null)
			{
				return this.IsEmpty();
			}
			bool result;
			try
			{
				PrincipalPermission principalPermission = (PrincipalPermission)target;
				if (principalPermission.IsUnrestricted())
				{
					result = true;
				}
				else if (this.IsUnrestricted())
				{
					result = false;
				}
				else
				{
					for (int i = 0; i < this.m_array.Length; i++)
					{
						bool flag = false;
						for (int j = 0; j < principalPermission.m_array.Length; j++)
						{
							if (principalPermission.m_array[j].m_authenticated == this.m_array[i].m_authenticated && (principalPermission.m_array[j].m_id == null || (this.m_array[i].m_id != null && this.m_array[i].m_id.Equals(principalPermission.m_array[j].m_id))) && (principalPermission.m_array[j].m_role == null || (this.m_array[i].m_role != null && this.m_array[i].m_role.Equals(principalPermission.m_array[j].m_role))))
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							return false;
						}
					}
					result = true;
				}
			}
			catch (InvalidCastException)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return result;
		}

		// Token: 0x0600272B RID: 10027 RVA: 0x0008DAFC File Offset: 0x0008BCFC
		public IPermission Intersect(IPermission target)
		{
			if (target == null)
			{
				return null;
			}
			if (!this.VerifyType(target))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			if (this.IsUnrestricted())
			{
				return target.Copy();
			}
			PrincipalPermission principalPermission = (PrincipalPermission)target;
			if (principalPermission.IsUnrestricted())
			{
				return this.Copy();
			}
			List<IDRole> list = null;
			for (int i = 0; i < this.m_array.Length; i++)
			{
				for (int j = 0; j < principalPermission.m_array.Length; j++)
				{
					if (principalPermission.m_array[j].m_authenticated == this.m_array[i].m_authenticated)
					{
						if (principalPermission.m_array[j].m_id == null || this.m_array[i].m_id == null || this.m_array[i].m_id.Equals(principalPermission.m_array[j].m_id))
						{
							if (list == null)
							{
								list = new List<IDRole>();
							}
							IDRole idrole = new IDRole();
							idrole.m_id = ((principalPermission.m_array[j].m_id == null) ? this.m_array[i].m_id : principalPermission.m_array[j].m_id);
							if (principalPermission.m_array[j].m_role == null || this.m_array[i].m_role == null || this.m_array[i].m_role.Equals(principalPermission.m_array[j].m_role))
							{
								idrole.m_role = ((principalPermission.m_array[j].m_role == null) ? this.m_array[i].m_role : principalPermission.m_array[j].m_role);
							}
							else
							{
								idrole.m_role = "";
							}
							idrole.m_authenticated = principalPermission.m_array[j].m_authenticated;
							list.Add(idrole);
						}
						else if (principalPermission.m_array[j].m_role == null || this.m_array[i].m_role == null || this.m_array[i].m_role.Equals(principalPermission.m_array[j].m_role))
						{
							if (list == null)
							{
								list = new List<IDRole>();
							}
							list.Add(new IDRole
							{
								m_id = "",
								m_role = ((principalPermission.m_array[j].m_role == null) ? this.m_array[i].m_role : principalPermission.m_array[j].m_role),
								m_authenticated = principalPermission.m_array[j].m_authenticated
							});
						}
					}
				}
			}
			if (list == null)
			{
				return null;
			}
			IDRole[] array = new IDRole[list.Count];
			IEnumerator enumerator = list.GetEnumerator();
			int num = 0;
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				array[num++] = (IDRole)obj;
			}
			return new PrincipalPermission(array);
		}

		// Token: 0x0600272C RID: 10028 RVA: 0x0008DDCC File Offset: 0x0008BFCC
		public IPermission Union(IPermission other)
		{
			if (other == null)
			{
				return this.Copy();
			}
			if (!this.VerifyType(other))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			PrincipalPermission principalPermission = (PrincipalPermission)other;
			if (this.IsUnrestricted() || principalPermission.IsUnrestricted())
			{
				return new PrincipalPermission(PermissionState.Unrestricted);
			}
			int num = this.m_array.Length + principalPermission.m_array.Length;
			IDRole[] array = new IDRole[num];
			int i;
			for (i = 0; i < this.m_array.Length; i++)
			{
				array[i] = this.m_array[i];
			}
			for (int j = 0; j < principalPermission.m_array.Length; j++)
			{
				array[i + j] = principalPermission.m_array[j];
			}
			return new PrincipalPermission(array);
		}

		// Token: 0x0600272D RID: 10029 RVA: 0x0008DE94 File Offset: 0x0008C094
		[ComVisible(false)]
		public override bool Equals(object obj)
		{
			IPermission permission = obj as IPermission;
			return (obj == null || permission != null) && this.IsSubsetOf(permission) && (permission == null || permission.IsSubsetOf(this));
		}

		// Token: 0x0600272E RID: 10030 RVA: 0x0008DECC File Offset: 0x0008C0CC
		[ComVisible(false)]
		public override int GetHashCode()
		{
			int num = 0;
			for (int i = 0; i < this.m_array.Length; i++)
			{
				num += this.m_array[i].GetHashCode();
			}
			return num;
		}

		// Token: 0x0600272F RID: 10031 RVA: 0x0008DEFF File Offset: 0x0008C0FF
		public IPermission Copy()
		{
			return new PrincipalPermission(this.m_array);
		}

		// Token: 0x06002730 RID: 10032 RVA: 0x0008DF0C File Offset: 0x0008C10C
		[SecurityCritical]
		private void ThrowSecurityException()
		{
			AssemblyName assemblyName = null;
			Evidence evidence = null;
			PermissionSet.s_fullTrust.Assert();
			try
			{
				Assembly callingAssembly = Assembly.GetCallingAssembly();
				assemblyName = callingAssembly.GetName();
				if (callingAssembly != Assembly.GetExecutingAssembly())
				{
					evidence = callingAssembly.Evidence;
				}
			}
			catch
			{
			}
			PermissionSet.RevertAssert();
			throw new SecurityException(Environment.GetResourceString("Security_PrincipalPermission"), assemblyName, null, null, null, SecurityAction.Demand, this, this, evidence);
		}

		// Token: 0x06002731 RID: 10033 RVA: 0x0008DF7C File Offset: 0x0008C17C
		[SecuritySafeCritical]
		public void Demand()
		{
			new SecurityPermission(SecurityPermissionFlag.ControlPrincipal).Assert();
			IPrincipal currentPrincipal = Thread.CurrentPrincipal;
			if (currentPrincipal == null)
			{
				this.ThrowSecurityException();
			}
			if (this.m_array == null)
			{
				return;
			}
			int num = this.m_array.Length;
			bool flag = false;
			for (int i = 0; i < num; i++)
			{
				if (!this.m_array[i].m_authenticated)
				{
					flag = true;
					break;
				}
				IIdentity identity = currentPrincipal.Identity;
				if (identity.IsAuthenticated && (this.m_array[i].m_id == null || string.Compare(identity.Name, this.m_array[i].m_id, StringComparison.OrdinalIgnoreCase) == 0))
				{
					if (this.m_array[i].m_role == null)
					{
						flag = true;
					}
					else
					{
						WindowsPrincipal windowsPrincipal = currentPrincipal as WindowsPrincipal;
						if (windowsPrincipal != null && this.m_array[i].Sid != null)
						{
							flag = windowsPrincipal.IsInRole(this.m_array[i].Sid);
						}
						else
						{
							flag = currentPrincipal.IsInRole(this.m_array[i].m_role);
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
			if (!flag)
			{
				this.ThrowSecurityException();
			}
		}

		// Token: 0x06002732 RID: 10034 RVA: 0x0008E094 File Offset: 0x0008C294
		public SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("IPermission");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), "System.Security.Permissions.PrincipalPermission");
			securityElement.AddAttribute("version", "1");
			int num = this.m_array.Length;
			for (int i = 0; i < num; i++)
			{
				securityElement.AddChild(this.m_array[i].ToXml());
			}
			return securityElement;
		}

		// Token: 0x06002733 RID: 10035 RVA: 0x0008E0F8 File Offset: 0x0008C2F8
		public void FromXml(SecurityElement elem)
		{
			CodeAccessPermission.ValidateElement(elem, this);
			if (elem.InternalChildren != null && elem.InternalChildren.Count != 0)
			{
				int count = elem.InternalChildren.Count;
				int num = 0;
				this.m_array = new IDRole[count];
				IEnumerator enumerator = elem.Children.GetEnumerator();
				while (enumerator.MoveNext())
				{
					IDRole idrole = new IDRole();
					idrole.FromXml((SecurityElement)enumerator.Current);
					this.m_array[num++] = idrole;
				}
				return;
			}
			this.m_array = new IDRole[0];
		}

		// Token: 0x06002734 RID: 10036 RVA: 0x0008E182 File Offset: 0x0008C382
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x06002735 RID: 10037 RVA: 0x0008E18F File Offset: 0x0008C38F
		int IBuiltInPermission.GetTokenIndex()
		{
			return PrincipalPermission.GetTokenIndex();
		}

		// Token: 0x06002736 RID: 10038 RVA: 0x0008E196 File Offset: 0x0008C396
		internal static int GetTokenIndex()
		{
			return 8;
		}

		// Token: 0x04000F23 RID: 3875
		private IDRole[] m_array;
	}
}
