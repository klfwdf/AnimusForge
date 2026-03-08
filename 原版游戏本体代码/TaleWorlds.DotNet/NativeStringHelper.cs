using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200002F RID: 47
	internal static class NativeStringHelper
	{
		// Token: 0x06000130 RID: 304 RVA: 0x0000575A File Offset: 0x0000395A
		internal static UIntPtr CreateRglVarString(string text)
		{
			return LibraryApplicationInterface.INativeStringHelper.CreateRglVarString(text);
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00005767 File Offset: 0x00003967
		internal static UIntPtr GetThreadLocalCachedRglVarString()
		{
			return LibraryApplicationInterface.INativeStringHelper.GetThreadLocalCachedRglVarString();
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00005773 File Offset: 0x00003973
		internal static void SetRglVarString(UIntPtr pointer, string text)
		{
			LibraryApplicationInterface.INativeStringHelper.SetRglVarString(pointer, text);
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00005781 File Offset: 0x00003981
		internal static void DeleteRglVarString(UIntPtr pointer)
		{
			LibraryApplicationInterface.INativeStringHelper.DeleteRglVarString(pointer);
		}
	}
}
