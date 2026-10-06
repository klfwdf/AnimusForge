using System;

namespace TaleWorlds.DotNet;

[EngineClass("ftdnNative_string")]
public sealed class NativeString : NativeObject
{
	internal NativeString(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public static NativeString Create()
	{
		return LibraryApplicationInterface.INativeString.Create();
	}

	public string GetString()
	{
		return LibraryApplicationInterface.INativeString.GetString(this);
	}

	public void SetString(string newString)
	{
		LibraryApplicationInterface.INativeString.SetString(this, newString);
	}
}
