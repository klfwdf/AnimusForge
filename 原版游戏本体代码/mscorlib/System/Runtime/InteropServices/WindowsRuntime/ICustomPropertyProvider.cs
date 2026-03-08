using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A0B RID: 2571
	[Guid("7C925755-3E48-42B4-8677-76372267033F")]
	[ComImport]
	internal interface ICustomPropertyProvider
	{
		// Token: 0x06006580 RID: 25984
		ICustomProperty GetCustomProperty(string name);

		// Token: 0x06006581 RID: 25985
		ICustomProperty GetIndexedProperty(string name, Type indexParameterType);

		// Token: 0x06006582 RID: 25986
		string GetStringRepresentation();

		// Token: 0x1700116F RID: 4463
		// (get) Token: 0x06006583 RID: 25987
		Type Type { get; }
	}
}
