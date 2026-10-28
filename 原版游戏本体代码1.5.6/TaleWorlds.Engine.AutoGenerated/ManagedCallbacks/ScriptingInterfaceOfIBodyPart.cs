using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIBodyPart : IBodyPart
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool DoSegmentsIntersectDelegate(Vec2 line1Start, Vec2 line1Direction, Vec2 line2Start, Vec2 line2Direction, ref Vec2 intersectionPoint);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static DoSegmentsIntersectDelegate call_DoSegmentsIntersectDelegate;

	public bool DoSegmentsIntersect(Vec2 line1Start, Vec2 line1Direction, Vec2 line2Start, Vec2 line2Direction, ref Vec2 intersectionPoint)
	{
		return call_DoSegmentsIntersectDelegate(line1Start, line1Direction, line2Start, line2Direction, ref intersectionPoint);
	}
}
