using System;
using System.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000230 RID: 560
	internal class DefaultAssemblyResolver : BaseAssemblyResolver
	{
		// Token: 0x06000C05 RID: 3077 RVA: 0x0002A5E6 File Offset: 0x000287E6
		public DefaultAssemblyResolver()
		{
			this.cache = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
		}

		// Token: 0x06000C06 RID: 3078 RVA: 0x0002A600 File Offset: 0x00028800
		public override AssemblyDefinition Resolve(AssemblyNameReference name)
		{
			Mixin.CheckName(name);
			AssemblyDefinition assembly;
			if (this.cache.TryGetValue(name.FullName, out assembly))
			{
				return assembly;
			}
			assembly = base.Resolve(name);
			this.cache[name.FullName] = assembly;
			return assembly;
		}

		// Token: 0x06000C07 RID: 3079 RVA: 0x0002A648 File Offset: 0x00028848
		protected void RegisterAssembly(AssemblyDefinition assembly)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException("assembly");
			}
			string name = assembly.Name.FullName;
			if (this.cache.ContainsKey(name))
			{
				return;
			}
			this.cache[name] = assembly;
		}

		// Token: 0x06000C08 RID: 3080 RVA: 0x0002A68C File Offset: 0x0002888C
		protected override void Dispose(bool disposing)
		{
			foreach (AssemblyDefinition assemblyDefinition in this.cache.Values)
			{
				assemblyDefinition.Dispose();
			}
			this.cache.Clear();
			base.Dispose(disposing);
		}

		// Token: 0x0400039E RID: 926
		private readonly IDictionary<string, AssemblyDefinition> cache;
	}
}
