using System;
using System.Collections;
using System.Threading;

namespace System.Security
{
	// Token: 0x020001E2 RID: 482
	internal class PermissionTokenFactory
	{
		// Token: 0x06001D34 RID: 7476 RVA: 0x0006548C File Offset: 0x0006368C
		internal PermissionTokenFactory(int size)
		{
			this.m_builtIn = new PermissionToken[17];
			this.m_size = size;
			this.m_index = 17;
			this.m_tokenTable = null;
			this.m_handleTable = new Hashtable(size);
			this.m_indexTable = new Hashtable(size);
		}

		// Token: 0x06001D35 RID: 7477 RVA: 0x000654E8 File Offset: 0x000636E8
		[SecuritySafeCritical]
		internal PermissionToken FindToken(Type cls)
		{
			IntPtr value = cls.TypeHandle.Value;
			PermissionToken permissionToken = (PermissionToken)this.m_handleTable[value];
			if (permissionToken != null)
			{
				return permissionToken;
			}
			if (this.m_tokenTable == null)
			{
				return null;
			}
			permissionToken = (PermissionToken)this.m_tokenTable[cls.AssemblyQualifiedName];
			if (permissionToken != null)
			{
				lock (this)
				{
					this.m_handleTable.Add(value, permissionToken);
				}
			}
			return permissionToken;
		}

		// Token: 0x06001D36 RID: 7478 RVA: 0x0006558C File Offset: 0x0006378C
		internal PermissionToken FindTokenByIndex(int i)
		{
			PermissionToken result;
			if (i < 17)
			{
				result = this.BuiltInGetToken(i, null, null);
			}
			else
			{
				result = (PermissionToken)this.m_indexTable[i];
			}
			return result;
		}

		// Token: 0x06001D37 RID: 7479 RVA: 0x000655C4 File Offset: 0x000637C4
		[SecuritySafeCritical]
		internal PermissionToken GetToken(Type cls, IPermission perm)
		{
			IntPtr value = cls.TypeHandle.Value;
			object obj = this.m_handleTable[value];
			if (obj == null)
			{
				string assemblyQualifiedName = cls.AssemblyQualifiedName;
				obj = ((this.m_tokenTable != null) ? this.m_tokenTable[assemblyQualifiedName] : null);
				if (obj == null)
				{
					lock (this)
					{
						if (this.m_tokenTable != null)
						{
							obj = this.m_tokenTable[assemblyQualifiedName];
						}
						else
						{
							this.m_tokenTable = new Hashtable(this.m_size, 1f, new PermissionTokenKeyComparer());
						}
						if (obj == null)
						{
							if (perm != null)
							{
								int index = this.m_index;
								this.m_index = index + 1;
								obj = new PermissionToken(index, PermissionTokenType.IUnrestricted, assemblyQualifiedName);
							}
							else if (cls.GetInterface("System.Security.Permissions.IUnrestrictedPermission") != null)
							{
								int index = this.m_index;
								this.m_index = index + 1;
								obj = new PermissionToken(index, PermissionTokenType.IUnrestricted, assemblyQualifiedName);
							}
							else
							{
								int index = this.m_index;
								this.m_index = index + 1;
								obj = new PermissionToken(index, PermissionTokenType.Normal, assemblyQualifiedName);
							}
							this.m_tokenTable.Add(assemblyQualifiedName, obj);
							this.m_indexTable.Add(this.m_index - 1, obj);
							PermissionToken.s_tokenSet.SetItem(((PermissionToken)obj).m_index, obj);
						}
						if (!this.m_handleTable.Contains(value))
						{
							this.m_handleTable.Add(value, obj);
						}
						goto IL_1DA;
					}
				}
				lock (this)
				{
					if (!this.m_handleTable.Contains(value))
					{
						this.m_handleTable.Add(value, obj);
					}
				}
			}
			IL_1DA:
			if ((((PermissionToken)obj).m_type & PermissionTokenType.DontKnow) != (PermissionTokenType)0)
			{
				if (perm != null)
				{
					((PermissionToken)obj).m_type = PermissionTokenType.IUnrestricted;
					((PermissionToken)obj).m_strTypeName = perm.GetType().AssemblyQualifiedName;
				}
				else
				{
					if (cls.GetInterface("System.Security.Permissions.IUnrestrictedPermission") != null)
					{
						((PermissionToken)obj).m_type = PermissionTokenType.IUnrestricted;
					}
					else
					{
						((PermissionToken)obj).m_type = PermissionTokenType.Normal;
					}
					((PermissionToken)obj).m_strTypeName = cls.AssemblyQualifiedName;
				}
			}
			return (PermissionToken)obj;
		}

