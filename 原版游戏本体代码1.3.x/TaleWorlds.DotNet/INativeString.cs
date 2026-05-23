using System;
using TaleWorlds.Library;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200001D RID: 29
	[LibraryInterfaceBase]
	internal interface INativeString
	{
		// Token: 0x0600008C RID: 140
		[EngineMethod("create", false, null, false)]
		NativeString Create();

		// Token: 0x0600008D RID: 141
		[EngineMethod("get_string", false, null, false)]
		string GetString(NativeString nativeString);

		// Token: 0x0600008E RID: 142
		[EngineMethod("set_string", false, null, false)]
		void SetString(NativeString nativeString, string newString);
	}
}
