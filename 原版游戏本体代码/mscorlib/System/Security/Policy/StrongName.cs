using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x0200036C RID: 876
	[ComVisible(true)]
	[Serializable]
	public sealed class StrongName : EvidenceBase, IIdentityPermissionFactory, IDelayEvaluatedEvidence
	{
		// Token: 0x06002B51 RID: 11089 RVA: 0x000A1850 File Offset: 0x0009FA50
		internal StrongName()
		{
		}

		// Token: 0x06002B52 RID: 11090 RVA: 0x000A1858 File Offset: 0x0009FA58
		public StrongName(StrongNamePublicKeyBlob blob, string name, Version version)
			: this(blob, name, version, null)
		{
		}

		// Token: 0x06002B53 RID: 11091 RVA: 0x000A1864 File Offset: 0x0009FA64
		internal StrongName(StrongNamePublicKeyBlob blob, string name, Version version, Assembly assembly)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_EmptyStrongName"));
			}
			if (blob == null)
			{
				throw new ArgumentNullException("blob");
			}
			if (version == null)
			{
				throw new ArgumentNullException("version");
			}
			RuntimeAssembly runtimeAssembly = assembly as RuntimeAssembly;
			if (assembly != null && runtimeAssembly == null)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "assembly");
			}
			this.m_publicKeyBlob = blob;
			this.m_name = name;
			this.m_version = version;
			this.m_assembly = runtimeAssembly;
		}

		// Token: 0x170005C9 RID: 1481
		// (get) Token: 0x06002B54 RID: 11092 RVA: 0x000A190B File Offset: 0x0009FB0B
		public StrongNamePublicKeyBlob PublicKey
		{
			get
			{
				return this.m_publicKeyBlob;
			}
		}

		// Token: 0x170005CA RID: 1482
		// (get) Token: 0x06002B55 RID: 11093 RVA: 0x000A1913 File Offset: 0x0009FB13
		public string Name
		{
			get
			{
				return this.m_name;
			}
		}

		// Token: 0x170005CB RID: 1483
		// (get) Token: 0x06002B56 RID: 11094 RVA: 0x000A191B File Offset: 0x0009FB1B
		public Version Version
		{
			get
			{
				return this.m_version;
			}
		}

		// Token: 0x170005CC RID: 1484
		// (get) Token: 0x06002B57 RID: 11095 RVA: 0x000A1923 File Offset: 0x0009FB23
		bool IDelayEvaluatedEvidence.IsVerified
		{
			[SecurityCritical]
			get
			{
				return !(this.m_assembly != null) || this.m_assembly.IsStrongNameVerified;
			}
		}

		// Token: 0x170005CD RID: 1485
		// (get) Token: 0x06002B58 RID: 11096 RVA: 0x000A1940 File Offset: 0x0009FB40
		bool IDelayEvaluatedEvidence.WasUsed
		{
			get
			{
				return this.m_wasUsed;
			}
		}

		// Token: 0x06002B59 RID: 11097 RVA: 0x000A1948 File Offset: 0x0009FB48
		void IDelayEvaluatedEvidence.MarkUsed()
		{
			this.m_wasUsed = true;
		}

		// Token: 0x06002B5A RID: 11098 RVA: 0x000A1954 File Offset: 0x0009FB54
		internal static bool CompareNames(string asmName, string mcName)
		{
			if (mcName.Length > 0 && mcName[mcName.Length - 1] == '*' && mcName.Length - 1 <= asmName.Length)
			{
				return string.Compare(mcName, 0, asmName, 0, mcName.Length - 1, StringComparison.OrdinalIgnoreCase) == 0;
			}
			return string.Compare(mcName, asmName, StringComparison.OrdinalIgnoreCase) == 0;
		}

		// Token: 0x06002B5B RID: 11099 RVA: 0x000A19AD File Offset: 0x0009FBAD
		public IPermission CreateIdentityPermission(Evidence evidence)
		{
			return new StrongNameIdentityPermission(this.m_publicKeyBlob, this.m_name, this.m_version);
		}

		// Token: 0x06002B5C RID: 11100 RVA: 0x000A19C6 File Offset: 0x0009FBC6
		public override EvidenceBase Clone()
		{
			return new StrongName(this.m_publicKeyBlob, this.m_name, this.m_version);
		}

		// Token: 0x06002B5D RID: 11101 RVA: 0x000A19DF File Offset: 0x0009FBDF
		public object Copy()
		{
			return this.Clone();
		}

		// Token: 0x06002B5E RID: 11102 RVA: 0x000A19E8 File Offset: 0x0009FBE8
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("StrongName");
			securityElement.AddAttribute("version", "1");
			if (this.m_publicKeyBlob != null)
			{
				securityElement.AddAttribute("Key", Hex.EncodeHexString(this.m_publicKeyBlob.PublicKey));
			}
			if (this.m_name != null)
			{
				securityElement.AddAttribute("Name", this.m_name);
			}
			if (this.m_version != null)
			{
				securityElement.AddAttribute("Version", this.m_version.ToString());
			}
			return securityElement;
		}

		// Token: 0x06002B5F RID: 11103 RVA: 0x000A1A74 File Offset: 0x0009FC74
		internal void FromXml(SecurityElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException("element");
			}
			if (string.Compare(element.Tag, "StrongName", StringComparison.Ordinal) != 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidXML"));
			}
			this.m_publicKeyBlob = null;
			this.m_version = null;
			string text = element.Attribute("Key");
			if (text != null)
			{
				this.m_publicKeyBlob = new StrongNamePublicKeyBlob(Hex.DecodeHexString(text));
			}
			this.m_name = element.Attribute("Name");
			string text2 = element.Attribute("Version");
			if (text2 != null)
			{
				this.m_version = new Version(text2);
			}
		}

		// Token: 0x06002B60 RID: 11104 RVA: 0x000A1B0C File Offset: 0x0009FD0C
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x06002B61 RID: 11105 RVA: 0x000A1B1C File Offset: 0x0009FD1C
		public override bool Equals(object o)
		{
			StrongName strongName = o as StrongName;
			return strongName != null && object.Equals(this.m_publicKeyBlob, strongName.m_publicKeyBlob) && object.Equals(this.m_name, strongName.m_name) && object.Equals(this.m_version, strongName.m_version);
		}

		// Token: 0x06002B62 RID: 11106 RVA: 0x000A1B6C File Offset: 0x0009FD6C
		public override int GetHashCode()
		{
			if (this.m_publicKeyBlob != null)
			{
				return this.m_publicKeyBlob.GetHashCode();
			}
			if (this.m_name != null || this.m_version != null)
			{
				return ((this.m_name == null) ? 0 : this.m_name.GetHashCode()) + ((this.m_version == null) ? 0 : this.m_version.GetHashCode());
			}
			return typeof(StrongName).GetHashCode();
		}

		// Token: 0x06002B63 RID: 11107 RVA: 0x000A1BE8 File Offset: 0x0009FDE8
		internal object Normalize()
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write(this.m_publicKeyBlob.PublicKey);
			binaryWriter.Write(this.m_version.Major);
			binaryWriter.Write(this.m_name);
			memoryStream.Position = 0L;
			return memoryStream;
		}

		// Token: 0x0400119C RID: 4508
		private StrongNamePublicKeyBlob m_publicKeyBlob;

		// Token: 0x0400119D RID: 4509
		private string m_name;

		// Token: 0x0400119E RID: 4510
		private Version m_version;

		// Token: 0x0400119F RID: 4511
		[NonSerialized]
		private RuntimeAssembly m_assembly;

		// Token: 0x040011A0 RID: 4512
		[NonSerialized]
		private bool m_wasUsed;
	}
}
