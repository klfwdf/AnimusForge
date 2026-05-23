using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002F4 RID: 756
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class KeyContainerPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x06002684 RID: 9860 RVA: 0x0008C758 File Offset: 0x0008A958
		public KeyContainerPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004CF RID: 1231
		// (get) Token: 0x06002685 RID: 9861 RVA: 0x0008C76F File Offset: 0x0008A96F
		// (set) Token: 0x06002686 RID: 9862 RVA: 0x0008C777 File Offset: 0x0008A977
		public string KeyStore
		{
			get
			{
				return this.m_keyStore;
			}
			set
			{
				this.m_keyStore = value;
			}
		}

		// Token: 0x170004D0 RID: 1232
		// (get) Token: 0x06002687 RID: 9863 RVA: 0x0008C780 File Offset: 0x0008A980
		// (set) Token: 0x06002688 RID: 9864 RVA: 0x0008C788 File Offset: 0x0008A988
		public string ProviderName
		{
			get
			{
				return this.m_providerName;
			}
			set
			{
				this.m_providerName = value;
			}
		}

		// Token: 0x170004D1 RID: 1233
		// (get) Token: 0x06002689 RID: 9865 RVA: 0x0008C791 File Offset: 0x0008A991
		// (set) Token: 0x0600268A RID: 9866 RVA: 0x0008C799 File Offset: 0x0008A999
		public int ProviderType
		{
			get
			{
				return this.m_providerType;
			}
			set
			{
				this.m_providerType = value;
			}
		}

		// Token: 0x170004D2 RID: 1234
		// (get) Token: 0x0600268B RID: 9867 RVA: 0x0008C7A2 File Offset: 0x0008A9A2
		// (set) Token: 0x0600268C RID: 9868 RVA: 0x0008C7AA File Offset: 0x0008A9AA
		public string KeyContainerName
		{
			get
			{
				return this.m_keyContainerName;
			}
			set
			{
				this.m_keyContainerName = value;
			}
		}

		// Token: 0x170004D3 RID: 1235
		// (get) Token: 0x0600268D RID: 9869 RVA: 0x0008C7B3 File Offset: 0x0008A9B3
		// (set) Token: 0x0600268E RID: 9870 RVA: 0x0008C7BB File Offset: 0x0008A9BB
		public int KeySpec
		{
			get
			{
				return this.m_keySpec;
			}
			set
			{
				this.m_keySpec = value;
			}
		}

		// Token: 0x170004D4 RID: 1236
		// (get) Token: 0x0600268F RID: 9871 RVA: 0x0008C7C4 File Offset: 0x0008A9C4
		// (set) Token: 0x06002690 RID: 9872 RVA: 0x0008C7CC File Offset: 0x0008A9CC
		public KeyContainerPermissionFlags Flags
		{
			get
			{
				return this.m_flags;
			}
			set
			{
				this.m_flags = value;
			}
		}

		// Token: 0x06002691 RID: 9873 RVA: 0x0008C7D8 File Offset: 0x0008A9D8
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new KeyContainerPermission(PermissionState.Unrestricted);
			}
			if (KeyContainerPermissionAccessEntry.IsUnrestrictedEntry(this.m_keyStore, this.m_providerName, this.m_providerType, this.m_keyContainerName, this.m_keySpec))
			{
				return new KeyContainerPermission(this.m_flags);
			}
			KeyContainerPermission keyContainerPermission = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
			KeyContainerPermissionAccessEntry accessEntry = new KeyContainerPermissionAccessEntry(this.m_keyStore, this.m_providerName, this.m_providerType, this.m_keyContainerName, this.m_keySpec, this.m_flags);
			keyContainerPermission.AccessEntries.Add(accessEntry);
			return keyContainerPermission;
		}

		// Token: 0x04000EF4 RID: 3828
		private KeyContainerPermissionFlags m_flags;

		// Token: 0x04000EF5 RID: 3829
		private string m_keyStore;

		// Token: 0x04000EF6 RID: 3830
		private string m_providerName;

		// Token: 0x04000EF7 RID: 3831
		private int m_providerType = -1;

		// Token: 0x04000EF8 RID: 3832
		private string m_keyContainerName;

		// Token: 0x04000EF9 RID: 3833
		private int m_keySpec = -1;
	}
}
