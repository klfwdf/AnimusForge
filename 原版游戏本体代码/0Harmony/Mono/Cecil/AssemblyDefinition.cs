using System;
using System.IO;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x020001E6 RID: 486
	internal sealed class AssemblyDefinition : ICustomAttributeProvider, IMetadataTokenProvider, ISecurityDeclarationProvider, IDisposable
	{
		// Token: 0x17000230 RID: 560
		// (get) Token: 0x0600095A RID: 2394 RVA: 0x0001E8FB File Offset: 0x0001CAFB
		// (set) Token: 0x0600095B RID: 2395 RVA: 0x0001E903 File Offset: 0x0001CB03
		public AssemblyNameDefinition Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
			}
		}

		// Token: 0x17000231 RID: 561
		// (get) Token: 0x0600095C RID: 2396 RVA: 0x0001E90C File Offset: 0x0001CB0C
		public string FullName
		{
			get
			{
				if (this.name == null)
				{
					return string.Empty;
				}
				return this.name.FullName;
			}
		}

		// Token: 0x17000232 RID: 562
		// (get) Token: 0x0600095D RID: 2397 RVA: 0x0001E927 File Offset: 0x0001CB27
		// (set) Token: 0x0600095E RID: 2398 RVA: 0x0001B842 File Offset: 0x00019A42
		public MetadataToken MetadataToken
		{
			get
			{
				return new MetadataToken(TokenType.Assembly, 1);
			}
			set
			{
			}
		}

		// Token: 0x17000233 RID: 563
		// (get) Token: 0x0600095F RID: 2399 RVA: 0x0001E934 File Offset: 0x0001CB34
		public Collection<ModuleDefinition> Modules
		{
			get
			{
				if (this.modules != null)
				{
					return this.modules;
				}
				if (this.main_module.HasImage)
				{
					return this.main_module.Read<AssemblyDefinition, Collection<ModuleDefinition>>(ref this.modules, this, (AssemblyDefinition _, MetadataReader reader) => reader.ReadModules());
				}
				Interlocked.CompareExchange<Collection<ModuleDefinition>>(ref this.modules, new Collection<ModuleDefinition>(1) { this.main_module }, null);
				return this.modules;
			}
		}

		// Token: 0x17000234 RID: 564
		// (get) Token: 0x06000960 RID: 2400 RVA: 0x0001E9B4 File Offset: 0x0001CBB4
		public ModuleDefinition MainModule
		{
			get
			{
				return this.main_module;
			}
		}

		// Token: 0x17000235 RID: 565
		// (get) Token: 0x06000961 RID: 2401 RVA: 0x0001E9BC File Offset: 0x0001CBBC
		// (set) Token: 0x06000962 RID: 2402 RVA: 0x0001E9C9 File Offset: 0x0001CBC9
		public MethodDefinition EntryPoint
		{
			get
			{
				return this.main_module.EntryPoint;
			}
			set
			{
				this.main_module.EntryPoint = value;
			}
		}

		// Token: 0x17000236 RID: 566
		// (get) Token: 0x06000963 RID: 2403 RVA: 0x0001E9D7 File Offset: 0x0001CBD7
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
				{
					return this.custom_attributes.Count > 0;
				}
				return this.GetHasCustomAttributes(this.main_module);
			}
		}

		// Token: 0x17000237 RID: 567
		// (get) Token: 0x06000964 RID: 2404 RVA: 0x0001E9FC File Offset: 0x0001CBFC
		public Collection<CustomAttribute> CustomAttributes
		{
			get
			{
				return this.custom_attributes ?? this.GetCustomAttributes(ref this.custom_attributes, this.main_module);
			}
		}

		// Token: 0x17000238 RID: 568
		// (get) Token: 0x06000965 RID: 2405 RVA: 0x0001EA1A File Offset: 0x0001CC1A
		public bool HasSecurityDeclarations
		{
			get
			{
				if (this.security_declarations != null)
				{
					return this.security_declarations.Count > 0;
				}
				return this.GetHasSecurityDeclarations(this.main_module);
			}
		}

		// Token: 0x17000239 RID: 569
		// (get) Token: 0x06000966 RID: 2406 RVA: 0x0001EA3F File Offset: 0x0001CC3F
		public Collection<SecurityDeclaration> SecurityDeclarations
		{
			get
			{
				return this.security_declarations ?? this.GetSecurityDeclarations(ref this.security_declarations, this.main_module);
			}
		}

		// Token: 0x06000967 RID: 2407 RVA: 0x00002B15 File Offset: 0x00000D15
		internal AssemblyDefinition()
		{
		}

		// Token: 0x06000968 RID: 2408 RVA: 0x0001EA60 File Offset: 0x0001CC60
		public void Dispose()
		{
			if (this.modules == null)
			{
				this.main_module.Dispose();
				return;
			}
			Collection<ModuleDefinition> modules = this.Modules;
			for (int i = 0; i < modules.Count; i++)
			{
				modules[i].Dispose();
			}
		}

		// Token: 0x06000969 RID: 2409 RVA: 0x0001EAA5 File Offset: 0x0001CCA5
		public static AssemblyDefinition CreateAssembly(AssemblyNameDefinition assemblyName, string moduleName, ModuleKind kind)
		{
			return AssemblyDefinition.CreateAssembly(assemblyName, moduleName, new ModuleParameters
			{
				Kind = kind
			});
		}

		// Token: 0x0600096A RID: 2410 RVA: 0x0001EABC File Offset: 0x0001CCBC
		public static AssemblyDefinition CreateAssembly(AssemblyNameDefinition assemblyName, string moduleName, ModuleParameters parameters)
		{
			if (assemblyName == null)
			{
				throw new ArgumentNullException("assemblyName");
			}
			if (moduleName == null)
			{
				throw new ArgumentNullException("moduleName");
			}
			Mixin.CheckParameters(parameters);
			if (parameters.Kind == ModuleKind.NetModule)
			{
				throw new ArgumentException("kind");
			}
			AssemblyDefinition assembly = ModuleDefinition.CreateModule(moduleName, parameters).Assembly;
			assembly.Name = assemblyName;
			return assembly;
		}

		// Token: 0x0600096B RID: 2411 RVA: 0x0001EB12 File Offset: 0x0001CD12
		public static AssemblyDefinition ReadAssembly(string fileName)
		{
			return AssemblyDefinition.ReadAssembly(ModuleDefinition.ReadModule(fileName));
		}

		// Token: 0x0600096C RID: 2412 RVA: 0x0001EB1F File Offset: 0x0001CD1F
		public static AssemblyDefinition ReadAssembly(string fileName, ReaderParameters parameters)
		{
			return AssemblyDefinition.ReadAssembly(ModuleDefinition.ReadModule(fileName, parameters));
		}

		// Token: 0x0600096D RID: 2413 RVA: 0x0001EB2D File Offset: 0x0001CD2D
		public static AssemblyDefinition ReadAssembly(Stream stream)
		{
			return AssemblyDefinition.ReadAssembly(ModuleDefinition.ReadModule(stream));
		}

		// Token: 0x0600096E RID: 2414 RVA: 0x0001EB3A File Offset: 0x0001CD3A
		public static AssemblyDefinition ReadAssembly(Stream stream, ReaderParameters parameters)
		{
			return AssemblyDefinition.ReadAssembly(ModuleDefinition.ReadModule(stream, parameters));
		}

		// Token: 0x0600096F RID: 2415 RVA: 0x0001EB48 File Offset: 0x0001CD48
		private static AssemblyDefinition ReadAssembly(ModuleDefinition module)
		{
			AssemblyDefinition assembly = module.Assembly;
			if (assembly == null)
			{
				throw new ArgumentException();
			}
			return assembly;
		}

		// Token: 0x06000970 RID: 2416 RVA: 0x0001EB59 File Offset: 0x0001CD59
		public void Write(string fileName)
		{
			this.Write(fileName, new WriterParameters());
		}

		// Token: 0x06000971 RID: 2417 RVA: 0x0001EB67 File Offset: 0x0001CD67
		public void Write(string fileName, WriterParameters parameters)
		{
			this.main_module.Write(fileName, parameters);
		}

		// Token: 0x06000972 RID: 2418 RVA: 0x0001EB76 File Offset: 0x0001CD76
		public void Write()
		{
			this.main_module.Write();
		}

		// Token: 0x06000973 RID: 2419 RVA: 0x0001EB83 File Offset: 0x0001CD83
		public void Write(WriterParameters parameters)
		{
			this.main_module.Write(parameters);
		}

		// Token: 0x06000974 RID: 2420 RVA: 0x0001EB91 File Offset: 0x0001CD91
		public void Write(Stream stream)
		{
			this.Write(stream, new WriterParameters());
		}

		// Token: 0x06000975 RID: 2421 RVA: 0x0001EB9F File Offset: 0x0001CD9F
		public void Write(Stream stream, WriterParameters parameters)
		{
			this.main_module.Write(stream, parameters);
		}

		// Token: 0x06000976 RID: 2422 RVA: 0x0001EBAE File Offset: 0x0001CDAE
		public override string ToString()
		{
			return this.FullName;
		}

		// Token: 0x0400031A RID: 794
		private AssemblyNameDefinition name;

		// Token: 0x0400031B RID: 795
		internal ModuleDefinition main_module;

		// Token: 0x0400031C RID: 796
		private Collection<ModuleDefinition> modules;

		// Token: 0x0400031D RID: 797
		private Collection<CustomAttribute> custom_attributes;

		// Token: 0x0400031E RID: 798
		private Collection<SecurityDeclaration> security_declarations;
	}
}
