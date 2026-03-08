using System;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000951 RID: 2385
	internal class ImporterCallback : ITypeLibImporterNotifySink
	{
		// Token: 0x060061AA RID: 25002 RVA: 0x0014E589 File Offset: 0x0014C789
		public void ReportEvent(ImporterEventKind EventKind, int EventCode, string EventMsg)
		{
		}

		// Token: 0x060061AB RID: 25003 RVA: 0x0014E58C File Offset: 0x0014C78C
		[SecuritySafeCritical]
		public Assembly ResolveRef(object TypeLib)
		{
			Assembly result;
			try
			{
				ITypeLibConverter typeLibConverter = new TypeLibConverter();
				result = typeLibConverter.ConvertTypeLibToAssembly(TypeLib, Marshal.GetTypeLibName((ITypeLib)TypeLib) + ".dll", TypeLibImporterFlags.None, new ImporterCallback(), null, null, null, null);
			}
			catch (Exception)
			{
				result = null;
			}
			return result;
		}
	}
}
