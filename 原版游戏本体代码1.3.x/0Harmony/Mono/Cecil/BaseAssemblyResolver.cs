using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000229 RID: 553
	internal abstract class BaseAssemblyResolver : IAssemblyResolver, IDisposable
	{
		// Token: 0x06000BB6 RID: 2998 RVA: 0x0002993C File Offset: 0x00027B3C
		public void AddSearchDirectory(string directory)
		{
			this.directories.Add(directory);
		}

		// Token: 0x06000BB7 RID: 2999 RVA: 0x0002994A File Offset: 0x00027B4A
		public void RemoveSearchDirectory(string directory)
		{
			this.directories.Remove(directory);
		}

		// Token: 0x06000BB8 RID: 3000 RVA: 0x0002995C File Offset: 0x00027B5C
		public string[] GetSearchDirectories()
		{
			string[] directories = new string[this.directories.size];
			Array.Copy(this.directories.items, directories, directories.Length);
			return directories;
		}

		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000BB9 RID: 3001 RVA: 0x00029990 File Offset: 0x00027B90
		// (remove) Token: 0x06000BBA RID: 3002 RVA: 0x000299C8 File Offset: 0x00027BC8
		public event AssemblyResolveEventHandler ResolveFailure;

		// Token: 0x06000BBB RID: 3003 RVA: 0x000299FD File Offset: 0x00027BFD
		protected BaseAssemblyResolver()
		{
			this.directories = new Collection<string>(2) { ".", "bin" };
		}

		// Token: 0x06000BBC RID: 3004 RVA: 0x00029A27 File Offset: 0x00027C27
		private AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
		{
			if (parameters.AssemblyResolver == null)
			{
				parameters.AssemblyResolver = this;
			}
			return ModuleDefinition.ReadModule(file, parameters).Assembly;
		}

		// Token: 0x06000BBD RID: 3005 RVA: 0x00029A44 File Offset: 0x00027C44
		public virtual AssemblyDefinition Resolve(AssemblyNameReference name)
		{
			return this.Resolve(name, new ReaderParameters());
		}

		// Token: 0x06000BBE RID: 3006 RVA: 0x00029A54 File Offset: 0x00027C54
		public virtual AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			Mixin.CheckName(name);
			Mixin.CheckParameters(parameters);
			AssemblyDefinition assembly = this.SearchDirectory(name, this.directories, parameters);
			if (assembly != null)
			{
				return assembly;
			}
			if (name.IsRetargetable)
			{
				name = new AssemblyNameReference(name.Name, Mixin.ZeroVersion)
				{
					PublicKeyToken = Empty<byte>.Array
				};
			}
			string framework_dir = Path.GetDirectoryName(typeof(object).Module.FullyQualifiedName);
			string[] array;
			if (!BaseAssemblyResolver.on_mono)
			{
				(array = new string[1])[0] = framework_dir;
			}
			else
			{
				string[] array2 = new string[2];
				array2[0] = framework_dir;
				array = array2;
				array2[1] = Path.Combine(framework_dir, "Facades");
			}
			string[] framework_dirs = array;
			if (BaseAssemblyResolver.IsZero(name.Version))
			{
				assembly = this.SearchDirectory(name, framework_dirs, parameters);
				if (assembly != null)
				{
					return assembly;
				}
			}
			if (name.Name == "mscorlib")
			{
				assembly = this.GetCorlib(name, parameters);
				if (assembly != null)
				{
					return assembly;
				}
			}
			assembly = this.GetAssemblyInGac(name, parameters);
			if (assembly != null)
			{
				return assembly;
			}
			assembly = this.SearchDirectory(name, framework_dirs, parameters);
			if (assembly != null)
			{
				return assembly;
			}
			if (this.ResolveFailure != null)
			{
				assembly = this.ResolveFailure(this, name);
				if (assembly != null)
				{
					return assembly;
				}
			}
			throw new AssemblyResolutionException(name);
		}

		// Token: 0x06000BBF RID: 3007 RVA: 0x00029B68 File Offset: 0x00027D68
		protected virtual AssemblyDefinition SearchDirectory(AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
		{
			string[] array2;
			if (!name.IsWindowsRuntime)
			{
				string[] array = new string[2];
				array[0] = ".dll";
				array2 = array;
				array[1] = ".exe";
			}
			else
			{
				string[] array3 = new string[2];
				array3[0] = ".winmd";
				array2 = array3;
				array3[1] = ".dll";
			}
			string[] extensions = array2;
			foreach (string directory in directories)
			{
				foreach (string extension in extensions)
				{
					string file = Path.Combine(directory, name.Name + extension);
					if (File.Exists(file))
					{
						try
						{
							return this.GetAssembly(file, parameters);
						}
						catch (BadImageFormatException)
						{
						}
					}
				}
			}
			return null;
		}

		// Token: 0x06000BC0 RID: 3008 RVA: 0x00029C3C File Offset: 0x00027E3C
		private static bool IsZero(Version version)
		{
			return version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0;
		}

		// Token: 0x06000BC1 RID: 3009 RVA: 0x00029C64 File Offset: 0x00027E64
		private AssemblyDefinition GetCorlib(AssemblyNameReference reference, ReaderParameters parameters)
		{
			Version version = reference.Version;
			if (typeof(object).Assembly.GetName().Version == version || BaseAssemblyResolver.IsZero(version))
			{
				return this.GetAssembly(typeof(object).Module.FullyQualifiedName, parameters);
			}
			string path = Directory.GetParent(Directory.GetParent(typeof(object).Module.FullyQualifiedName).FullName).FullName;
			if (!BaseAssemblyResolver.on_mono)
			{
				switch (version.Major)
				{
				case 1:
					if (version.MajorRevision == 3300)
					{
						path = Path.Combine(path, "v1.0.3705");
						goto IL_187;
					}
					path = Path.Combine(path, "v1.1.4322");
					goto IL_187;
				case 2:
					path = Path.Combine(path, "v2.0.50727");
					goto IL_187;
				case 4:
					path = Path.Combine(path, "v4.0.30319");
					goto IL_187;
				}
				string str = "Version not supported: ";
				Version version2 = version;
				throw new NotSupportedException(str + ((version2 != null) ? version2.ToString() : null));
			}
			if (version.Major == 1)
			{
				path = Path.Combine(path, "1.0");
			}
			else if (version.Major == 2)
			{
				if (version.MajorRevision == 5)
				{
					path = Path.Combine(path, "2.1");
				}
				else
				{
					path = Path.Combine(path, "2.0");
				}
			}
			else
			{
				if (version.Major != 4)
				{
					string str2 = "Version not supported: ";
					Version version3 = version;
					throw new NotSupportedException(str2 + ((version3 != null) ? version3.ToString() : null));
				}
				path = Path.Combine(path, "4.0");
			}
			IL_187:
			string file = Path.Combine(path, "mscorlib.dll");
			if (File.Exists(file))
			{
				return this.GetAssembly(file, parameters);
			}
			if (BaseAssemblyResolver.on_mono && Directory.Exists(path + "-api"))
			{
				file = Path.Combine(path + "-api", "mscorlib.dll");
				if (File.Exists(file))
				{
					return this.GetAssembly(file, parameters);
				}
			}
			return null;
		}

		// Token: 0x06000BC2 RID: 3010 RVA: 0x00029E58 File Offset: 0x00028058
		private static Collection<string> GetGacPaths()
		{
			if (BaseAssemblyResolver.on_mono)
			{
				return BaseAssemblyResolver.GetDefaultMonoGacPaths();
			}
			Collection<string> paths = new Collection<string>(2);
			string windir = Environment.GetEnvironmentVariable("WINDIR");
			if (windir == null)
			{
				return paths;
			}
			paths.Add(Path.Combine(windir, "assembly"));
			paths.Add(Path.Combine(windir, Path.Combine("Microsoft.NET", "assembly")));
			return paths;
		}

		// Token: 0x06000BC3 RID: 3011 RVA: 0x00029EB8 File Offset: 0x000280B8
		private static Collection<string> GetDefaultMonoGacPaths()
		{
			Collection<string> paths = new Collection<string>(1);
			string gac = BaseAssemblyResolver.GetCurrentMonoGac();
			if (gac != null)
			{
				paths.Add(gac);
			}
			string gac_paths_env = Environment.GetEnvironmentVariable("MONO_GAC_PREFIX");
			if (string.IsNullOrEmpty(gac_paths_env))
			{
				return paths;
			}
			foreach (string prefix in gac_paths_env.Split(new char[] { Path.PathSeparator }))
			{
				if (!string.IsNullOrEmpty(prefix))
				{
					string gac_path = Path.Combine(Path.Combine(Path.Combine(prefix, "lib"), "mono"), "gac");
					if (Directory.Exists(gac_path) && !paths.Contains(gac))
					{
						paths.Add(gac_path);
					}
				}
			}
			return paths;
		}

		// Token: 0x06000BC4 RID: 3012 RVA: 0x00029F64 File Offset: 0x00028164
		private static string GetCurrentMonoGac()
		{
			return Path.Combine(Directory.GetParent(Path.GetDirectoryName(typeof(object).Module.FullyQualifiedName)).FullName, "gac");
		}

		// Token: 0x06000BC5 RID: 3013 RVA: 0x00029F93 File Offset: 0x00028193
		private AssemblyDefinition GetAssemblyInGac(AssemblyNameReference reference, ReaderParameters parameters)
		{
			if (reference.PublicKeyToken == null || reference.PublicKeyToken.Length == 0)
			{
				return null;
			}
			if (this.gac_paths == null)
			{
				this.gac_paths = BaseAssemblyResolver.GetGacPaths();
			}
			if (BaseAssemblyResolver.on_mono)
			{
				return this.GetAssemblyInMonoGac(reference, parameters);
			}
			return this.GetAssemblyInNetGac(reference, parameters);
		}

		// Token: 0x06000BC6 RID: 3014 RVA: 0x00029FD4 File Offset: 0x000281D4
		private AssemblyDefinition GetAssemblyInMonoGac(AssemblyNameReference reference, ReaderParameters parameters)
		{
			for (int i = 0; i < this.gac_paths.Count; i++)
			{
				string gac_path = this.gac_paths[i];
				string file = BaseAssemblyResolver.GetAssemblyFile(reference, string.Empty, gac_path);
				if (File.Exists(file))
				{
					return this.GetAssembly(file, parameters);
				}
			}
			return null;
		}

		// Token: 0x06000BC7 RID: 3015 RVA: 0x0002A024 File Offset: 0x00028224
		private AssemblyDefinition GetAssemblyInNetGac(AssemblyNameReference reference, ReaderParameters parameters)
		{
			string[] gacs = new string[] { "GAC_MSIL", "GAC_32", "GAC_64", "GAC" };
			string[] prefixes = new string[]
			{
				string.Empty,
				"v4.0_"
			};
			for (int i = 0; i < this.gac_paths.Count; i++)
			{
				for (int j = 0; j < gacs.Length; j++)
				{
					string gac = Path.Combine(this.gac_paths[i], gacs[j]);
					string file = BaseAssemblyResolver.GetAssemblyFile(reference, prefixes[i], gac);
					if (Directory.Exists(gac) && File.Exists(file))
					{
						return this.GetAssembly(file, parameters);
					}
				}
			}
			return null;
		}

		// Token: 0x06000BC8 RID: 3016 RVA: 0x0002A0D4 File Offset: 0x000282D4
		private static string GetAssemblyFile(AssemblyNameReference reference, string prefix, string gac)
		{
			StringBuilder gac_folder = new StringBuilder().Append(prefix).Append(reference.Version).Append("__");
			for (int i = 0; i < reference.PublicKeyToken.Length; i++)
			{
				gac_folder.Append(reference.PublicKeyToken[i].ToString("x2"));
			}
			return Path.Combine(Path.Combine(Path.Combine(gac, reference.Name), gac_folder.ToString()), reference.Name + ".dll");
		}

		// Token: 0x06000BC9 RID: 3017 RVA: 0x0002A15E File Offset: 0x0002835E
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06000BCA RID: 3018 RVA: 0x0001B842 File Offset: 0x00019A42
		protected virtual void Dispose(bool disposing)
		{
		}

		// Token: 0x0400038B RID: 907
		private static readonly bool on_mono = Type.GetType("Mono.Runtime") != null;

		// Token: 0x0400038C RID: 908
		private readonly Collection<string> directories;

		// Token: 0x0400038D RID: 909
		private Collection<string> gac_paths;
	}
}
