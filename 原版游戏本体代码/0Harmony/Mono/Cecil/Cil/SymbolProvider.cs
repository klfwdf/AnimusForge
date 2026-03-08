using System;
using System.IO;
using System.Reflection;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000325 RID: 805
	internal static class SymbolProvider
	{
		// Token: 0x060014CC RID: 5324 RVA: 0x00041AEC File Offset: 0x0003FCEC
		private static AssemblyName GetSymbolAssemblyName(SymbolKind kind)
		{
			if (kind == SymbolKind.PortablePdb)
			{
				throw new ArgumentException();
			}
			string suffix = SymbolProvider.GetSymbolNamespace(kind);
			AssemblyName cecil_name = typeof(SymbolProvider).Assembly.GetName();
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.Name = cecil_name.Name + "." + suffix;
			assemblyName.Version = cecil_name.Version;
			assemblyName.CultureInfo = cecil_name.CultureInfo;
			assemblyName.SetPublicKeyToken(cecil_name.GetPublicKeyToken());
			return assemblyName;
		}

		// Token: 0x060014CD RID: 5325 RVA: 0x00041B60 File Offset: 0x0003FD60
		private static Type GetSymbolType(SymbolKind kind, string fullname)
		{
			Type type = Type.GetType(fullname);
			if (type != null)
			{
				return type;
			}
			AssemblyName assembly_name = SymbolProvider.GetSymbolAssemblyName(kind);
			type = Type.GetType(fullname + ", " + assembly_name.FullName);
			if (type != null)
			{
				return type;
			}
			try
			{
				Assembly assembly = Assembly.Load(assembly_name);
				if (assembly != null)
				{
					return assembly.GetType(fullname);
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (FileLoadException)
			{
			}
			return null;
		}

		// Token: 0x060014CE RID: 5326 RVA: 0x00041BEC File Offset: 0x0003FDEC
		public static ISymbolReaderProvider GetReaderProvider(SymbolKind kind)
		{
			if (kind == SymbolKind.PortablePdb)
			{
				return new PortablePdbReaderProvider();
			}
			if (kind == SymbolKind.EmbeddedPortablePdb)
			{
				return new EmbeddedPortablePdbReaderProvider();
			}
			string provider_name = SymbolProvider.GetSymbolTypeName(kind, "ReaderProvider");
			Type symbolType = SymbolProvider.GetSymbolType(kind, provider_name);
			if (symbolType == null)
			{
				throw new TypeLoadException("Could not find symbol provider type " + provider_name);
			}
			return (ISymbolReaderProvider)Activator.CreateInstance(symbolType);
		}

		// Token: 0x060014CF RID: 5327 RVA: 0x00041C44 File Offset: 0x0003FE44
		private static string GetSymbolTypeName(SymbolKind kind, string name)
		{
			return string.Concat(new string[]
			{
				"Mono.Cecil.",
				SymbolProvider.GetSymbolNamespace(kind),
				".",
				kind.ToString(),
				name
			});
		}

		// Token: 0x060014D0 RID: 5328 RVA: 0x00041C7E File Offset: 0x0003FE7E
		private static string GetSymbolNamespace(SymbolKind kind)
		{
			if (kind == SymbolKind.PortablePdb || kind == SymbolKind.EmbeddedPortablePdb)
			{
				return "Cil";
			}
			if (kind == SymbolKind.NativePdb)
			{
				return "Pdb";
			}
			if (kind == SymbolKind.Mdb)
			{
				return "Mdb";
			}
			throw new ArgumentException();
		}
	}
}
