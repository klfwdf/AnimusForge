using System;
using System.IO;
using System.Reflection;
using System.Security;

namespace System.Globalization
{
	// Token: 0x020003BB RID: 955
	internal sealed class GlobalizationAssembly
	{
		// Token: 0x06002F5A RID: 12122 RVA: 0x000B5E20 File Offset: 0x000B4020
		[SecurityCritical]
		internal unsafe static byte* GetGlobalizationResourceBytePtr(Assembly assembly, string tableName)
		{
			Stream manifestResourceStream = assembly.GetManifestResourceStream(tableName);
			UnmanagedMemoryStream unmanagedMemoryStream = manifestResourceStream as UnmanagedMemoryStream;
			if (unmanagedMemoryStream != null)
			{
				byte* positionPointer = unmanagedMemoryStream.PositionPointer;
				if (positionPointer != null)
				{
					return positionPointer;
				}
			}
			throw new InvalidOperationException();
		}
	}
}
