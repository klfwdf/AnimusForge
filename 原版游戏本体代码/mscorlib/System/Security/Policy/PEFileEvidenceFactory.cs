using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Policy
{
	// Token: 0x02000361 RID: 865
	internal sealed class PEFileEvidenceFactory : IRuntimeEvidenceFactory
	{
		// Token: 0x06002ABB RID: 10939 RVA: 0x0009E56A File Offset: 0x0009C76A
		[SecurityCritical]
		private PEFileEvidenceFactory(SafePEFileHandle peFile)
		{
			this.m_peFile = peFile;
		}

		// Token: 0x170005B4 RID: 1460
		// (get) Token: 0x06002ABC RID: 10940 RVA: 0x0009E579 File Offset: 0x0009C779
		internal SafePEFileHandle PEFile
		{
			[SecurityCritical]
			get
			{
				return this.m_peFile;
			}
		}

		// Token: 0x170005B5 RID: 1461
		// (get) Token: 0x06002ABD RID: 10941 RVA: 0x0009E581 File Offset: 0x0009C781
		public IEvidenceFactory Target
		{
			get
			{
				return null;
			}
		}

		// Token: 0x06002ABE RID: 10942 RVA: 0x0009E584 File Offset: 0x0009C784
		[SecurityCritical]
		private static Evidence CreateSecurityIdentity(SafePEFileHandle peFile, Evidence hostProvidedEvidence)
		{
			PEFileEvidenceFactory target = new PEFileEvidenceFactory(peFile);
			Evidence evidence = new Evidence(target);
			if (hostProvidedEvidence != null)
			{
				evidence.MergeWithNoDuplicates(hostProvidedEvidence);
			}
			return evidence;
		}

		// Token: 0x06002ABF RID: 10943
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void FireEvidenceGeneratedEvent(SafePEFileHandle peFile, EvidenceTypeGenerated type);

		// Token: 0x06002AC0 RID: 10944 RVA: 0x0009E5AA File Offset: 0x0009C7AA
		[SecuritySafeCritical]
		internal void FireEvidenceGeneratedEvent(EvidenceTypeGenerated type)
		{
			PEFileEvidenceFactory.FireEvidenceGeneratedEvent(this.m_peFile, type);
		}

		// Token: 0x06002AC1 RID: 10945
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetAssemblySuppliedEvidence(SafePEFileHandle peFile, ObjectHandleOnStack retSerializedEvidence);

		// Token: 0x06002AC2 RID: 10946 RVA: 0x0009E5B8 File Offset: 0x0009C7B8
		[SecuritySafeCritical]
		public IEnumerable<EvidenceBase> GetFactorySuppliedEvidence()
		{
			if (this.m_assemblyProvidedEvidence == null)
			{
				byte[] array = null;
				PEFileEvidenceFactory.GetAssemblySuppliedEvidence(this.m_peFile, JitHelpers.GetObjectHandleOnStack<byte[]>(ref array));
				this.m_assemblyProvidedEvidence = new List<EvidenceBase>();
				if (array != null)
				{
					Evidence evidence = new Evidence();
					new SecurityPermission(SecurityPermissionFlag.SerializationFormatter).Assert();
					try
					{
						BinaryFormatter binaryFormatter = new BinaryFormatter();
						using (MemoryStream memoryStream = new MemoryStream(array))
						{
							evidence = (Evidence)binaryFormatter.Deserialize(memoryStream);
						}
					}
					catch
					{
					}
					CodeAccessPermission.RevertAssert();
					if (evidence != null)
					{
						IEnumerator assemblyEnumerator = evidence.GetAssemblyEnumerator();
						while (assemblyEnumerator.MoveNext())
						{
							if (assemblyEnumerator.Current != null)
							{
								EvidenceBase evidenceBase = assemblyEnumerator.Current as EvidenceBase;
								if (evidenceBase == null)
								{
									evidenceBase = new LegacyEvidenceWrapper(assemblyEnumerator.Current);
								}
								this.m_assemblyProvidedEvidence.Add(evidenceBase);
							}
						}
					}
				}
			}
			return this.m_assemblyProvidedEvidence;
		}

		// Token: 0x06002AC3 RID: 10947
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetLocationEvidence(SafePEFileHandle peFile, out SecurityZone zone, StringHandleOnStack retUrl);

		// Token: 0x06002AC4 RID: 10948
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetPublisherCertificate(SafePEFileHandle peFile, ObjectHandleOnStack retCertificate);

		// Token: 0x06002AC5 RID: 10949 RVA: 0x0009E6A8 File Offset: 0x0009C8A8
		public EvidenceBase GenerateEvidence(Type evidenceType)
		{
			if (evidenceType == typeof(Site))
			{
				return this.GenerateSiteEvidence();
			}
			if (evidenceType == typeof(Url))
			{
				return this.GenerateUrlEvidence();
			}
			if (evidenceType == typeof(Zone))
			{
				return this.GenerateZoneEvidence();
			}
			if (evidenceType == typeof(Publisher))
			{
				return this.GeneratePublisherEvidence();
			}
			return null;
		}

		// Token: 0x06002AC6 RID: 10950 RVA: 0x0009E71C File Offset: 0x0009C91C
		[SecuritySafeCritical]
		private void GenerateLocationEvidence()
		{
			if (!this.m_generatedLocationEvidence)
			{
				SecurityZone securityZone = SecurityZone.NoZone;
				string text = null;
				PEFileEvidenceFactory.GetLocationEvidence(this.m_peFile, out securityZone, JitHelpers.GetStringHandleOnStack(ref text));
				if (securityZone != SecurityZone.NoZone)
				{
					this.m_zoneEvidence = new Zone(securityZone);
				}
				if (!string.IsNullOrEmpty(text))
				{
					this.m_urlEvidence = new Url(text, true);
					if (!text.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
					{
						this.m_siteEvidence = Site.CreateFromUrl(text);
					}
				}
				this.m_generatedLocationEvidence = true;
			}
		}

		// Token: 0x06002AC7 RID: 10951 RVA: 0x0009E790 File Offset: 0x0009C990
		[SecuritySafeCritical]
		private Publisher GeneratePublisherEvidence()
		{
			byte[] array = null;
			PEFileEvidenceFactory.GetPublisherCertificate(this.m_peFile, JitHelpers.GetObjectHandleOnStack<byte[]>(ref array));
			if (array == null)
			{
				return null;
			}
			return new Publisher(new X509Certificate(array));
		}

		// Token: 0x06002AC8 RID: 10952 RVA: 0x0009E7C1 File Offset: 0x0009C9C1
		private Site GenerateSiteEvidence()
		{
			if (this.m_siteEvidence == null)
			{
				this.GenerateLocationEvidence();
			}
			return this.m_siteEvidence;
		}

		// Token: 0x06002AC9 RID: 10953 RVA: 0x0009E7D7 File Offset: 0x0009C9D7
		private Url GenerateUrlEvidence()
		{
			if (this.m_urlEvidence == null)
			{
				this.GenerateLocationEvidence();
			}
			return this.m_urlEvidence;
		}

		// Token: 0x06002ACA RID: 10954 RVA: 0x0009E7ED File Offset: 0x0009C9ED
		private Zone GenerateZoneEvidence()
		{
			if (this.m_zoneEvidence == null)
			{
				this.GenerateLocationEvidence();
			}
			return this.m_zoneEvidence;
		}

		// Token: 0x0400116A RID: 4458
		[SecurityCritical]
		private SafePEFileHandle m_peFile;

		// Token: 0x0400116B RID: 4459
		private List<EvidenceBase> m_assemblyProvidedEvidence;

		// Token: 0x0400116C RID: 4460
		private bool m_generatedLocationEvidence;

		// Token: 0x0400116D RID: 4461
		private Site m_siteEvidence;

		// Token: 0x0400116E RID: 4462
		private Url m_urlEvidence;

		// Token: 0x0400116F RID: 4463
		private Zone m_zoneEvidence;
	}
}
