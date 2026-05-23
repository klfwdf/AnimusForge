using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Policy
{
	// Token: 0x02000349 RID: 841
	internal sealed class AssemblyEvidenceFactory : IRuntimeEvidenceFactory
	{
		// Token: 0x060029C8 RID: 10696 RVA: 0x0009A6CE File Offset: 0x000988CE
		private AssemblyEvidenceFactory(RuntimeAssembly targetAssembly, PEFileEvidenceFactory peFileFactory)
		{
			this.m_targetAssembly = targetAssembly;
			this.m_peFileFactory = peFileFactory;
		}

		// Token: 0x1700057E RID: 1406
		// (get) Token: 0x060029C9 RID: 10697 RVA: 0x0009A6E4 File Offset: 0x000988E4
		internal SafePEFileHandle PEFile
		{
			[SecurityCritical]
			get
			{
				return this.m_peFileFactory.PEFile;
			}
		}

		// Token: 0x1700057F RID: 1407
		// (get) Token: 0x060029CA RID: 10698 RVA: 0x0009A6F1 File Offset: 0x000988F1
		public IEvidenceFactory Target
		{
			get
			{
				return this.m_targetAssembly;
			}
		}

		// Token: 0x060029CB RID: 10699 RVA: 0x0009A6FC File Offset: 0x000988FC
		public EvidenceBase GenerateEvidence(Type evidenceType)
		{
			EvidenceBase evidenceBase = this.m_peFileFactory.GenerateEvidence(evidenceType);
			if (evidenceBase != null)
			{
				return evidenceBase;
			}
			if (evidenceType == typeof(GacInstalled))
			{
				return this.GenerateGacEvidence();
			}
			if (evidenceType == typeof(Hash))
			{
				return this.GenerateHashEvidence();
			}
			if (evidenceType == typeof(PermissionRequestEvidence))
			{
				return this.GeneratePermissionRequestEvidence();
			}
			if (evidenceType == typeof(StrongName))
			{
				return this.GenerateStrongNameEvidence();
			}
			return null;
		}

		// Token: 0x060029CC RID: 10700 RVA: 0x0009A780 File Offset: 0x00098980
		private GacInstalled GenerateGacEvidence()
		{
			if (!this.m_targetAssembly.GlobalAssemblyCache)
			{
				return null;
			}
			this.m_peFileFactory.FireEvidenceGeneratedEvent(EvidenceTypeGenerated.Gac);
			return new GacInstalled();
		}

		// Token: 0x060029CD RID: 10701 RVA: 0x0009A7A2 File Offset: 0x000989A2
		private Hash GenerateHashEvidence()
		{
			if (this.m_targetAssembly.IsDynamic)
			{
				return null;
			}
			this.m_peFileFactory.FireEvidenceGeneratedEvent(EvidenceTypeGenerated.Hash);
			return new Hash(this.m_targetAssembly);
		}

		// Token: 0x060029CE RID: 10702 RVA: 0x0009A7CC File Offset: 0x000989CC
		[SecuritySafeCritical]
		private PermissionRequestEvidence GeneratePermissionRequestEvidence()
		{
			PermissionSet permissionSet = null;
			PermissionSet permissionSet2 = null;
			PermissionSet permissionSet3 = null;
			AssemblyEvidenceFactory.GetAssemblyPermissionRequests(this.m_targetAssembly.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<PermissionSet>(ref permissionSet), JitHelpers.GetObjectHandleOnStack<PermissionSet>(ref permissionSet2), JitHelpers.GetObjectHandleOnStack<PermissionSet>(ref permissionSet3));
			if (permissionSet != null || permissionSet2 != null || permissionSet3 != null)
			{
				return new PermissionRequestEvidence(permissionSet, permissionSet2, permissionSet3);
			}
			return null;
		}

		// Token: 0x060029CF RID: 10703 RVA: 0x0009A818 File Offset: 0x00098A18
		[SecuritySafeCritical]
		private StrongName GenerateStrongNameEvidence()
		{
			byte[] array = null;
			string name = null;
			ushort major = 0;
			ushort minor = 0;
			ushort build = 0;
			ushort revision = 0;
			AssemblyEvidenceFactory.GetStrongNameInformation(this.m_targetAssembly.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack<byte[]>(ref array), JitHelpers.GetStringHandleOnStack(ref name), out major, out minor, out build, out revision);
			if (array == null || array.Length == 0)
			{
				return null;
			}
			return new StrongName(new StrongNamePublicKeyBlob(array), name, new Version((int)major, (int)minor, (int)build, (int)revision), this.m_targetAssembly);
		}

		// Token: 0x060029D0 RID: 10704
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetAssemblyPermissionRequests(RuntimeAssembly assembly, ObjectHandleOnStack retMinimumPermissions, ObjectHandleOnStack retOptionalPermissions, ObjectHandleOnStack retRefusedPermissions);

		// Token: 0x060029D1 RID: 10705 RVA: 0x0009A87F File Offset: 0x00098A7F
		public IEnumerable<EvidenceBase> GetFactorySuppliedEvidence()
		{
			return this.m_peFileFactory.GetFactorySuppliedEvidence();
		}

		// Token: 0x060029D2 RID: 10706
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void GetStrongNameInformation(RuntimeAssembly assembly, ObjectHandleOnStack retPublicKeyBlob, StringHandleOnStack retSimpleName, out ushort majorVersion, out ushort minorVersion, out ushort build, out ushort revision);

		// Token: 0x060029D3 RID: 10707 RVA: 0x0009A88C File Offset: 0x00098A8C
		[SecurityCritical]
		private static Evidence UpgradeSecurityIdentity(Evidence peFileEvidence, RuntimeAssembly targetAssembly)
		{
			peFileEvidence.Target = new AssemblyEvidenceFactory(targetAssembly, peFileEvidence.Target as PEFileEvidenceFactory);
			HostSecurityManager hostSecurityManager = AppDomain.CurrentDomain.HostSecurityManager;
			if ((hostSecurityManager.Flags & HostSecurityManagerOptions.HostAssemblyEvidence) == HostSecurityManagerOptions.HostAssemblyEvidence)
			{
				peFileEvidence = hostSecurityManager.ProvideAssemblyEvidence(targetAssembly, peFileEvidence);
				if (peFileEvidence == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("Policy_NullHostEvidence", new object[]
					{
						hostSecurityManager.GetType().FullName,
						targetAssembly.FullName
					}));
				}
			}
			return peFileEvidence;
		}

		// Token: 0x04001126 RID: 4390
		private PEFileEvidenceFactory m_peFileFactory;

		// Token: 0x04001127 RID: 4391
		private RuntimeAssembly m_targetAssembly;
	}
}
