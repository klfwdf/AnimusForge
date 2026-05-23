using System;
using System.IO;
using Mono.Cecil.Cil;

namespace Mono.Cecil
{
	// Token: 0x02000278 RID: 632
	internal sealed class ReaderParameters
	{
		// Token: 0x170003B3 RID: 947
		// (get) Token: 0x06000F03 RID: 3843 RVA: 0x0002FDA6 File Offset: 0x0002DFA6
		// (set) Token: 0x06000F04 RID: 3844 RVA: 0x0002FDAE File Offset: 0x0002DFAE
		public ReadingMode ReadingMode
		{
			get
			{
				return this.reading_mode;
			}
			set
			{
				this.reading_mode = value;
			}
		}

		// Token: 0x170003B4 RID: 948
		// (get) Token: 0x06000F05 RID: 3845 RVA: 0x0002FDB7 File Offset: 0x0002DFB7
		// (set) Token: 0x06000F06 RID: 3846 RVA: 0x0002FDBF File Offset: 0x0002DFBF
		public bool InMemory
		{
			get
			{
				return this.in_memory;
			}
			set
			{
				this.in_memory = value;
			}
		}

		// Token: 0x170003B5 RID: 949
		// (get) Token: 0x06000F07 RID: 3847 RVA: 0x0002FDC8 File Offset: 0x0002DFC8
		// (set) Token: 0x06000F08 RID: 3848 RVA: 0x0002FDD0 File Offset: 0x0002DFD0
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

		// Token: 0x170003B6 RID: 950
		// (get) Token: 0x06000F09 RID: 3849 RVA: 0x0002FDD9 File Offset: 0x0002DFD9
		// (set) Token: 0x06000F0A RID: 3850 RVA: 0x0002FDE1 File Offset: 0x0002DFE1
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

		// Token: 0x170003B7 RID: 951
		// (get) Token: 0x06000F0B RID: 3851 RVA: 0x0002FDEA File Offset: 0x0002DFEA
		// (set) Token: 0x06000F0C RID: 3852 RVA: 0x0002FDF2 File Offset: 0x0002DFF2
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

		// Token: 0x170003B8 RID: 952
		// (get) Token: 0x06000F0D RID: 3853 RVA: 0x0002FDFB File Offset: 0x0002DFFB
		// (set) Token: 0x06000F0E RID: 3854 RVA: 0x0002FE03 File Offset: 0x0002E003
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

		// Token: 0x170003B9 RID: 953
		// (get) Token: 0x06000F0F RID: 3855 RVA: 0x0002FE0C File Offset: 0x0002E00C
		// (set) Token: 0x06000F10 RID: 3856 RVA: 0x0002FE14 File Offset: 0x0002E014
		public Stream SymbolStream
		{
			get
			{
				return this.symbol_stream;
			}
			set
			{
				this.symbol_stream = value;
			}
		}

		// Token: 0x170003BA RID: 954
		// (get) Token: 0x06000F11 RID: 3857 RVA: 0x0002FE1D File Offset: 0x0002E01D
		// (set) Token: 0x06000F12 RID: 3858 RVA: 0x0002FE25 File Offset: 0x0002E025
		public ISymbolReaderProvider SymbolReaderProvider
		{
			get
			{
				return this.symbol_reader_provider;
			}
			set
			{
				this.symbol_reader_provider = value;
			}
		}

		// Token: 0x170003BB RID: 955
		// (get) Token: 0x06000F13 RID: 3859 RVA: 0x0002FE2E File Offset: 0x0002E02E
		// (set) Token: 0x06000F14 RID: 3860 RVA: 0x0002FE36 File Offset: 0x0002E036
		public bool ReadSymbols
		{
			get
			{
				return this.read_symbols;
			}
			set
			{
				this.read_symbols = value;
			}
		}

		// Token: 0x170003BC RID: 956
		// (get) Token: 0x06000F15 RID: 3861 RVA: 0x0002FE3F File Offset: 0x0002E03F
		// (set) Token: 0x06000F16 RID: 3862 RVA: 0x0002FE47 File Offset: 0x0002E047
		public bool ThrowIfSymbolsAreNotMatching
		{
			get
			{
				return this.throw_symbols_mismatch;
			}
			set
			{
				this.throw_symbols_mismatch = value;
			}
		}

		// Token: 0x170003BD RID: 957
		// (get) Token: 0x06000F17 RID: 3863 RVA: 0x0002FE50 File Offset: 0x0002E050
		// (set) Token: 0x06000F18 RID: 3864 RVA: 0x0002FE58 File Offset: 0x0002E058
		public bool ReadWrite
		{
			get
			{
				return this.read_write;
			}
			set
			{
				this.read_write = value;
			}
		}

		// Token: 0x170003BE RID: 958
		// (get) Token: 0x06000F19 RID: 3865 RVA: 0x0002FE61 File Offset: 0x0002E061
		// (set) Token: 0x06000F1A RID: 3866 RVA: 0x0002FE69 File Offset: 0x0002E069
		public bool ApplyWindowsRuntimeProjections
		{
			get
			{
				return this.projections;
			}
			set
			{
				this.projections = value;
			}
		}

		// Token: 0x06000F1B RID: 3867 RVA: 0x0002FE72 File Offset: 0x0002E072
		public ReaderParameters()
			: this(ReadingMode.Deferred)
		{
		}

		// Token: 0x06000F1C RID: 3868 RVA: 0x0002FE7B File Offset: 0x0002E07B
		public ReaderParameters(ReadingMode readingMode)
		{
			this.reading_mode = readingMode;
			this.throw_symbols_mismatch = true;
		}

		// Token: 0x0400049A RID: 1178
		private ReadingMode reading_mode;

		// Token: 0x0400049B RID: 1179
		internal IAssemblyResolver assembly_resolver;

		// Token: 0x0400049C RID: 1180
		internal IMetadataResolver metadata_resolver;

		// Token: 0x0400049D RID: 1181
		internal IMetadataImporterProvider metadata_importer_provider;

		// Token: 0x0400049E RID: 1182
		internal IReflectionImporterProvider reflection_importer_provider;

		// Token: 0x0400049F RID: 1183
		private Stream symbol_stream;

		// Token: 0x040004A0 RID: 1184
		private ISymbolReaderProvider symbol_reader_provider;

		// Token: 0x040004A1 RID: 1185
		private bool read_symbols;

		// Token: 0x040004A2 RID: 1186
		private bool throw_symbols_mismatch;

		// Token: 0x040004A3 RID: 1187
		private bool projections;

		// Token: 0x040004A4 RID: 1188
		private bool in_memory;

		// Token: 0x040004A5 RID: 1189
		private bool read_write;
	}
}
