using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Bannerlord.ModuleManager;

namespace Bannerlord.BUTR.Shared.Helpers;

[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal record ModuleInfoExtendedHelper : Bannerlord.ModuleManager.ModuleInfoExtended
{
	public bool IsExternal { get; }

	public string Path { get; }

	public ModuleInfoExtendedHelper(Bannerlord.ModuleManager.ModuleInfoExtended module, bool isExternal, string path)
		: base(module)
	{
		IsExternal = isExternal;
		Path = path;
	}
}
