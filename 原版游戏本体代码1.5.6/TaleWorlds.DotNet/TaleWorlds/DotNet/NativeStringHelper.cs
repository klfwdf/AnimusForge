using System;

namespace TaleWorlds.DotNet;

internal static class NativeStringHelper
{
	internal static UIntPtr CreateRglVarString(string text)
	{
		return LibraryApplicationInterface.INativeStringHelper.CreateRglVarString(text);
	}

	internal static UIntPtr GetThreadLocalCachedRglVarString()
	{
		return LibraryApplicationInterface.INativeStringHelper.GetThreadLocalCachedRglVarString();
	}

	internal static void SetRglVarString(UIntPtr pointer, string text)
	{
		LibraryApplicationInterface.INativeStringHelper.SetRglVarString(pointer, text);
	}

	internal static void DeleteRglVarString(UIntPtr pointer)
	{
		LibraryApplicationInterface.INativeStringHelper.DeleteRglVarString(pointer);
	}
}
