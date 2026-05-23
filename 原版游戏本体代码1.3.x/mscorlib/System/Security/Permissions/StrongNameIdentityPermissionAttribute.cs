using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002FB RID: 763
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class StrongNameIdentityPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026E0 RID: 9952 RVA: 0x0008CE5A File Offset: 0x0008B05A
		public StrongNameIdentityPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004F6 RID: 1270
		// (get) Token: 0x060026E1 RID: 9953 RVA: 0x0008CE63 File Offset: 0x0008B063
		// (set) Token: 0x060026E2 RID: 9954 RVA: 0x0008CE6B File Offset: 0x0008B06B
		public string Name
		{
			get
			{
				return this.m_name;
			}
			set
			{
				this.m_name = value;
			}
		}

		// Token: 0x170004F7 RID: 1271
		// (get) Token: 0x060026E3 RID: 9955 RVA: 0x0008CE74 File Offset: 0x0008B074
		// (set) Token: 0x060026E4 RID: 9956 RVA: 0x0008CE7C File Offset: 0x0008B07C
		public string Version
		{
			get
			{
				return this.m_version;
			}
			set
			{
				this.m_version = value;
			}
		}

		// Token: 0x170004F8 RID: 1272
		// (get) Token: 0x060026E5 RID: 9957 RVA: 0x0008CE85 File Offset: 0x0008B085
		// (set) Token: 0x060026E6 RID: 9958 RVA: 0x0008CE8D File Offset: 0x0008B08D
		public string PublicKey
		{
			get
			{
				return this.m_blob;
			}
			set
			{
				this.m_blob = value;
			}
		}

		// Token: 0x060026E7 RID: 9959 RVA: 0x0008CE98 File Offset: 0x0008B098
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new StrongNameIdentityPermission(PermissionState.Unrestricted);
			}
			if (this.m_blob == null && this.m_name == null && this.m_version == null)
			{
				return new StrongNameIdentityPermission(PermissionState.None);
			}
			if (this.m_blob == null)
			{
				throw new ArgumentException(Environment.GetResourceString("ArgumentNull_Key"));
			}
			StrongNamePublicKeyBlob blob = new StrongNamePublicKeyBlob(this.m_blob);
			if (this.m_version == null || this.m_version.Equals(string.Empty))
			{
				return new StrongNameIdentityPermission(blob, this.m_name, null);
			}
			return new StrongNameIdentityPermission(blob, this.m_name, new Version(this.m_version));
		}

		// Token: 0x04000F07 RID: 3847
		private string m_name;

		// Token: 0x04000F08 RID: 3848
		private string m_version;

		// Token: 0x04000F09 RID: 3849
		private string m_blob;
	}
}
