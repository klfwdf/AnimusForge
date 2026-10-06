using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfITime : ITime
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetApplicationTimeDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetApplicationTimeDelegate call_GetApplicationTimeDelegate;

	public float GetApplicationTime()
	{
		return call_GetApplicationTimeDelegate();
	}
}
