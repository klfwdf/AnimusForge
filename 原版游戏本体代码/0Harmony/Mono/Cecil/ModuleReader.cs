using System;
using Mono.Cecil.Cil;
using Mono.Cecil.PE;

namespace Mono.Cecil
{
	// Token: 0x020001ED RID: 493
	internal abstract class ModuleReader
	{
		// Token: 0x060009A3 RID: 2467 RVA: 0x0001F15D File Offset: 0x0001D35D
		protected ModuleReader(Image image, ReadingMode mode)
		{
			this.module = new ModuleDefinition(image);
			this.module.ReadingMode = mode;
		}

		// Token: 0x060009A4 RID: 2468
		protected abstract void ReadModule();

		// Token: 0x060009A5 RID: 2469
		public abstract void ReadSymbols(ModuleDefinition module);

		// Token: 0x060009A6 RID: 2470 RVA: 0x0001F17D File Offset: 0x0001D37D
		protected void ReadModuleManifest(MetadataReader reader)
		{
			reader.Populate(this.module);
			this.ReadAssembly(reader);
		}

		// Token: 0x060009A7 RID: 2471 RVA: 0x0001F194 File Offset: 0x0001D394
		private void ReadAssembly(MetadataReader reader)
		{
			AssemblyNameDefinition name = reader.ReadAssemblyNameDefinition();
			if (name == null)
			{
				this.module.kind = ModuleKind.NetModule;
				return;
			}
			AssemblyDefinition assembly = new AssemblyDefinition();
			assembly.Name = name;
			this.module.assembly = assembly;
			assembly.main_module = this.module;
		}

		// Token: 0x060009A8 RID: 2472 RVA: 0x0001F1E0 File Offset: 0x0001D3E0
		public static ModuleDefinition CreateModule(Image image, ReaderParameters parameters)
		{
			ModuleReader moduleReader = ModuleReader.CreateModuleReader(image, parameters.ReadingMode);
			ModuleDefinition module = moduleReader.module;
			if (parameters.assembly_resolver != null)
			{
				module.assembly_resolver = Disposable.NotOwned<IAssemblyResolver>(parameters.assembly_resolver);
			}
			if (parameters.metadata_resolver != null)
			{
				module.metadata_resolver = parameters.metadata_resolver;
			}
			if (parameters.metadata_importer_provider != null)
			{
				module.metadata_importer = parameters.metadata_importer_provider.GetMetadataImporter(module);
			}
			if (parameters.reflection_importer_provider != null)
			{
				module.reflection_importer = parameters.reflection_importer_provider.GetReflectionImporter(module);
			}
			ModuleReader.GetMetadataKind(module, parameters);
			moduleReader.ReadModule();
			ModuleReader.ReadSymbols(module, parameters);
			moduleReader.ReadSymbols(module);
			if (parameters.ReadingMode == ReadingMode.Immediate)
			{
				module.MetadataSystem.Clear();
			}
			return module;
		}

		// Token: 0x060009A9 RID: 2473 RVA: 0x0001F290 File Offset: 0x0001D490
		private static void ReadSymbols(ModuleDefinition module, ReaderParameters parameters)
		{
			ISymbolReaderProvider symbol_reader_provider = parameters.SymbolReaderProvider;
			if (symbol_reader_provider == null && parameters.ReadSymbols)
			{
				symbol_reader_provider = new DefaultSymbolReaderProvider();
			}
			if (symbol_reader_provider != null)
			{
				module.SymbolReaderProvider = symbol_reader_provider;
				ISymbolReader reader = ((parameters.SymbolStream != null) ? symbol_reader_provider.GetSymbolReader(module, parameters.SymbolStream) : symbol_reader_provider.GetSymbolReader(module, module.FileName));
				if (reader != null)
				{
					try
					{
						module.ReadSymbols(reader, parameters.ThrowIfSymbolsAreNotMatching);
					}
					catch (Exception)
					{
						reader.Dispose();
						throw;
					}
				}
			}
			if (module.Image.HasDebugTables())
			{
				module.ReadSymbols(new PortablePdbReader(module.Image, module));
			}
		}

		// Token: 0x060009AA RID: 2474 RVA: 0x0001F330 File Offset: 0x0001D530
		private static void GetMetadataKind(ModuleDefinition module, ReaderParameters parameters)
		{
			if (!parameters.ApplyWindowsRuntimeProjections)
			{
				module.MetadataKind = MetadataKind.Ecma335;
				return;
			}
			string runtime_version = module.RuntimeVersion;
			if (!runtime_version.Contains("WindowsRuntime"))
			{
				module.MetadataKind = MetadataKind.Ecma335;
				return;
			}
			if (runtime_version.Contains("CLR"))
			{
				module.MetadataKind = MetadataKind.ManagedWindowsMetadata;
				return;
			}
			module.MetadataKind = MetadataKind.WindowsMetadata;
		}

		// Token: 0x060009AB RID: 2475 RVA: 0x0001F385 File Offset: 0x0001D585
		private static ModuleReader CreateModuleReader(Image image, ReadingMode mode)
		{
			if (mode == ReadingMode.Immediate)
			{
				return new ImmediateModuleReader(image);
			}
			if (mode != ReadingMode.Deferred)
			{
				throw new ArgumentException();
			}
			return new DeferredModuleReader(image);
		}

		// Token: 0x0400033B RID: 827
		protected readonly ModuleDefinition module;
	}
}
