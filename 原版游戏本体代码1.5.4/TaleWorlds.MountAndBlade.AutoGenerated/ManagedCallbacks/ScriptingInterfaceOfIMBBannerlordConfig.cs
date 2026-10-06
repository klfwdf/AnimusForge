using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBBannerlordConfig : IMBBannerlordConfig
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ValidateOptionsDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static ValidateOptionsDelegate call_ValidateOptionsDelegate;

	public void ValidateOptions()
	{
		call_ValidateOptionsDelegate();
	}
}