		// Token: 0x06001D38 RID: 7480 RVA: 0x00065864 File Offset: 0x00063A64
		internal PermissionToken GetToken(string typeStr)
		{
			object obj = null;
			obj = ((this.m_tokenTable != null) ? this.m_tokenTable[typeStr] : null);
			if (obj == null)
			{
				lock (this)
				{
					if (this.m_tokenTable != null)
					{
						obj = this.m_tokenTable[typeStr];
					}
					else
					{
						this.m_tokenTable = new Hashtable(this.m_size, 1f, new PermissionTokenKeyComparer());
					}
					if (obj == null)
					{
						int index = this.m_index;
						this.m_index = index + 1;
						obj = new PermissionToken(index, PermissionTokenType.DontKnow, typeStr);
						this.m_tokenTable.Add(typeStr, obj);
						this.m_indexTable.Add(this.m_index - 1, obj);
						PermissionToken.s_tokenSet.SetItem(((PermissionToken)obj).m_index, obj);
					}
				}
			}
			return (PermissionToken)obj;
		}

		// Token: 0x06001D39 RID: 7481 RVA: 0x00065960 File Offset: 0x00063B60
		internal PermissionToken BuiltInGetToken(int index, IPermission perm, Type cls)
		{
			PermissionToken permissionToken = Volatile.Read<PermissionToken>(ref this.m_builtIn[index]);
			if (permissionToken == null)
			{
				lock (this)
				{
					permissionToken = this.m_builtIn[index];
					if (permissionToken == null)
					{
						PermissionTokenType permissionTokenType = PermissionTokenType.DontKnow;
						if (perm != null)
						{
							permissionTokenType = PermissionTokenType.IUnrestricted;
						}
						else if (cls != null)
						{
							permissionTokenType = PermissionTokenType.IUnrestricted;
						}
						permissionToken = new PermissionToken(index, permissionTokenType | PermissionTokenType.BuiltIn, null);
						Volatile.Write<PermissionToken>(ref this.m_builtIn[index], permissionToken);
						PermissionToken.s_tokenSet.SetItem(permissionToken.m_index, permissionToken);
					}
				}
			}
			if ((permissionToken.m_type & PermissionTokenType.DontKnow) != (PermissionTokenType)0)
			{
				permissionToken.m_type = PermissionTokenType.BuiltIn;
				if (perm != null)
				{
					permissionToken.m_type |= PermissionTokenType.IUnrestricted;
				}
				else if (cls != null)
				{
					permissionToken.m_type |= PermissionTokenType.IUnrestricted;
				}
				else
				{
					permissionToken.m_type |= PermissionTokenType.DontKnow;
				}
			}
			return permissionToken;
		}

		// Token: 0x04000A35 RID: 2613
		private volatile int m_size;

		// Token: 0x04000A36 RID: 2614
		private volatile int m_index;

		// Token: 0x04000A37 RID: 2615
		private volatile Hashtable m_tokenTable;

		// Token: 0x04000A38 RID: 2616
		private volatile Hashtable m_handleTable;

		// Token: 0x04000A39 RID: 2617
		private volatile Hashtable m_indexTable;

		// Token: 0x04000A3A RID: 2618
		private volatile PermissionToken[] m_builtIn;

		// Token: 0x04000A3B RID: 2619
		private const string s_unrestrictedPermissionInferfaceName = "System.Security.Permissions.IUnrestrictedPermission";
	}
}
