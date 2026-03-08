using System;

namespace Mono.Cecil
{
	// Token: 0x02000279 RID: 633
	internal sealed class ModuleParameters
	{
		// Token: 0x170003BF RID: 959
		// (get) Token: 0x06000F1D RID: 3869 RVA: 0x0002FE91 File Offset: 0x0002E091
		// (set) Token: 0x06000F1E RID: 3870 RVA: 0x0002FE99 File Offset: 0x0002E099
		public ModuleKind Kind
		{
			get
			{
				return this.kind;
			}
			set
			{
				this.kind = value;
			}
		}

		// Token: 0x170003C0 RID: 960
		// (get) Token: 0x06000F1F RID: 3871 RVA: 0x0002FEA2 File Offset: 0x0002E0A2
		// (set) Token: 0x06000F20 RID: 3872 RVA: 0x0002FEAA File Offset: 0x0002E0AA
		public TargetRuntime Runtime
		{
			get
			{
				return this.runtime;
			}
			set
			{
				this.runtime = value;
			}
		}

		// Token: 0x170003C1 RID: 961
		// (get) Token: 0x06000F21 RID: 3873 RVA: 0x0002FEB3 File Offset: 0x0002E0B3
		// (set) Token: 0x06000F22 RID: 3874 RVA: 0x0002FEBB File Offset: 0x0002E0BB
		public uint? Timestamp
		{
			get
			{
				return this.timestamp;
			}
			set
			{
				this.timestamp = value;
			}
		}

		// Token: 0x170003C2 RID: 962
		// (get) Token: 0x06000F23 RID: 3875 RVA: 0x0002FEC4 File Offset: 0x0002E0C4
		// (set) Token: 0x06000F24 RID: 3876 RVA: 0x0002FECC File Offset: 0x0002E0CC
		public TargetArchitecture Architecture
		{
			get
			{
				return this.architecture;
			}
			set
			{
				this.architecture = value;
			}
		}

		// Token: 0x170003C3 RID: 963
		// (get) Token: 0x06000F25 RID: 3877 RVA: 0x0002FED5 File Offset: 0x0002E0D5
		// (set) Token: 0x06000F26 RID: 3878 RVA: 0x0002FEDD File Offset: 0x0002E0DD
		public IAssemblyResolver AssemblyResolver
		{
			get
			{
				return this.assembly_resolver;
			}
			set
			{
				this.assembly_resolver = value;
			}
		}

		// Token: 0x170003C4 RID: 964
		// (get) Token: 0x06000F27 RID: 3879 RVA: 0x0002FEE6 File Offset: 0x0002E0E6
		// (set) Token: 0x06000F28 RID: 3880 RVA: 0x0002FEEE File Offset: 0x0002E0EE
		public IMetadataResolver MetadataResolver
		{
			get
			{
				return this.metadata_resolver;
			}
			set
			{
				this.metadata_resolver = value;
			}
		}

		// Token: 0x170003C5 RID: 965
		// (get) Token: 0x06000F29 RID: 3881 RVA: 0x0002FEF7 File Offset: 0x0002E0F7
		// (set) Token: 0x06000F2A RID: 3882 RVA: 0x0002FEFF File Offset: 0x0002E0FF
		public IMetadataImporterProvider MetadataImporterProvider
		{
			get
			{
				return this.metadata_importer_provider;
			}
			set
			{
				this.metadata_importer_provider = value;
			}
		}

		// Token: 0x170003C6 RID: 966
		// (get) Token: 0x06000F2B RID: 3883 RVA: 0x0002FF08 File Offset: 0x0002E108
		// (set) Token: 0x06000F2C RID: 3884 RVA: 0x0002FF10 File Offset: 0x0002E110
		public IReflectionImporterProvider ReflectionImporterProvider
		{
			get
			{
				return this.reflection_importer_provider;
			}
			set
			{
				this.reflection_importer_provider = value;
			}
		}

		// Token: 0x06000F2D RID: 3885 RVA: 0x0002FF19 File Offset: 0x0002E119
		public ModuleParameters()
		{
			this.kind = ModuleKind.Dll;
			this.Runtime = ModuleParameters.GetCurrentRuntime();
			this.architecture = TargetArchitecture.I386;
		}

		// Token: 0x06000F2E RID: 3886 RVA: 0x0002FF3E File Offset: 0x0002E13E
		private static TargetRuntime GetCurrentRuntime()
		{
			return typeof(object).Assembly.ImageRuntimeVersion.ParseRuntime();
		}

		// Token: 0x040004A6 RID: 1190
		private ModuleKind kind;

		// Token: 0x040004A7 RID: 1191
		private TargetRuntime runtime;

		// Token: 0x040004A8 RID: 1192
		private uint? timestamp;

		// Token: 0x040004A9 RID: 1193
		private TargetArchitecture architecture;

		// Token: 0x040004AA RID: 1194
		private IAssemblyResolver assembly_resolver;

		// Token: 0x040004AB RID: 1195
		private IMetadataResolver metadata_resolver;

		// Token: 0x040004AC RID: 1196
		private IMetadataImporterProvider metadata_importer_provider;

		// Token: 0x040004AD RID: 1197
		private IReflectionImporterProvider reflection_importer_provider;
	}
}
