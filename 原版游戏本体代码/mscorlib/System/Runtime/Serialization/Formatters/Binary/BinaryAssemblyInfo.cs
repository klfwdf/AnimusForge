using System;
using System.Reflection;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000781 RID: 1921
	internal sealed class BinaryAssemblyInfo
	{
		// Token: 0x060053C3 RID: 21443 RVA: 0x00126E97 File Offset: 0x00125097
		internal BinaryAssemblyInfo(string assemblyString)
		{
			this.assemblyString = assemblyString;
		}

		// Token: 0x060053C4 RID: 21444 RVA: 0x00126EA6 File Offset: 0x001250A6
		internal BinaryAssemblyInfo(string assemblyString, Assembly assembly)
		{
			this.assemblyString = assemblyString;
			this.assembly = assembly;
		}

		// Token: 0x060053C5 RID: 21445 RVA: 0x00126EBC File Offset: 0x001250BC
		internal Assembly GetAssembly()
		{
			if (this.assembly == null)
			{
				this.assembly = FormatterServices.LoadAssemblyFromStringNoThrow(this.assemblyString);
				if (this.assembly == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_AssemblyNotFound", new object[] { this.assemblyString }));
				}
			}
			return this.assembly;
		}

		// Token: 0x040025B9 RID: 9657
		internal string assemblyString;

		// Token: 0x040025BA RID: 9658
		private Assembly assembly;
	}
}
