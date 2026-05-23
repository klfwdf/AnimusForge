using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

namespace System.Security.Policy
{
	// Token: 0x02000377 RID: 887
	[ComVisible(true)]
	[Serializable]
	public sealed class Publisher : EvidenceBase, IIdentityPermissionFactory
	{
		// Token: 0x06002C01 RID: 11265 RVA: 0x000A3F84 File Offset: 0x000A2184
		public Publisher(X509Certificate cert)
		{
			if (cert == null)
			{
				throw new ArgumentNullException("cert");
			}
			this.m_cert = cert;
		}

		// Token: 0x06002C02 RID: 11266 RVA: 0x000A3FA1 File Offset: 0x000A21A1
		public IPermission CreateIdentityPermission(Evidence evidence)
		{
			return new PublisherIdentityPermission(this.m_cert);
		}

		// Token: 0x06002C03 RID: 11267 RVA: 0x000A3FB0 File Offset: 0x000A21B0
		public override bool Equals(object o)
		{
			Publisher publisher = o as Publisher;
			return publisher != null && Publisher.PublicKeyEquals(this.m_cert, publisher.m_cert);
		}

		// Token: 0x06002C04 RID: 11268 RVA: 0x000A3FDC File Offset: 0x000A21DC
		internal static bool PublicKeyEquals(X509Certificate cert1, X509Certificate cert2)
		{
			if (cert1 == null)
			{
				return cert2 == null;
			}
			if (cert2 == null)
			{
				return false;
			}
			byte[] publicKey = cert1.GetPublicKey();
			string keyAlgorithm = cert1.GetKeyAlgorithm();
			byte[] keyAlgorithmParameters = cert1.GetKeyAlgorithmParameters();
			byte[] publicKey2 = cert2.GetPublicKey();
			string keyAlgorithm2 = cert2.GetKeyAlgorithm();
			byte[] keyAlgorithmParameters2 = cert2.GetKeyAlgorithmParameters();
			int num = publicKey.Length;
			if (num != publicKey2.Length)
			{
				return false;
			}
			for (int i = 0; i < num; i++)
			{
				if (publicKey[i] != publicKey2[i])
				{
					return false;
				}
			}
			if (!keyAlgorithm.Equals(keyAlgorithm2))
			{
				return false;
			}
			num = keyAlgorithmParameters.Length;
			if (keyAlgorithmParameters2.Length != num)
			{
				return false;
			}
			for (int j = 0; j < num; j++)
			{
				if (keyAlgorithmParameters[j] != keyAlgorithmParameters2[j])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06002C05 RID: 11269 RVA: 0x000A4087 File Offset: 0x000A2287
		public override int GetHashCode()
		{
			return this.m_cert.GetHashCode();
		}

		// Token: 0x170005DC RID: 1500
		// (get) Token: 0x06002C06 RID: 11270 RVA: 0x000A4094 File Offset: 0x000A2294
		public X509Certificate Certificate
		{
			get
			{
				return new X509Certificate(this.m_cert);
			}
		}

		// Token: 0x06002C07 RID: 11271 RVA: 0x000A40A1 File Offset: 0x000A22A1
		public override EvidenceBase Clone()
		{
			return new Publisher(this.m_cert);
		}

		// Token: 0x06002C08 RID: 11272 RVA: 0x000A40AE File Offset: 0x000A22AE
		public object Copy()
		{
			return this.Clone();
		}

		// Token: 0x06002C09 RID: 11273 RVA: 0x000A40B8 File Offset: 0x000A22B8
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("System.Security.Policy.Publisher");
			securityElement.AddAttribute("version", "1");
			securityElement.AddChild(new SecurityElement("X509v3Certificate", (this.m_cert != null) ? this.m_cert.GetRawCertDataString() : ""));
			return securityElement;
		}

		// Token: 0x06002C0A RID: 11274 RVA: 0x000A410B File Offset: 0x000A230B
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x06002C0B RID: 11275 RVA: 0x000A4118 File Offset: 0x000A2318
		internal object Normalize()
		{
			return new MemoryStream(this.m_cert.GetRawCertData())
			{
				Position = 0L
			};
		}

		// Token: 0x040011BA RID: 4538
		private X509Certificate m_cert;
	}
}
