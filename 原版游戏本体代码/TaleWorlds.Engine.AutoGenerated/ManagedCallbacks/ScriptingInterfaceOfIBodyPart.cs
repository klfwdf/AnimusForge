using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000009 RID: 9
	internal class ScriptingInterfaceOfIBodyPart : IBodyPart
	{
		// Token: 0x0600006D RID: 109 RVA: 0x0000EC0C File Offset: 0x0000CE0C
		public bool DoSegmentsIntersect(Vec2 line1Start, Vec2 line1Direction, Vec2 line2Start, Vec2 line2Direction, ref Vec2 intersectionPoint)
		{
			return ScriptingInterfaceOfIBodyPart.call_DoSegmentsIntersectDelegate(line1Start, line1Direction, line2Start, line2Direction, ref intersectionPoint);
		}

		// Token: 0x04000006 RID: 6
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000007 RID: 7
		public static ScriptingInterfaceOfIBodyPart.DoSegmentsIntersectDelegate call_DoSegmentsIntersectDelegate;

		// Token: 0x0200008C RID: 140
		// (Invoke) Token: 0x06000843 RID: 2115
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool DoSegmentsIntersectDelegate(Vec2 line1Start, Vec2 line1Direction, Vec2 line2Start, Vec2 line2Direction, ref Vec2 intersectionPoint);
	}
}
