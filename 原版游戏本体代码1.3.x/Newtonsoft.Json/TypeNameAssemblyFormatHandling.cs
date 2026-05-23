using System;

namespace Newtonsoft.Json
{
	/// <summary>
	/// Indicates the method that will be used during deserialization for locating and loading assemblies.
	/// </summary>
	// Token: 0x0200003D RID: 61
	public enum TypeNameAssemblyFormatHandling
	{
		/// <summary>
		/// In simple mode, the assembly used during deserialization need not match exactly the assembly used during serialization. Specifically, the version numbers need not match as the <c>LoadWithPartialName</c> method of the <see cref="T:System.Reflection.Assembly" /> class is used to load the assembly.
		/// </summary>
		// Token: 0x0400013B RID: 315
		Simple,
		/// <summary>
		/// In full mode, the assembly used during deserialization must match exactly the assembly used during serialization. The <c>Load</c> method of the <see cref="T:System.Reflection.Assembly" /> class is used to load the assembly.
		/// </summary>
		// Token: 0x0400013C RID: 316
		Full
	}
}
