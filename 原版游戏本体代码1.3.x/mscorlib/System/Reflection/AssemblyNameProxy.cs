using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005C8 RID: 1480
	[ComVisible(true)]
	public class AssemblyNameProxy : MarshalByRefObject
	{
		// Token: 0x060044B3 RID: 17587 RVA: 0x000FCE76 File Offset: 0x000FB076
		public AssemblyName GetAssemblyName(string assemblyFile)
		{
			return AssemblyName.GetAssemblyName(assemblyFile);
		}
	}
}
