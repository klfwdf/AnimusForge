using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IAsyncTask
{
	[EngineMethod("create_with_function", false, null, false)]
	AsyncTask CreateWithDelegate(ManagedDelegate function, bool isBackground);

	[EngineMethod("invoke", false, null, false)]
	void Invoke(UIntPtr Pointer);

	[EngineMethod("wait", false, null, false)]
	void Wait(UIntPtr Pointer);
}
