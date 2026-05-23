using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation.Manifest
{
	// Token: 0x02000710 RID: 1808
	[StructLayout(LayoutKind.Sequential)]
	internal class MetadataSectionEntry : IDisposable
	{
		// Token: 0x060050FA RID: 20730 RVA: 0x0011DE94 File Offset: 0x0011C094
		~MetadataSectionEntry()
		{
			this.Dispose(false);
		}

		// Token: 0x060050FB RID: 20731 RVA: 0x0011DEC4 File Offset: 0x0011C0C4
		void IDisposable.Dispose()
		{
			this.Dispose(true);
		}

		// Token: 0x060050FC RID: 20732 RVA: 0x0011DED0 File Offset: 0x0011C0D0
		[SecuritySafeCritical]
		public void Dispose(bool fDisposing)
		{
			if (this.ManifestHash != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.ManifestHash);
				this.ManifestHash = IntPtr.Zero;
			}
			if (this.MvidValue != IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(this.MvidValue);
				this.MvidValue = IntPtr.Zero;
			}
			if (fDisposing)
			{
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x040023BD RID: 9149
		public uint SchemaVersion;

		// Token: 0x040023BE RID: 9150
		public uint ManifestFlags;

		// Token: 0x040023BF RID: 9151
		public uint UsagePatterns;

		// Token: 0x040023C0 RID: 9152
		public IDefinitionIdentity CdfIdentity;

		// Token: 0x040023C1 RID: 9153
		[MarshalAs(UnmanagedType.LPWStr)]
		public string LocalPath;

		// Token: 0x040023C2 RID: 9154
		public uint HashAlgorithm;

		// Token: 0x040023C3 RID: 9155
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr ManifestHash;

		// Token: 0x040023C4 RID: 9156
		public uint ManifestHashSize;

		// Token: 0x040023C5 RID: 9157
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ContentType;

		// Token: 0x040023C6 RID: 9158
		[MarshalAs(UnmanagedType.LPWStr)]
		public string RuntimeImageVersion;

		// Token: 0x040023C7 RID: 9159
		[MarshalAs(UnmanagedType.SysInt)]
		public IntPtr MvidValue;

		// Token: 0x040023C8 RID: 9160
		public uint MvidValueSize;

		// Token: 0x040023C9 RID: 9161
		public DescriptionMetadataEntry DescriptionData;

		// Token: 0x040023CA RID: 9162
		public DeploymentMetadataEntry DeploymentData;

		// Token: 0x040023CB RID: 9163
		public DependentOSMetadataEntry DependentOSData;

		// Token: 0x040023CC RID: 9164
		[MarshalAs(UnmanagedType.LPWStr)]
		public string defaultPermissionSetID;

		// Token: 0x040023CD RID: 9165
		[MarshalAs(UnmanagedType.LPWStr)]
		public string RequestedExecutionLevel;

		// Token: 0x040023CE RID: 9166
		public bool RequestedExecutionLevelUIAccess;

		// Token: 0x040023CF RID: 9167
		public IReferenceIdentity ResourceTypeResourcesDependency;

		// Token: 0x040023D0 RID: 9168
		public IReferenceIdentity ResourceTypeManifestResourcesDependency;

		// Token: 0x040023D1 RID: 9169
		[MarshalAs(UnmanagedType.LPWStr)]
		public string KeyInfoElement;

		// Token: 0x040023D2 RID: 9170
		public CompatibleFrameworksMetadataEntry CompatibleFrameworksData;
	}
}
