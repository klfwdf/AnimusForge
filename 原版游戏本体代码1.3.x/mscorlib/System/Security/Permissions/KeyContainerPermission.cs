using System;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Permissions
{
	// Token: 0x02000318 RID: 792
	[ComVisible(true)]
	[Serializable]
	public sealed class KeyContainerPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
	{
		// Token: 0x060027E1 RID: 10209 RVA: 0x00090F18 File Offset: 0x0008F118
		public KeyContainerPermission(PermissionState state)
		{
			if (state == PermissionState.Unrestricted)
			{
				this.m_flags = KeyContainerPermissionFlags.AllFlags;
			}
			else
			{
				if (state != PermissionState.None)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
				}
				this.m_flags = KeyContainerPermissionFlags.NoFlags;
			}
			this.m_accessEntries = new KeyContainerPermissionAccessEntryCollection(this.m_flags);
		}

		// Token: 0x060027E2 RID: 10210 RVA: 0x00090F69 File Offset: 0x0008F169
		public KeyContainerPermission(KeyContainerPermissionFlags flags)
		{
			KeyContainerPermission.VerifyFlags(flags);
			this.m_flags = flags;
			this.m_accessEntries = new KeyContainerPermissionAccessEntryCollection(this.m_flags);
		}

		// Token: 0x060027E3 RID: 10211 RVA: 0x00090F90 File Offset: 0x0008F190
		public KeyContainerPermission(KeyContainerPermissionFlags flags, KeyContainerPermissionAccessEntry[] accessList)
		{
			if (accessList == null)
			{
				throw new ArgumentNullException("accessList");
			}
			KeyContainerPermission.VerifyFlags(flags);
			this.m_flags = flags;
			this.m_accessEntries = new KeyContainerPermissionAccessEntryCollection(this.m_flags);
			for (int i = 0; i < accessList.Length; i++)
			{
				this.m_accessEntries.Add(accessList[i]);
			}
		}

		// Token: 0x1700051C RID: 1308
		// (get) Token: 0x060027E4 RID: 10212 RVA: 0x00090FEC File Offset: 0x0008F1EC
		public KeyContainerPermissionFlags Flags
		{
			get
			{
				return this.m_flags;
			}
		}

		// Token: 0x1700051D RID: 1309
		// (get) Token: 0x060027E5 RID: 10213 RVA: 0x00090FF4 File Offset: 0x0008F1F4
		public KeyContainerPermissionAccessEntryCollection AccessEntries
		{
			get
			{
				return this.m_accessEntries;
			}
		}

		// Token: 0x060027E6 RID: 10214 RVA: 0x00090FFC File Offset: 0x0008F1FC
		public bool IsUnrestricted()
		{
			if (this.m_flags != KeyContainerPermissionFlags.AllFlags)
			{
				return false;
			}
			foreach (KeyContainerPermissionAccessEntry keyContainerPermissionAccessEntry in this.AccessEntries)
			{
				if ((keyContainerPermissionAccessEntry.Flags & KeyContainerPermissionFlags.AllFlags) != KeyContainerPermissionFlags.AllFlags)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060027E7 RID: 10215 RVA: 0x0009104C File Offset: 0x0008F24C
		private bool IsEmpty()
		{
			if (this.Flags == KeyContainerPermissionFlags.NoFlags)
			{
				foreach (KeyContainerPermissionAccessEntry keyContainerPermissionAccessEntry in this.AccessEntries)
				{
					if (keyContainerPermissionAccessEntry.Flags != KeyContainerPermissionFlags.NoFlags)
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		// Token: 0x060027E8 RID: 10216 RVA: 0x0009108C File Offset: 0x0008F28C
		public override bool IsSubsetOf(IPermission target)
		{
			if (target == null)
			{
				return this.IsEmpty();
			}
			if (!base.VerifyType(target))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			KeyContainerPermission keyContainerPermission = (KeyContainerPermission)target;
			if ((this.m_flags & keyContainerPermission.m_flags) != this.m_flags)
			{
				return false;
			}
			foreach (KeyContainerPermissionAccessEntry keyContainerPermissionAccessEntry in this.AccessEntries)
			{
				KeyContainerPermissionFlags applicableFlags = KeyContainerPermission.GetApplicableFlags(keyContainerPermissionAccessEntry, keyContainerPermission);
				if ((keyContainerPermissionAccessEntry.Flags & applicableFlags) != keyContainerPermissionAccessEntry.Flags)
				{
					return false;
				}
			}
			foreach (KeyContainerPermissionAccessEntry keyContainerPermissionAccessEntry2 in keyContainerPermission.AccessEntries)
			{
				KeyContainerPermissionFlags applicableFlags2 = KeyContainerPermission.GetApplicableFlags(keyContainerPermissionAccessEntry2, this);
				if ((applicableFlags2 & keyContainerPermissionAccessEntry2.Flags) != applicableFlags2)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060027E9 RID: 10217 RVA: 0x00091164 File Offset: 0x0008F364
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
			KeyContainerPermission keyContainerPermission = (KeyContainerPermission)target;
			if (this.IsEmpty() || keyContainerPermission.IsEmpty())
			{
				return null;
			}
			KeyContainerPermissionFlags flags = keyContainerPermission.m_flags & this.m_flags;
			KeyContainerPermission keyContainerPermission2 = new KeyContainerPermission(flags);
			foreach (KeyContainerPermissionAccessEntry accessEntry in this.AccessEntries)
			{
				keyContainerPermission2.AddAccessEntryAndIntersect(accessEntry, keyContainerPermission);
			}
			foreach (KeyContainerPermissionAccessEntry accessEntry2 in keyContainerPermission.AccessEntries)
			{
				keyContainerPermission2.AddAccessEntryAndIntersect(accessEntry2, this);
			}
			if (!keyContainerPermission2.IsEmpty())
			{
				return keyContainerPermission2;
			}
			return null;
		}

		// Token: 0x060027EA RID: 10218 RVA: 0x00091230 File Offset: 0x0008F430
		public override IPermission Union(IPermission target)
		{
			if (target == null)
			{
				return this.Copy();
			}
			if (!base.VerifyType(target))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			KeyContainerPermission keyContainerPermission = (KeyContainerPermission)target;
			if (this.IsUnrestricted() || keyContainerPermission.IsUnrestricted())
			{
				return new KeyContainerPermission(PermissionState.Unrestricted);
			}
			KeyContainerPermissionFlags flags = this.m_flags | keyContainerPermission.m_flags;
			KeyContainerPermission keyContainerPermission2 = new KeyContainerPermission(flags);
			foreach (KeyContainerPermissionAccessEntry accessEntry in this.AccessEntries)
			{
				keyContainerPermission2.AddAccessEntryAndUnion(accessEntry, keyContainerPermission);
			}
			foreach (KeyContainerPermissionAccessEntry accessEntry2 in keyContainerPermission.AccessEntries)
			{
				keyContainerPermission2.AddAccessEntryAndUnion(accessEntry2, this);
			}
			if (!keyContainerPermission2.IsEmpty())
			{
				return keyContainerPermission2;
			}
			return null;
		}

		// Token: 0x060027EB RID: 10219 RVA: 0x00091304 File Offset: 0x0008F504
		public override IPermission Copy()
		{
			if (this.IsEmpty())
			{
				return null;
			}
			KeyContainerPermission keyContainerPermission = new KeyContainerPermission(this.m_flags);
			foreach (KeyContainerPermissionAccessEntry accessEntry in this.AccessEntries)
			{
				keyContainerPermission.AccessEntries.Add(accessEntry);
			}
			return keyContainerPermission;
		}

		// Token: 0x060027EC RID: 10220 RVA: 0x00091354 File Offset: 0x0008F554
		public override SecurityElement ToXml()
		{
			SecurityElement securityElement = CodeAccessPermission.CreatePermissionElement(this, "System.Security.Permissions.KeyContainerPermission");
			if (!this.IsUnrestricted())
			{
				securityElement.AddAttribute("Flags", this.m_flags.ToString());
				if (this.AccessEntries.Count > 0)
				{
					SecurityElement securityElement2 = new SecurityElement("AccessList");
					foreach (KeyContainerPermissionAccessEntry keyContainerPermissionAccessEntry in this.AccessEntries)
					{
						SecurityElement securityElement3 = new SecurityElement("AccessEntry");
						securityElement3.AddAttribute("KeyStore", keyContainerPermissionAccessEntry.KeyStore);
						securityElement3.AddAttribute("ProviderName", keyContainerPermissionAccessEntry.ProviderName);
						securityElement3.AddAttribute("ProviderType", keyContainerPermissionAccessEntry.ProviderType.ToString(null, null));
						securityElement3.AddAttribute("KeyContainerName", keyContainerPermissionAccessEntry.KeyContainerName);
						securityElement3.AddAttribute("KeySpec", keyContainerPermissionAccessEntry.KeySpec.ToString(null, null));
						securityElement3.AddAttribute("Flags", keyContainerPermissionAccessEntry.Flags.ToString());
						securityElement2.AddChild(securityElement3);
					}
					securityElement.AddChild(securityElement2);
				}
			}
			else
			{
				securityElement.AddAttribute("Unrestricted", "true");
			}
			return securityElement;
		}

		// Token: 0x060027ED RID: 10221 RVA: 0x00091494 File Offset: 0x0008F694
		public override void FromXml(SecurityElement securityElement)
		{
			CodeAccessPermission.ValidateElement(securityElement, this);
			if (XMLUtil.IsUnrestricted(securityElement))
			{
				this.m_flags = KeyContainerPermissionFlags.AllFlags;
				this.m_accessEntries = new KeyContainerPermissionAccessEntryCollection(this.m_flags);
				return;
			}
			this.m_flags = KeyContainerPermissionFlags.NoFlags;
			string text = securityElement.Attribute("Flags");
			if (text != null)
			{
				KeyContainerPermissionFlags flags = (KeyContainerPermissionFlags)Enum.Parse(typeof(KeyContainerPermissionFlags), text);
				KeyContainerPermission.VerifyFlags(flags);
				this.m_flags = flags;
			}
			this.m_accessEntries = new KeyContainerPermissionAccessEntryCollection(this.m_flags);
			if (securityElement.InternalChildren != null && securityElement.InternalChildren.Count != 0)
			{
				foreach (object obj in securityElement.Children)
				{
					SecurityElement securityElement2 = (SecurityElement)obj;
					if (securityElement2 != null && string.Equals(securityElement2.Tag, "AccessList"))
					{
						this.AddAccessEntries(securityElement2);
					}
				}
			}
		}

		// Token: 0x060027EE RID: 10222 RVA: 0x0009156A File Offset: 0x0008F76A
		int IBuiltInPermission.GetTokenIndex()
		{
			return KeyContainerPermission.GetTokenIndex();
		}

		// Token: 0x060027EF RID: 10223 RVA: 0x00091574 File Offset: 0x0008F774
		private void AddAccessEntries(SecurityElement securityElement)
		{
			if (securityElement.InternalChildren != null && securityElement.InternalChildren.Count != 0)
			{
				foreach (object obj in securityElement.Children)
				{
					SecurityElement securityElement2 = (SecurityElement)obj;
					if (securityElement2 != null && string.Equals(securityElement2.Tag, "AccessEntry"))
					{
						int count = securityElement2.m_lAttributes.Count;
						string keyStore = null;
						string providerName = null;
						int providerType = -1;
						string keyContainerName = null;
						int keySpec = -1;
						KeyContainerPermissionFlags flags = KeyContainerPermissionFlags.NoFlags;
						for (int i = 0; i < count; i += 2)
						{
							string a = (string)securityElement2.m_lAttributes[i];
							string text = (string)securityElement2.m_lAttributes[i + 1];
							if (string.Equals(a, "KeyStore"))
							{
								keyStore = text;
							}
							if (string.Equals(a, "ProviderName"))
							{
								providerName = text;
							}
							else if (string.Equals(a, "ProviderType"))
							{
								providerType = Convert.ToInt32(text, null);
							}
							else if (string.Equals(a, "KeyContainerName"))
							{
								keyContainerName = text;
							}
							else if (string.Equals(a, "KeySpec"))
							{
								keySpec = Convert.ToInt32(text, null);
							}
							else if (string.Equals(a, "Flags"))
							{
								flags = (KeyContainerPermissionFlags)Enum.Parse(typeof(KeyContainerPermissionFlags), text);
							}
						}
						KeyContainerPermissionAccessEntry accessEntry = new KeyContainerPermissionAccessEntry(keyStore, providerName, providerType, keyContainerName, keySpec, flags);
						this.AccessEntries.Add(accessEntry);
					}
				}
			}
		}

		// Token: 0x060027F0 RID: 10224 RVA: 0x000916F0 File Offset: 0x0008F8F0
		private void AddAccessEntryAndUnion(KeyContainerPermissionAccessEntry accessEntry, KeyContainerPermission target)
		{
			KeyContainerPermissionAccessEntry keyContainerPermissionAccessEntry = new KeyContainerPermissionAccessEntry(accessEntry);
			keyContainerPermissionAccessEntry.Flags |= KeyContainerPermission.GetApplicableFlags(accessEntry, target);
			this.AccessEntries.Add(keyContainerPermissionAccessEntry);
		}

		// Token: 0x060027F1 RID: 10225 RVA: 0x00091728 File Offset: 0x0008F928
		private void AddAccessEntryAndIntersect(KeyContainerPermissionAccessEntry accessEntry, KeyContainerPermission target)
		{
			KeyContainerPermissionAccessEntry keyContainerPermissionAccessEntry = new KeyContainerPermissionAccessEntry(accessEntry);
			keyContainerPermissionAccessEntry.Flags &= KeyContainerPermission.GetApplicableFlags(accessEntry, target);
			this.AccessEntries.Add(keyContainerPermissionAccessEntry);
		}

		// Token: 0x060027F2 RID: 10226 RVA: 0x0009175D File Offset: 0x0008F95D
		internal static void VerifyFlags(KeyContainerPermissionFlags flags)
		{
			if ((flags & ~(KeyContainerPermissionFlags.Create | KeyContainerPermissionFlags.Open | KeyContainerPermissionFlags.Delete | KeyContainerPermissionFlags.Import | KeyContainerPermissionFlags.Export | KeyContainerPermissionFlags.Sign | KeyContainerPermissionFlags.Decrypt | KeyContainerPermissionFlags.ViewAcl | KeyContainerPermissionFlags.ChangeAcl)) != KeyContainerPermissionFlags.NoFlags)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)flags }));
			}
		}

		// Token: 0x060027F3 RID: 10227 RVA: 0x00091788 File Offset: 0x0008F988
		private static KeyContainerPermissionFlags GetApplicableFlags(KeyContainerPermissionAccessEntry accessEntry, KeyContainerPermission target)
		{
			KeyContainerPermissionFlags keyContainerPermissionFlags = KeyContainerPermissionFlags.NoFlags;
			bool flag = true;
			int num = target.AccessEntries.IndexOf(accessEntry);
			if (num != -1)
			{
				return target.AccessEntries[num].Flags;
			}
			foreach (KeyContainerPermissionAccessEntry keyContainerPermissionAccessEntry in target.AccessEntries)
			{
				if (accessEntry.IsSubsetOf(keyContainerPermissionAccessEntry))
				{
					if (!flag)
					{
						keyContainerPermissionFlags &= keyContainerPermissionAccessEntry.Flags;
					}
					else
					{
						keyContainerPermissionFlags = keyContainerPermissionAccessEntry.Flags;
						flag = false;
					}
				}
			}
			if (flag)
			{
				keyContainerPermissionFlags = target.Flags;
			}
			return keyContainerPermissionFlags;
		}

		// Token: 0x060027F4 RID: 10228 RVA: 0x0009180A File Offset: 0x0008FA0A
		private static int GetTokenIndex()
		{
			return 16;
		}

		// Token: 0x04000F74 RID: 3956
		private KeyContainerPermissionFlags m_flags;

		// Token: 0x04000F75 RID: 3957
		private KeyContainerPermissionAccessEntryCollection m_accessEntries;
	}
}
